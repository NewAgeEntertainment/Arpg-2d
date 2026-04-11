using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CompanionPartyManager : MonoBehaviour
{
    public static CompanionPartyManager Instance { get; private set; }
    public static event System.Action<Transform> OnPlayerResolved;

    public static event System.Action<string, GameObject> OnRecruited;
    public static event System.Action<string> OnDismissed;
    public static event System.Action OnPartyChanged;

    [Header("Lifetime")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Player / Roots")]
    public Transform player;          // auto-resolved
    public Transform partyRoot;       // DDOL parent for travelling companions

    [System.Serializable]
    public class RosterEntry
    {
        [Tooltip("Unique roster id (must match CompanionIdentity.id).")]
        public string id;

        [Tooltip("Prefab fallback if no scene instance is found when recruiting.")]
        public GameObject prefab;

        [Tooltip("(Optional/legacy) Scene instance reference — not required with identity search.")]
        public Transform sceneInstance;

        [Tooltip("Auto-recruit on startup (useful if this manager starts in a save or test scene).")]
        public bool startInParty = false;
    }

    [Header("Roster")]
    public List<RosterEntry> roster = new List<RosterEntry>();

    [Header("Spawn for prefab fallback")]
    public float spawnOffset = 0.8f;
    public float ringSpacing = 0.6f;

    [Header("Behavior")]
    public bool forceFollowOnRecruit = true;
    public bool reactivateIfAlreadyInScene = true;

    [Header("Scene Load Snap (keep close to player)")]
    public float snapIfFartherThan = 6f;
    public float snapRingRadius = 1.4f;
    public float snapExtraPerCompanion = 0.25f;
    public float snapDelay = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    // ---------- Runtime ----------
    private readonly Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>();
    private Coroutine resolveCo;

    // ---------- NEW: origin remembering for scene instances ----------
    [System.Serializable]
    private struct OriginInfo
    {
        public string sceneName;
        public string parentPath;   // transform path under that scene
        public Vector3 position;
        public Quaternion rotation;
        public bool wasActive;
        public bool wasPrefabSpawn; // prefab fallback (no origin to return to)
    }

    // id -> origin info
    private readonly Dictionary<string, OriginInfo> _originById = new Dictionary<string, OriginInfo>();

    // queue returns when target scene isn't loaded yet
    private readonly List<(string id, GameObject go, OriginInfo origin)> _pendingReturns = new();

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        if (partyRoot == null)
        {
            var go = new GameObject("PartyRoot");
            partyRoot = go.transform;
        }
        if (dontDestroyOnLoad) DontDestroyOnLoad(partyRoot.gameObject);

        TryResolvePlayerImmediate();

        foreach (var e in roster)
            if (e != null && e.startInParty)
                Recruit(e.id, silent: false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        PlayerLocator.OnChanged += HandleLocatorChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        PlayerLocator.OnChanged -= HandleLocatorChanged;
    }

    private void HandleLocatorChanged(Transform t)
    {
        if (!t) return;
        player = t;
        OnPlayerResolved?.Invoke(player);
        RebindAllCompanionsTo(player);
        if (verboseLogs) Debug.Log($"[PartyManager] PlayerLocator → {player.name}");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (verboseLogs) Debug.Log($"[PartyManager] Scene loaded: {scene.name}");
        if (resolveCo != null) StopCoroutine(resolveCo);
        resolveCo = StartCoroutine(ResolvePlayerRebindAndSnap());

        // Process any pending returns destined for this scene
        if (_pendingReturns.Count > 0)
        {
            for (int i = _pendingReturns.Count - 1; i >= 0; i--)
            {
                var pr = _pendingReturns[i];
                if (pr.origin.sceneName == scene.name)
                {
                    try { ReturnCompanionToOrigin(pr.go, pr.origin); }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[PartyManager] Pending return for '{pr.id}' failed: {ex}");
                    }
                    _pendingReturns.RemoveAt(i);
                }
            }
        }
    }

    private IEnumerator ResolvePlayerRebindAndSnap(float timeout = 6f)
    {
        yield return null; // let spawners place the player
        float end = Time.unscaledTime + timeout;
        while (Time.unscaledTime < end)
        {
            if (TryResolvePlayerImmediate())
            {
                RebindAllCompanionsTo(player);
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, snapDelay));
                SnapAllCompanionsNearPlayer();
                yield break;
            }
            yield return null;
        }
        if (verboseLogs) Debug.LogWarning("[PartyManager] Timed out waiting for Player.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────
    public IEnumerable<string> ActiveIds()
    {
        foreach (var kv in _active) yield return kv.Key;
    }

    public GameObject FindActiveInstance(string id)
    {
        return (!string.IsNullOrEmpty(id) && _active.TryGetValue(id, out var go)) ? go : null;
    }

    public GameObject Recruit(string id, bool silent = false)
    {
        var entry = GetEntry(id);
        if (entry == null)
        {
            Debug.LogWarning($"[PartyManager] No roster entry '{id}'.");
            return null;
        }

        // 0) Already traveling?
        if (_active.TryGetValue(id, out var existing) && existing)
        {
            if (reactivateIfAlreadyInScene && !existing.activeSelf)
                existing.SetActive(true);

            FlagInParty(existing, true);

            if (!silent && verboseLogs)
                Debug.Log($"[PartyManager] Recruit '{id}' → already active @{existing.transform.position}");

            return existing;
        }

        // Has this companion already been recruited/persisted before?
        bool hasPersistentState =
            _originById.ContainsKey(id) ||
            CompanionStatsSaver.PendingSnapshots.ContainsKey(id);

        GameObject go = null;

        // 1) First-time recruitment: prefer scene instance if one exists.
        if (!hasPersistentState)
        {
            var sceneInst = FindSceneInstanceById(id);
            if (sceneInst != null)
            {
                go = sceneInst;

                if (reactivateIfAlreadyInScene && !go.activeSelf)
                    go.SetActive(true);

                // Remember where the scene version came from before promoting it.
                RememberOrigin(id, go, wasPrefab: false);

                PromoteToDDOL(go);
                SetupCompanion(go);
                if (forceFollowOnRecruit) ForceFollow(go);
                FlagInParty(go, true);

                _active[id] = go;

                OnRecruited?.Invoke(id, go);
                OnPartyChanged?.Invoke();

                if (!silent && verboseLogs)
                    Debug.Log($"[PartyManager] Recruit '{id}' (first-time scene instance)");

                return go;
            }
        }

        // 2) Persistent restore / later recruit: prefer prefab.
        if (entry.prefab != null)
        {
            go = Instantiate(entry.prefab, partyRoot);
            go.transform.position = GetSpawnPosition();

            // Prefab-spawned persistent companion; do not try to bind future restores
            // to scene copies.
            RememberOrigin(id, go, wasPrefab: true);

            SetupCompanion(go);
            if (forceFollowOnRecruit) ForceFollow(go);
            FlagInParty(go, true);

            _active[id] = go;

            OnRecruited?.Invoke(id, go);
            OnPartyChanged?.Invoke();

            if (!silent && verboseLogs)
            {
                string why = hasPersistentState ? "persistent prefab restore" : "prefab fallback";
                Debug.Log($"[PartyManager] Recruit '{id}' ({why})");
            }

            return go;
        }

        // 3) Last resort: scene instance only if no prefab exists at all.
        var fallbackSceneInst = FindSceneInstanceById(id);
        if (fallbackSceneInst != null)
        {
            go = fallbackSceneInst;

            if (reactivateIfAlreadyInScene && !go.activeSelf)
                go.SetActive(true);

            RememberOrigin(id, go, wasPrefab: false);

            PromoteToDDOL(go);
            SetupCompanion(go);
            if (forceFollowOnRecruit) ForceFollow(go);
            FlagInParty(go, true);

            _active[id] = go;

            OnRecruited?.Invoke(id, go);
            OnPartyChanged?.Invoke();

            if (!silent && verboseLogs)
                Debug.Log($"[PartyManager] Recruit '{id}' (scene fallback, no prefab assigned)");

            return go;
        }

        Debug.LogWarning($"[PartyManager] Recruit '{id}' FAILED: no prefab assigned and no scene instance found.");
        return null;
    }

    public void Dismiss(string id, bool destroy = false)
    {
        var go = FindActiveInstance(id);
        if (go == null)
        {
            if (verboseLogs) Debug.LogWarning($"[PartyManager] Dismiss '{id}' ignored: not active.");
            return;
        }

        // Clear party flag on the component if present
        FlagInParty(go, false);

        // Remove from active set first so listeners see the new state
        _active.Remove(id);

        // Return or disable/destroy
        if (_originById.TryGetValue(id, out var origin))
        {
            if (origin.wasPrefabSpawn)
            {
                // Persistent companions spawned from prefab should be removed cleanly
                // so future Recruit() creates a fresh persistent instance.
                Destroy(go);
            }
            else
            {
                ReturnCompanionToOrigin(go, origin);
            }
        }
        else
        {
            Destroy(go);
        }

        if (verboseLogs) Debug.Log($"[PartyManager] Dismissed '{id}'");

        OnDismissed?.Invoke(id);
        OnPartyChanged?.Invoke();
    }

    public void DismissAll(bool destroy = false)
    {
        var list = new List<string>(_active.Keys);
        foreach (var id in list) Dismiss(id, destroy);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────────────────
    private RosterEntry GetEntry(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < roster.Count; i++)
            if (roster[i] != null && roster[i].id == id) return roster[i];
        return null;
    }

    private GameObject FindSceneInstanceById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        var all = FindObjectsByType<CompanionIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var ci = all[i];
            if (ci == null || ci.id != id) continue;

            var go = ci.gameObject;
            if (go == null) continue;

            // Never treat DDOL travelling companions as scene candidates.
            if (go.scene.name == "DontDestroyOnLoad") continue;

            // If this object is already one of our active party instances, skip it.
            if (_active.TryGetValue(id, out var activeGo) && activeGo == go)
                continue;

            // If the scene copy is already marked as being in the party, skip it.
            var comp = go.GetComponent<Companion>();
            if (comp != null && comp.InParty)
                continue;

            return go;
        }

        return null;
    }

    private void SetupCompanion(GameObject go)
    {
        if (go.transform.parent != partyRoot)
            go.transform.SetParent(partyRoot, true);

        var comp = go.GetComponent<Companion>();
        if (comp != null && comp.playerTarget == null && player != null)
            comp.playerTarget = player;
    }

    private void ForceFollow(GameObject go)
    {
        var comp = go.GetComponent<Companion>();
        if (comp != null) comp.SetInParty(true);
    }

    private void FlagInParty(GameObject go, bool value)
    {
        var comp = go.GetComponent<Companion>();
        if (comp != null) comp.SetInParty(value);
    }

    private Vector3 GetSpawnPosition()
    {
        if (player == null) return Vector3.zero;

        int n = _active.Count + 1;
        float radius = spawnOffset + (n * ringSpacing * 0.25f);
        float angle = n * 110f * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        return player.position + offset;
    }

    private void PromoteToDDOL(GameObject go)
    {
        if (!go) return;
        if (go.scene.name != "DontDestroyOnLoad") DontDestroyOnLoad(go);
        if (partyRoot && go.transform.parent != partyRoot)
            go.transform.SetParent(partyRoot, true);
    }

    private bool TryResolvePlayerImmediate()
    {
        if (player != null) { OnPlayerResolved?.Invoke(player); return true; }

        if (PlayerLocator.Current != null) player = PlayerLocator.Current;

        if (player == null && GameManager.Instance != null && GameManager.Instance.Player != null)
            player = GameManager.Instance.Player.transform;

        if (player == null)
        {
            var tagObj = GameObject.FindGameObjectWithTag("Player");
            if (tagObj != null) player = tagObj.transform;
        }

        if (player == null)
        {
            var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            OnPlayerResolved?.Invoke(player);
            if (verboseLogs) Debug.Log($"[PartyManager] Resolved Player → {player.name}");
            return true;
        }
        return false;
    }

    private void RebindAllCompanionsTo(Transform newPlayer)
    {
        if (!newPlayer) return;

        foreach (var kv in _active)
        {
            var comp = kv.Value ? kv.Value.GetComponent<Companion>() : null;
            if (comp) comp.playerTarget = newPlayer;
        }

        var allComps = FindObjectsByType<Companion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var comp in allComps)
            if (comp != null && comp.playerTarget == null)
                comp.playerTarget = newPlayer;

        if (verboseLogs) Debug.Log($"[PartyManager] Rebound companions → '{newPlayer.name}'");
    }

    private void SnapAllCompanionsNearPlayer()
    {
        if (!player || _active.Count == 0) return;

        int i = 0;
        float baseRadius = snapRingRadius;
        foreach (var kv in _active)
        {
            var go = kv.Value;
            if (!go || !go.activeInHierarchy) { i++; continue; }

            Vector3 cur = go.transform.position;
            float dist = Vector2.Distance(cur, player.position);
            if (dist <= snapIfFartherThan) { i++; continue; }

            float radius = baseRadius + i * snapExtraPerCompanion;
            float angle = (110f * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Vector3 target = player.position + offset;

            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d)
            {
                rb2d.velocity = Vector2.zero;
                rb2d.position = target;
            }
            go.transform.position = target;

            i++;
            if (verboseLogs) Debug.Log($"[PartyManager] Snapped '{go.name}' near player @ {target}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // NEW: origin tracking & returning
    // ─────────────────────────────────────────────────────────────────────

    private void RememberOrigin(string id, GameObject go, bool wasPrefab)
    {
        if (wasPrefab)
        {
            _originById[id] = new OriginInfo { wasPrefabSpawn = true };
            return;
        }

        var t = go.transform;
        var origin = new OriginInfo
        {
            sceneName = go.scene.name,
            parentPath = GetTransformPath(t.parent),
            position = t.position,
            rotation = t.rotation,
            wasActive = go.activeSelf,
            wasPrefabSpawn = false
        };
        _originById[id] = origin;
    }

    private static string GetTransformPath(Transform tr)
    {
        if (tr == null) return string.Empty;
        var stack = new System.Collections.Generic.Stack<string>();
        while (tr != null && tr.gameObject.scene.name != "DontDestroyOnLoad")
        {
            stack.Push(tr.name);
            tr = tr.parent;
        }
        return string.Join("/", stack.ToArray());
    }

    private static Transform FindByPathInScene(Scene scene, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            var child = r.transform.Find(path);
            if (child != null) return child;
        }
        return null;
    }

    private void ReturnCompanionToOrigin(GameObject go, OriginInfo origin)
    {
        // If target scene isn't loaded, queue and hide
        var targetScene = SceneManager.GetSceneByName(origin.sceneName);
        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            if (verboseLogs) Debug.LogWarning($"[PartyManager] Scene '{origin.sceneName}' not loaded; queuing return for '{go.name}'.");
            _pendingReturns.Add((id: GetId(go), go: go, origin: origin));
            go.SetActive(false);
            return;
        }

        // Object MUST be a root before MoveGameObjectToScene
        go.transform.SetParent(null, worldPositionStays: true);

        // Move into original scene
        SceneManager.MoveGameObjectToScene(go, targetScene);

        // Restore parent if available
        var newParent = FindByPathInScene(targetScene, origin.parentPath);
        if (newParent != null)
            go.transform.SetParent(newParent, worldPositionStays: true);

        // Restore pose & active state
        go.transform.position = origin.position;
        go.transform.rotation = origin.rotation;
        go.SetActive(origin.wasActive);

        // Clear party state
        var comp = go.GetComponent<Companion>();
        if (comp) comp.SetInParty(false);
    }

    private string GetId(GameObject go)
    {
        var ci = go ? go.GetComponent<CompanionIdentity>() : null;
        return ci != null ? ci.id : go ? go.name : "unknown";
    }
}
