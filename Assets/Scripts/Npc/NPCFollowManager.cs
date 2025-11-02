using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityObject = UnityEngine.Object;

[DefaultExecutionOrder(-1000)]
public class NPCFollowManager : MonoBehaviour
{
    private static NPCFollowManager _instance;
    public static NPCFollowManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("NPCFollowManager");
                _instance = go.AddComponent<NPCFollowManager>();
            }
            return _instance;
        }
    }

    [SerializeField, Min(0f)]
    private float teleportOffsetDistance = 1.2f;

    [SerializeField, Tooltip("If true, use 2D top-down teleport logic (x/y)")]
    private bool use2DTopDown = true;

    // how long to wait (in seconds) for the player to appear after scene load before giving up
    private const float PlayerWaitTimeout = 3.0f;
    // how often to poll for player presence while waiting
    private const float PlayerPollInterval = 0.05f;

    // Set of ids that should be following (persisted state)
    private HashSet<string> followingIds = new HashSet<string>();

    // Map id -> persistent follower GameObject (the DontDestroyOnLoad instance)
    private Dictionary<string, GameObject> persistentFollowers = new Dictionary<string, GameObject>();

    // --- NEW: pending IDs written by the saver during ApplyData (so NPCs that spawn later can pick them up)
    public static HashSet<string> PendingFollowingIds = new HashSet<string>();

    // --- NEW: fields for hooking a PlayerLocator-like type's OnChanged event ---
    private Type hookedPlayerLocatorType = null;
    private Delegate playerLocatorOnChangedDelegate = null;
    // ---------------------------------------------------------------------------

    // ------------------ NEW: Persistence helpers --------------------
    // PlayerPrefs key used as a fallback/simple persistence
    private const string PlayerPrefsKey_FollowingIds = "NPCFollowManager_FollowingIds_v1";

    // Keep track of any delegates we add to save/load events (so we can unsubscribe)
    private readonly List<Tuple<EventInfo, Delegate>> hookedSaveEvents = new List<Tuple<EventInfo, Delegate>>();
    // Also store UnityEvent subscriptions we add (FieldInfo + instance object + UnityAction) so we can remove listeners later
    private readonly List<Tuple<FieldInfo, UnityEngine.Object, UnityAction>> hookedUnityEventListeners = new List<Tuple<FieldInfo, UnityEngine.Object, UnityAction>>();
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // --- NEW: Try to find and hook any PlayerLocator-like type in loaded assemblies
        TryHookExternalPlayerLocator();

        // --- NEW: Try to auto-hook save system events (PixelCrushers SaveSystemEvents or other common save systems)
        TryHookSaveSystemEvents();

        // --- NEW: If any pending IDs were set by the saver before this manager woke,
        // merge them into followingIds and try to start follow on scene for each.
        if (PendingFollowingIds != null && PendingFollowingIds.Count > 0)
        {
            foreach (var id in PendingFollowingIds.ToList())
            {
                if (string.IsNullOrEmpty(id)) continue;
                followingIds.Add(id);
                TryStartFollowOnScene(id);
            }
            PendingFollowingIds.Clear();
        }

        // Restore prefs fallback (this was already present in earlier versions)
        RestoreFollowStateFromPrefs();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // --- NEW: Unsubscribe from PlayerLocator.OnChanged if we hooked it ---
        if (hookedPlayerLocatorType != null && playerLocatorOnChangedDelegate != null)
        {
            var ev = hookedPlayerLocatorType.GetEvent("OnChanged", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (ev != null)
            {
                try
                {
                    ev.RemoveEventHandler(null, playerLocatorOnChangedDelegate);
                }
                catch (Exception) { /* ignore unsubscribe errors */ }
            }
            playerLocatorOnChangedDelegate = null;
            hookedPlayerLocatorType = null;
        }

        // --- NEW: Unsubscribe any hooked save/load events added via EventInfo (static events) ---
        foreach (var kv in hookedSaveEvents)
        {
            try
            {
                kv.Item1.RemoveEventHandler(null, kv.Item2);
            }
            catch (Exception) { /* ignore */ }
        }
        hookedSaveEvents.Clear();

        // --- NEW: Remove UnityEvent listeners we added to SaveSystemEvents instances/fields ---
        foreach (var kv in hookedUnityEventListeners)
        {
            try
            {
                var field = kv.Item1;
                var instance = kv.Item2;
                var action = kv.Item3;
                var ue = field.GetValue(instance) as UnityEvent;
                if (ue != null && action != null)
                {
                    ue.RemoveListener(action);
                }
            }
            catch (Exception) { /* ignore */ }
        }
        hookedUnityEventListeners.Clear();
    }

    private void OnApplicationQuit()
    {
        CleanupFollowStateOnExit();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        // In the Editor, OnDisable is invoked when stopping play mode.
        // Clean up editor-persistent state to avoid carry-over between play sessions.
        CleanupFollowStateOnExit();
#endif
    }

    /// <summary>
    /// Cleanup runtime follow state on application exit / editor play-stop to avoid stale follow state persisting across sessions.
    /// </summary>
    private void CleanupFollowStateOnExit()
    {
        try
        {
            // Tell persistent followers to stop following (but do not destroy them).
            foreach (var kv in new List<KeyValuePair<string, GameObject>>(persistentFollowers))
            {
                var pers = kv.Value;
                if (pers == null) continue;
                var npcComp = pers.GetComponent<NPC>();
                if (npcComp != null)
                {
                    try { npcComp.StopFollowing(); } catch { }
                }
            }

            // Clear runtime sets so next play session starts fresh
            followingIds.Clear();
            PendingFollowingIds.Clear();

            // Remove PlayerPrefs snapshot so editor/play sessions don't accidentally reuse it
            if (PlayerPrefs.HasKey(PlayerPrefsKey_FollowingIds))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey_FollowingIds);
                PlayerPrefs.Save();
            }

            Debug.Log("[NPCFollowManager] CleanupFollowStateOnExit: cleared follow state and pending IDs.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NPCFollowManager] CleanupFollowStateOnExit error: " + e.Message);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we already have a persistent follower for an id, ensure it's active, near and rebind to the player.
        foreach (var kv in new List<KeyValuePair<string, GameObject>>(persistentFollowers))
        {
            var id = kv.Key;
            var persistentGO = kv.Value;
            if (persistentGO == null)
            {
                persistentFollowers.Remove(id);
                followingIds.Remove(id);
                continue;
            }

            // Ensure persistent GO is active so its Start/OnEnable run
            if (!persistentGO.activeSelf)
            {
                try { persistentGO.SetActive(true); }
                catch (Exception) { /* ignore */ }
            }

            // Try to find player immediately
            Transform playerTransform = FindPlayerTransform();
            if (playerTransform != null)
            {
                // Teleport and rebind (use saved decision whether to start following)
                bool shouldStartFollow = followingIds.Contains(id);
                if (use2DTopDown)
                    TeleportNearPlayer2D(persistentGO.transform, playerTransform, teleportOffsetDistance);
                else
                    TeleportIfFar3D(persistentGO.transform, playerTransform, teleportOffsetDistance);

                var npcComp = persistentGO.GetComponent<NPC>();
                if (npcComp != null)
                {
                    // Prefer RebindFollow(Transform, bool) if implemented; fallback to single-arg or StartFollow
                    var rebindMethod = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform), typeof(bool) });
                    if (rebindMethod != null)
                    {
                        rebindMethod.Invoke(npcComp, new object[] { playerTransform, shouldStartFollow });
                    }
                    else
                    {
                        var rebindSingle = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform) });
                        if (rebindSingle != null)
                        {
                            rebindSingle.Invoke(npcComp, new object[] { playerTransform });
                            if (shouldStartFollow)
                                npcComp.StartFollow(playerTransform);
                        }
                        else
                        {
                            if (shouldStartFollow)
                                npcComp.StartFollow(playerTransform);
                        }
                    }
                }
            }
            else
            {
                // Player not yet present — start a short coroutine to wait and then bind
                StartCoroutine(WaitAndBindPersistentFollower(id, persistentGO));
            }

            // Destroy any scene-local NPCs with the same id to avoid duplicates
            var allIds = UnityObject.FindObjectsByType<NPCIdentity>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            for (int i = 0; i < allIds.Length; i++)
            {
                var ident = allIds[i];
                if (ident == null) continue;
                if (ident.id != id) continue;

                var candidate = ident.gameObject;
                // if candidate is the persistent GO (or parented under it), skip
                if (candidate == persistentGO || candidate.transform.IsChildOf(persistentGO.transform)) continue;

                // If candidate is part of a scene root (not persistent), destroy it
                if (!IsGameObjectPersistent(candidate))
                {
                    Debug.Log($"[NPCFollowManager] Destroying scene-local duplicate for id='{id}' -> {candidate.name}");
                    Destroy(candidate);
                }
            }
        }
    }

    private IEnumerator WaitAndBindPersistentFollower(string id, GameObject persistentGO)
    {
        float elapsed = 0f;
        while (elapsed < PlayerWaitTimeout)
        {
            Transform playerTransform = FindPlayerTransform();
            if (playerTransform != null)
            {
                // Ensure persistent GO is active before teleport/rebind
                if (!persistentGO.activeSelf)
                {
                    try { persistentGO.SetActive(true); }
                    catch (Exception) { /* ignore */ }
                }

                // teleport then rebind
                bool shouldStartFollow = followingIds.Contains(id);
                if (use2DTopDown)
                    TeleportNearPlayer2D(persistentGO.transform, playerTransform, teleportOffsetDistance);
                else
                    TeleportIfFar3D(persistentGO.transform, playerTransform, teleportOffsetDistance);

                var npcComp = persistentGO.GetComponent<NPC>();
                if (npcComp != null)
                {
                    var rebindMethod = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform), typeof(bool) });
                    if (rebindMethod != null)
                    {
                        rebindMethod.Invoke(npcComp, new object[] { playerTransform, shouldStartFollow });
                    }
                    else
                    {
                        var rebindSingle = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform) });
                        if (rebindSingle != null)
                        {
                            rebindSingle.Invoke(npcComp, new object[] { playerTransform });
                            if (shouldStartFollow)
                                npcComp.StartFollow(playerTransform);
                        }
                        else
                        {
                            if (shouldStartFollow)
                                npcComp.StartFollow(playerTransform);
                        }
                    }

                    Debug.Log($"[NPCFollowManager] Bound persistent follower '{id}' to player after waiting {elapsed:F2}s");
                }
                yield break;
            }

            yield return new WaitForSecondsRealtime(PlayerPollInterval);
            elapsed += PlayerPollInterval;
        }

        Debug.LogWarning($"[NPCFollowManager] Timed out waiting for player to appear to bind persistent follower '{id}'.");
    }

    // ------------------ Public API -----------------------

    /// <summary>
    /// Start following and persist the follower across scenes.
    /// </summary>
    public void StartFollow(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        followingIds.Add(id);

        // If we already have a persistent follower for this id, ensure it's following and return
        if (persistentFollowers.TryGetValue(id, out GameObject pers) && pers != null)
        {
            var npc = pers.GetComponent<NPC>();
            if (npc != null)
            {
                Transform playerTransform = FindPlayerTransform();
                if (playerTransform != null)
                    npc.StartFollow(playerTransform);
            }
            return;
        }

        // Otherwise try to find an NPC instance in the current scene and make it persistent
        var npcInstance = FindNPCById(id);
        if (npcInstance != null)
        {
            // If the scene instance is inactive, activate it so Start/OnEnable run
            if (!npcInstance.gameObject.activeSelf)
            {
                try { npcInstance.gameObject.SetActive(true); }
                catch (Exception) { /* ignore */ }
            }

            MakePersistentFollower(id, npcInstance.gameObject);
            Transform playerTransform = FindPlayerTransform();
            if (playerTransform != null)
                npcInstance.StartFollow(playerTransform);
        }
        else
        {
            // No instance found now; it will be handled when/if it spawns or on next sceneLoaded
            Debug.Log($"[NPCFollowManager] StartFollow: no NPC instance found for id='{id}' right now. Will attach when it appears.");
        }
    }

    /// <summary>
    /// Stop following and remove persistent follower (if any).
    /// This version un-persists the follower and returns it to the active scene,
    /// then stops its follow behavior rather than destroying the GameObject.
    /// </summary>
    public void StopFollow(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        followingIds.Remove(id);

        // If we have a persistent follower for this id, "unpersist" it instead of destroying it.
        if (persistentFollowers.TryGetValue(id, out GameObject pers))
        {
            if (pers != null)
            {
                // Stop its NPC behaviour cleanly
                var npcComp = pers.GetComponent<NPC>();
                if (npcComp != null)
                {
                    try { npcComp.StopFollowing(); } catch (Exception) { /* ignore */ }
                }

                // Move the object back into the active scene so it becomes a normal scene GameObject.
                try
                {
                    var activeScene = SceneManager.GetActiveScene();
                    SceneManager.MoveGameObjectToScene(pers, activeScene);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NPCFollowManager] Could not move persistent follower '{pers.name}' back to active scene: {ex.Message}");
                }

                // Ensure it's active and unparented (so normal scene lifecycles apply)
                try
                {
                    pers.transform.SetParent(null);
                    if (!pers.activeSelf) pers.SetActive(true);
                }
                catch (Exception) { /* ignore */ }

                // Remove from our persistent map so we no longer treat it as DontDestroyOnLoad-managed
                persistentFollowers.Remove(id);

                // If you *do* want it destroyed instead of being returned to the scene, uncomment:
                // Destroy(pers);
            }

            return;
        }

        // If there isn't a persistent follower, try to find an instance in the current scene and stop it
        var npc = FindNPCById(id);
        if (npc != null)
        {
            try { npc.StopFollowing(); } catch (Exception) { /* ignore */ }
        }
    }

    public bool IsFollowing(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return followingIds.Contains(id);
    }

    public void TryStartFollowOnScenePublic(string id)
    {
        TryStartFollowOnScene(id);
    }

    // ------------------ Internal helpers --------------------

    private void TryStartFollowOnScene(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        // If we already made a persistent follower, ensure it is following
        if (persistentFollowers.TryGetValue(id, out GameObject pers) && pers != null)
        {
            var npcComp = pers.GetComponent<NPC>();
            if (npcComp != null)
            {
                Transform playerTransform = FindPlayerTransform();
                if (playerTransform != null)
                    npcComp.StartFollow(playerTransform);
            }
            return;
        }

        // Find an NPC instance in the scene
        var npc = FindNPCById(id);
        if (npc == null)
        {
            // Not in scene yet; will be retried later (sceneLoaded or NPC.Start)
            Debug.Log($"[NPCFollowManager] TryStartFollowOnScene: no NPC instance found for id='{id}'.");
            return;
        }

        // If the scene NPC is inactive, activate it so Start/OnEnable fire before making it persistent
        if (!npc.gameObject.activeSelf)
        {
            try { npc.gameObject.SetActive(true); }
            catch (Exception) { /* ignore */ }
        }

        // Make this instance persistent and start following
        MakePersistentFollower(id, npc.gameObject);

        Transform playerTransformNow = FindPlayerTransform();
        if (playerTransformNow == null)
        {
            Debug.LogWarning($"[NPCFollowManager] Player transform not found when starting follow for id='{id}'.");
            return;
        }

        // Teleport near player if necessary
        if (use2DTopDown)
            TeleportNearPlayer2D(npc.transform, playerTransformNow, teleportOffsetDistance);
        else
            TeleportIfFar3D(npc.transform, playerTransformNow, teleportOffsetDistance);

        npc.StartFollow(playerTransformNow);
    }

    /// <summary>
    /// Create a persistent follower from a scene instance (DontDestroyOnLoad) and ensure duplicates are removed.
    /// </summary>
    private void MakePersistentFollower(string id, GameObject npcGO)
    {
        if (npcGO == null) return;
        if (persistentFollowers.ContainsKey(id) && persistentFollowers[id] == npcGO) return;

        // Ensure the GO is active so its Start/OnEnable have run
        if (!npcGO.activeSelf)
        {
            try { npcGO.SetActive(true); }
            catch (Exception) { /* ignore */ }
        }

        // Un-parent from scene root to avoid being destroyed with the scene (optional)
        npcGO.transform.SetParent(null);

        DontDestroyOnLoad(npcGO);
        persistentFollowers[id] = npcGO;

        Debug.Log($"[NPCFollowManager] Made persistent follower for id='{id}' -> {npcGO.name}");

        // Destroy other scene-local duplicates immediately
        var allIds = UnityObject.FindObjectsByType<NPCIdentity>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        for (int i = 0; i < allIds.Length; i++)
        {
            var ident = allIds[i];
            if (ident == null) continue;
            if (ident.id != id) continue;

            var candidate = ident.gameObject;
            if (candidate == npcGO || candidate.transform.IsChildOf(npcGO.transform)) continue;

            if (!IsGameObjectPersistent(candidate))
            {
                Debug.Log($"[NPCFollowManager] Destroying duplicate '{candidate.name}' for id='{id}' after making persistent.");
                Destroy(candidate);
            }
        }
    }

    private NPC FindNPCById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var all = UnityObject.FindObjectsByType<NPCIdentity>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var ident = all[i];
            if (ident != null && ident.id == id)
            {
                var npc = ident.GetComponent<NPC>();
                if (!npc) npc = ident.GetComponentInParent<NPC>(true);
                if (npc) return npc;
            }
        }
        return null;
    }

    private Transform FindPlayerTransform()
    {
        // Prefer PixelCrushers.PlayerLocator if available
        var locatorType = Type.GetType("PixelCrushers.PlayerLocator, PixelCrushers.DialogueSystem.Core");
        if (locatorType != null)
        {
            var currentProp = locatorType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
            if (currentProp != null)
            {
                var cur = currentProp.GetValue(null, null) as Transform;
                if (cur != null) return cur;
            }
        }

        // --- NEW: Try to find any PlayerLocator-like type (including your own PlayerLocator) ---
        if (hookedPlayerLocatorType != null)
        {
            var currentProp = hookedPlayerLocatorType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (currentProp != null)
            {
                try
                {
                    var cur = currentProp.GetValue(null, null) as Transform;
                    if (cur != null) return cur;
                }
                catch (Exception) { /* ignore reflection errors */ }
            }
        }
        else
        {
            // If not hooked yet, try a quick scan for a "PlayerLocator" type and read its Current property
            var quickType = FindTypeByName("PlayerLocator");
            if (quickType != null)
            {
                var currentProp = quickType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (currentProp != null)
                {
                    try
                    {
                        var cur = currentProp.GetValue(null, null) as Transform;
                        if (cur != null)
                        {
                            // Cache the type for future fast lookup and also try to hook its OnChanged event
                            hookedPlayerLocatorType = quickType;
                            TryHookPlayerLocatorEvent(quickType);
                            return cur;
                        }
                    }
                    catch (Exception) { /* ignore reflection errors */ }
                }
            }
        }
        // -------------------------------------------------------------------------------

        // Tag lookup
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go) return go.transform;

        // Try to find a Player component (if you have one)
        var playerComp = FindObjectOfType<Player>();
        if (playerComp != null) return playerComp.transform;

        // Try to find by name
        go = GameObject.Find("Player");
        if (go) return go.transform;

        // Try a GameManager instance that might expose the player
        var gmType = Type.GetType("GameManager");
        if (gmType != null)
        {
            var gmInstanceProp = gmType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (gmInstanceProp != null)
            {
                var gmInstance = gmInstanceProp.GetValue(null, null);
                if (gmInstance != null)
                {
                    var playerProp = gmType.GetProperty("Player", BindingFlags.Instance | BindingFlags.Public) ??
                                     gmType.GetProperty("CurrentPlayer", BindingFlags.Instance | BindingFlags.Public);
                    if (playerProp != null)
                    {
                        var playerObj = playerProp.GetValue(gmInstance, null) as GameObject;
                        if (playerObj != null) return playerObj.transform;
                    }
                }
            }
        }

        return null;
    }

    private bool IsGameObjectPersistent(GameObject go)
    {
        if (go == null) return false;
        // A heuristic: if root has DontDestroyOnLoad applied it will be under special scene "DontDestroyOnLoad"
        return go.scene.name == "DontDestroyOnLoad";
    }

    // ------------------ Teleport helpers -----------------------

    private void TeleportIfFar3D(Transform npcTransform, Transform playerTransform, float minDistance)
    {
        if (npcTransform == null || playerTransform == null) return;
        float sqDist = (npcTransform.position - playerTransform.position).sqrMagnitude;
        if (sqDist > (minDistance * minDistance * 16f))
        {
            Vector3 offset = -playerTransform.forward;
            if (offset.sqrMagnitude < 0.0001f) offset = Vector3.back;
            offset = offset.normalized * minDistance;
            Vector3 target = playerTransform.position + offset;
            target.z = npcTransform.position.z;
            npcTransform.position = target;
        }
        else if (sqDist > (minDistance * minDistance))
        {
            Vector3 direction = (playerTransform.position - npcTransform.position).normalized;
            Vector3 target = playerTransform.position - direction * minDistance;
            target.z = npcTransform.position.z;
            npcTransform.position = target;
        }
    }

    private void TeleportNearPlayer2D(Transform npcTransform, Transform playerTransform, float offset)
    {
        if (npcTransform == null || playerTransform == null) return;

        Vector2 npcPos = npcTransform.position;
        Vector2 playerPos = playerTransform.position;
        float sqDist = (npcPos - playerPos).sqrMagnitude;

        float veryFarThreshold = (offset * 16f) * (offset * 16f);
        if (sqDist > veryFarThreshold)
        {
            Vector3 target = new Vector3(playerPos.x - offset, playerPos.y, npcTransform.position.z);
            npcTransform.position = target;
            return;
        }

        float nearThreshold = offset * offset;
        if (sqDist > nearThreshold)
        {
            Vector2 dir = (playerPos - npcPos).normalized;
            Vector3 target = new Vector3(playerPos.x - dir.x * offset, playerPos.y - dir.y * offset, npcTransform.position.z);
            npcTransform.position = target;
        }
    }

    // ------------------ NEW: Reflection helpers for PlayerLocator hooking --------------------

    // Try to find a type named "PlayerLocator" in any loaded assembly and, if found, attach to its OnChanged event.
    private void TryHookExternalPlayerLocator()
    {
        var t = FindTypeByName("PlayerLocator");
        if (t != null)
        {
            hookedPlayerLocatorType = t;
            TryHookPlayerLocatorEvent(t);
        }
        else
        {
            // not found now; we'll attempt again lazily on first FindPlayerTransform quick-check.
        }
    }

    // Find a type by short name across loaded assemblies
    private Type FindTypeByName(string shortName)
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                for (int i = 0; i < types.Length; i++)
                {
                    var tt = types[i];
                    if (tt == null) continue;
                    if (tt.Name == shortName) return tt;
                }
            }
        }
        catch (Exception) { /* ignore reflection errors */ }
        return null;
    }

    // Hook to a static event named "OnChanged" with signature Action<Transform> if present.
    private void TryHookPlayerLocatorEvent(Type locatorType)
    {
        if (locatorType == null) return;
        try
        {
            var ev = locatorType.GetEvent("OnChanged", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (ev == null) return;

            // Create a delegate to our handler method PlayerLocator_OnChanged(Transform)
            var handlerMethod = GetType().GetMethod(nameof(PlayerLocator_OnChanged), BindingFlags.Instance | BindingFlags.NonPublic);
            if (handlerMethod == null) return;

            var handlerDelegate = Delegate.CreateDelegate(ev.EventHandlerType, this, handlerMethod);
            ev.AddEventHandler(null, handlerDelegate);
            playerLocatorOnChangedDelegate = handlerDelegate;

            Debug.Log($"[NPCFollowManager] Subscribed to {locatorType.FullName}.OnChanged for faster rebinds.");
        }
        catch (Exception)
        {
            // ignore errors; subscription is optional
        }
    }

    // Called when a PlayerLocator-like type raises OnChanged(Transform newPlayer)
    private void PlayerLocator_OnChanged(Transform newPlayer)
    {
        if (newPlayer == null) return;

        // Rebind all persistent followers immediately (teleport + rebind)
        foreach (var kv in new List<KeyValuePair<string, GameObject>>(persistentFollowers))
        {
            var id = kv.Key;
            var persistentGO = kv.Value;
            if (persistentGO == null) continue;

            // Ensure active
            if (!persistentGO.activeSelf)
            {
                try { persistentGO.SetActive(true); }
                catch (Exception) { /* ignore */ }
            }

            if (use2DTopDown)
                TeleportNearPlayer2D(persistentGO.transform, newPlayer, teleportOffsetDistance);
            else
                TeleportIfFar3D(persistentGO.transform, newPlayer, teleportOffsetDistance);

            var npcComp = persistentGO.GetComponent<NPC>();
            if (npcComp != null)
            {
                var rebindMethod = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform), typeof(bool) });
                bool shouldStart = followingIds.Contains(id);
                if (rebindMethod != null)
                {
                    try { rebindMethod.Invoke(npcComp, new object[] { newPlayer, shouldStart }); }
                    catch (Exception) { /* ignore errors from target */ }
                }
                else
                {
                    // fallback
                    try
                    {
                        var rebindSingle = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform) });
                        if (rebindSingle != null)
                        {
                            rebindSingle.Invoke(npcComp, new object[] { newPlayer });
                            if (shouldStart) npcComp.StartFollow(newPlayer);
                        }
                        else if (shouldStart)
                        {
                            npcComp.StartFollow(newPlayer);
                        }
                    }
                    catch (Exception) { /* ignore */ }
                }
            }
        }
    }
    // ----------------------------------------------------------------------------------------------

    // ------------------ NEW: Public API for save/load --------------------

    /// <summary>
    /// Call this BEFORE saving the game to capture the current follow state.
    /// PixelCrushers or other save systems can call this if they provide a hook.
    /// </summary>
    public void OnBeforeSave()
    {
        SaveFollowStateToPrefs();
        Debug.Log("[NPCFollowManager] OnBeforeSave: saved followingIds count=" + followingIds.Count);
    }

    /// <summary>
    /// Call this AFTER loading a game to restore follow state and rebind followers to the player.
    /// PixelCrushers or other save systems can call this if they provide a hook.
    /// </summary>
    public void OnAfterLoad()
    {
        RestoreFollowStateFromPrefs();

        // If the saver put pending ids into the static set, merge them now too.
        if (PendingFollowingIds != null && PendingFollowingIds.Count > 0)
        {
            foreach (var id in PendingFollowingIds.ToList())
            {
                if (string.IsNullOrEmpty(id)) continue;
                followingIds.Add(id);
                TryStartFollowOnScene(id);
            }
            PendingFollowingIds.Clear();
        }

        StartCoroutine(RestoreFollowingAfterLoadCoroutine());
        Debug.Log("[NPCFollowManager] OnAfterLoad: restoring follow state for " + followingIds.Count + " ids");
    }

    // Coroutine that tries to reattach persistent or scene NPCs after load.
    private IEnumerator RestoreFollowingAfterLoadCoroutine()
    {
        // Wait a frame to let scene objects initialize (safe minimal delay)
        yield return null;

        // Try to find player and start follow for each id. Use scene attempts + persistent follower logic like TryStartFollowOnScene.
        foreach (var id in followingIds.ToList())
        {
            // If we already have a persistent follower, ensure it's bound to the current player now.
            if (persistentFollowers.TryGetValue(id, out GameObject pers) && pers != null)
            {
                // Ensure active
                if (!pers.activeSelf)
                {
                    try { pers.SetActive(true); }
                    catch (Exception) { /* ignore */ }
                }

                Transform playerTransform = FindPlayerTransform();
                if (playerTransform != null)
                {
                    // Teleport and rebind
                    if (use2DTopDown)
                        TeleportNearPlayer2D(pers.transform, playerTransform, teleportOffsetDistance);
                    else
                        TeleportIfFar3D(pers.transform, playerTransform, teleportOffsetDistance);

                    var npcComp = pers.GetComponent<NPC>();
                    if (npcComp != null)
                    {
                        var rebindMethod = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform), typeof(bool) });
                        if (rebindMethod != null)
                        {
                            rebindMethod.Invoke(npcComp, new object[] { playerTransform, true });
                        }
                        else
                        {
                            var rebindSingle = npcComp.GetType().GetMethod("RebindFollow", new Type[] { typeof(Transform) });
                            if (rebindSingle != null)
                            {
                                rebindSingle.Invoke(npcComp, new object[] { playerTransform });
                                npcComp.StartFollow(playerTransform);
                            }
                            else
                            {
                                npcComp.StartFollow(playerTransform);
                            }
                        }
                    }
                }
                else
                {
                    // If no player yet, fall back to TryStartFollowOnScene which will log/wait later
                    TryStartFollowOnScene(id);
                }
            }
            else
            {
                // No persisted follower yet in this session; try to find one in-scene and start following
                TryStartFollowOnScene(id);
            }
        }

        // Additionally, wait briefly and retry a single time for any ids that might have spawned a frame later.
        yield return new WaitForSecondsRealtime(0.05f);
        foreach (var id in followingIds.ToList())
        {
            if (!persistentFollowers.ContainsKey(id))
            {
                TryStartFollowOnScene(id);
            }
        }
    }

    // ------------------ NEW: PlayerPrefs (fallback) persistence --------------------

    [Serializable]
    private class FollowingIdsSerializable
    {
        public string[] ids;
    }

    private void SaveFollowStateToPrefs()
    {
        try
        {
            var arr = followingIds.ToArray();
            var wrapper = new FollowingIdsSerializable { ids = arr };
            var json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PlayerPrefsKey_FollowingIds, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NPCFollowManager] SaveFollowStateToPrefs failed: " + e.Message);
        }
    }

    private void RestoreFollowStateFromPrefs()
    {
        try
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey_FollowingIds)) return;
            var json = PlayerPrefs.GetString(PlayerPrefsKey_FollowingIds, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            var wrapper = JsonUtility.FromJson<FollowingIdsSerializable>(json);
            if (wrapper == null || wrapper.ids == null) return;
            followingIds.Clear();
            foreach (var s in wrapper.ids) if (!string.IsNullOrEmpty(s)) followingIds.Add(s);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NPCFollowManager] RestoreFollowStateFromPrefs failed: " + e.Message);
        }
    }

    // ------------------ NEW: Try auto-hooking save-system events (PixelCrushers etc) --------------------

    // Look for SaveSystemEvents type and hook its UnityEvent fields (onSaveStart/onSaveEnd/onLoadStart/onLoadEnd) on any instances found,
    // or hook static events if present. This is best-effort and optional.
    private void TryHookSaveSystemEvents()
    {
        try
        {
            // Try the well-known PixelCrushers type names first
            var candidateNames = new[] { "PixelCrushers.SaveSystemEvents", "SaveSystemEvents" };
            Type saveEventsType = null;
            foreach (var cn in candidateNames)
            {
                saveEventsType = FindTypeByFullOrShortName(cn);
                if (saveEventsType != null) break;
            }

            if (saveEventsType == null)
            {
                // no SaveSystemEvents type found — bail (we'll rely on public API calls)
                return;
            }

            // Common UnityEvent field names used by PixelCrushers' SaveSystemEvents
            var unityEventFieldNames = new[] { "onSaveStart", "onSaveEnd", "onLoadStart", "onLoadEnd", "onLoadEnd" };

            // For each field, if static UnityEvent -> add listener. If instance field -> find instances and add listener to each.
            foreach (var fldName in unityEventFieldNames)
            {
                var fi = saveEventsType.GetField(fldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if (fi == null) continue;

                // If static UnityEvent field
                if (fi.IsStatic)
                {
                    var ue = fi.GetValue(null) as UnityEvent;
                    if (ue != null)
                    {
                        // Choose action based on field name
                        if (fldName.IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0)
                            ue.AddListener(new UnityAction(OnBeforeSave));
                        else
                            ue.AddListener(new UnityAction(OnAfterLoad));
                        // We can't remove static UnityEvent listeners safely later via reflection (no reference), but it's acceptable
                    }
                }
                else
                {
                    // instance field: find any components in loaded scenes and add listeners
                    UnityEngine.Object[] instances = UnityEngine.Object.FindObjectsOfType(saveEventsType, true);
                    foreach (var inst in instances)
                    {
                        if (inst == null) continue;
                        var ue = fi.GetValue(inst) as UnityEvent;
                        if (ue != null)
                        {
                            UnityAction action = (fldName.IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0)
                                ? new UnityAction(OnBeforeSave)
                                : new UnityAction(OnAfterLoad);

                            ue.AddListener(action);
                            hookedUnityEventListeners.Add(Tuple.Create(fi, inst, action));
                            Debug.Log($"[NPCFollowManager] Subscribed to {saveEventsType.FullName}.{fldName} on instance {inst.name}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NPCFollowManager] TryHookSaveSystemEvents failed: {ex.Message}");
        }
    }

    // Helper: find type by full name or short name
    private Type FindTypeByFullOrShortName(string name)
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                for (int i = 0; i < types.Length; i++)
                {
                    var tt = types[i];
                    if (tt == null) continue;
                    if (tt.FullName == name || tt.Name == name) return tt;
                }
            }
        }
        catch { }
        return null;
    }

    // ---------------- existing implementation continues (end of class) ----------------
}
