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

    private readonly Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>();
    private Coroutine resolveCo;

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
                Recruit(e.id, silent: true);
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

        // Already traveling?
        if (_active.TryGetValue(id, out var existing) && existing)
        {
            // Ensure it’s in the right state.
            if (reactivateIfAlreadyInScene && !existing.activeSelf) existing.SetActive(true);
            FlagInParty(existing, true);

            if (!silent && verboseLogs)
                Debug.Log($"[PartyManager] Recruit '{id}' → already active @{existing.transform.position}");

            // Do NOT raise OnRecruited for idempotent call.
            return existing;
        }

        // 1) Prefer an in-scene instance (promote to DDOL)
        var sceneInst = FindSceneInstanceById(id);
        if (sceneInst != null)
        {
            var go = sceneInst;
            if (reactivateIfAlreadyInScene && !go.activeSelf) go.SetActive(true);

            PromoteToDDOL(go);
            SetupCompanion(go);
            if (forceFollowOnRecruit) ForceFollow(go);
            FlagInParty(go, true);

            _active[id] = go;

            OnRecruited?.Invoke(id, go);
            OnPartyChanged?.Invoke();

            if (!silent && verboseLogs)
                Debug.Log($"[PartyManager] Recruit '{id}' (promoted scene instance)");

            return go;
        }

        // 2) Prefab fallback
        if (entry.prefab != null)
        {
            var go = Instantiate(entry.prefab, partyRoot);
            go.transform.position = GetSpawnPosition();

            SetupCompanion(go);
            if (forceFollowOnRecruit) ForceFollow(go);
            FlagInParty(go, true);

            _active[id] = go;

            OnRecruited?.Invoke(id, go);
            OnPartyChanged?.Invoke();

            if (!silent && verboseLogs)
                Debug.Log($"[PartyManager] Recruit '{id}' (prefab fallback)");

            return go;
        }

        Debug.LogWarning($"[PartyManager] Recruit '{id}' FAILED: no scene instance found here and no prefab assigned.");
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

        // Destroy or simply hide
        if (destroy) Destroy(go);
        else go.SetActive(false);

        if (verboseLogs) Debug.Log($"[PartyManager] Dismissed '{id}' (destroy={destroy})");

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

        // Look for a CompanionIdentity with matching id that is NOT already in the DDOL scene.
        var all = FindObjectsByType<CompanionIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var ci = all[i];
            if (ci == null || ci.id != id) continue;
            var go = ci.gameObject;
            if (go.scene.name == "DontDestroyOnLoad") continue; // already promoted/travelling
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
}
