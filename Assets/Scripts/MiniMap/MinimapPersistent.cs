using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;


#if CINEMACHINE
#endif

#if PIXEL_CRUSHERS
using PixelCrushers;
#endif

/// <summary>
/// Keep one minimap across scenes and re-hook its follow target after scene changes or save loads,
/// without requiring setter methods on your follow scripts.
/// </summary>
public class MinimapPersistent : MonoBehaviour
{
    public static MinimapPersistent Instance;

    [Header("Core")]
    [Tooltip("Your MinimapUI component (same object or child).")]
    public MinimapUI minimapUI;

    [Tooltip("The minimap camera used to render the map (optional; only for convenience).")]
    public Camera minimapCamera;

    [Header("Follow (optional refs)")]
    [Tooltip("If you use a simple follow script, drag it here (no code changes needed).")]
    public MonoBehaviour cameraFollow;               // e.g., MinimapCameraFollow (any class)

    [Tooltip("If you use a Cinemachine bridge/holder, drag it here (no code changes needed).")]
    public MonoBehaviour cineBridge;                 // e.g., MinimapCinemachineBridge (any class)

    [Header("Lookup")]
    [Tooltip("Tag used to locate the player/root follow target in each scene.")]
    public string playerTag = "Player";

    [Tooltip("Optional child under the player to follow (e.g., 'Body'). Leave empty to follow the root.")]
    public string childFollowName = "";

    [Header("Retry")]
    [Tooltip("How long to keep trying to hook a follow target after a load/spawn (unscaled time).")]
    public float retargetTimeout = 2f;

    [Tooltip("How often to retry while waiting for the player to exist (unscaled time).")]
    public float retargetInterval = 0.1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (minimapUI == null) minimapUI = GetComponent<MinimapUI>();
        if (minimapCamera == null && minimapUI != null) minimapCamera = minimapUI.minimapCamera;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

#if PIXEL_CRUSHERS
        SaveSystem.onGameLoaded += OnGameLoaded;
        SaveSystem.beforeSceneChange += OnBeforeSceneChange;
#endif
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

#if PIXEL_CRUSHERS
        SaveSystem.onGameLoaded -= OnGameLoaded;
        SaveSystem.beforeSceneChange -= OnBeforeSceneChange;
#endif
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        ReapplyFogIfPossible();
        StartCoroutine(RetargetRoutine());
    }

#if PIXEL_CRUSHERS
    void OnBeforeSceneChange()
    {
        // Optional: mute/minimize transitions, if desired.
    }

    void OnGameLoaded(int slot)
    {
        // After a saved game is loaded, objects may spawn a few frames later.
        ReapplyFogIfPossible();
        StartCoroutine(RetargetRoutine());
    }
#endif

    void ReapplyFogIfPossible()
    {
        // Call MinimapUI.ReapplyFogTexture() if the user added it; otherwise ignore.
        if (minimapUI == null) return;

        var m = typeof(MinimapUI).GetMethod("ReapplyFogTexture", BindingFlags.Public | BindingFlags.Instance);
        if (m != null) m.Invoke(minimapUI, null);
    }

    IEnumerator RetargetRoutine()
    {
        float deadline = Time.unscaledTime + retargetTimeout;
        while (Time.unscaledTime < deadline)
        {
            if (TryHookFollowTarget()) yield break;
            yield return new WaitForSecondsRealtime(retargetInterval);
        }
        // One last attempt
        TryHookFollowTarget();
    }

    public bool TryHookFollowTarget()
    {
        Transform follow = ResolvePlayerTransform();
        if (follow == null) return false;

        // Prefer Cinemachine if present
        if (TrySetCinemachineFollow(follow)) return true;

        // Fallback: non-Cinemachine follow script (any class)
        if (TrySetPlainFollow(cameraFollow, follow)) return true;

        // As a last resort: search on the same GameObject for something that looks like a follow script
        if (cameraFollow == null)
        {
            var anyFollow = GetComponentInChildren<MonoBehaviour>(true);
            if (TrySetPlainFollow(anyFollow, follow)) return true;
        }

        return false;
    }

    Transform ResolvePlayerTransform()
    {
#if PIXEL_CRUSHERS
        // If your PixelCrushers setup exposes a player Transform, prefer it.
        if (SaveSystem.player != null) // Some templates expose this. If not, it just stays null.
        {
            var t = SaveSystem.player.transform;
            if (!string.IsNullOrEmpty(childFollowName))
            {
                var child = t.Find(childFollowName);
                if (child != null) return child;
            }
            return t;
        }
#endif
        // Default: find by tag
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go == null) return null;

        var root = go.transform;
        if (!string.IsNullOrEmpty(childFollowName))
        {
            var child = root.Find(childFollowName);
            if (child != null) return child;
        }
        return root;
    }

    // ---------- Cinemachine path ----------
    bool TrySetCinemachineFollow(Transform follow)
    {
#if CINEMACHINE
        CinemachineCamera vcam = null;

        // 1) If cineBridge is assigned, look under it.
        if (cineBridge != null)
            vcam = cineBridge.GetComponentInChildren<CinemachineCamera>(true);

        // 2) If not found, try under the minimapUI root.
        if (vcam == null && minimapUI != null)
            vcam = minimapUI.GetComponentInChildren<CinemachineCamera>(true);

        // 3) As a final fallback, search under this persistent root.
        if (vcam == null)
            vcam = GetComponentInChildren<CinemachineCamera>(true);

        if (vcam != null)
        {
            vcam.Follow = follow;
            return true;
        }
#endif
        return false;
    }

    // ---------- Non-Cinemachine path (no code changes needed in your scripts) ----------
    bool TrySetPlainFollow(MonoBehaviour followScript, Transform follow)
    {
        if (followScript == null || follow == null) return false;

        var t = followScript.GetType();

        // Try a SetTarget(Transform) method if it exists
        var setTargetMethod = t.GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setTargetMethod != null && setTargetMethod.GetParameters().Length == 1 &&
            setTargetMethod.GetParameters()[0].ParameterType == typeof(Transform))
        {
            setTargetMethod.Invoke(followScript, new object[] { follow });
            return true;
        }

        // Try common field names: target, followTarget, follow
        string[] fieldNames = { "target", "followTarget", "follow" };
        foreach (var fname in fieldNames)
        {
            var f = t.GetField(fname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && typeof(Transform).IsAssignableFrom(f.FieldType))
            {
                f.SetValue(followScript, follow);
                return true;
            }
        }

        // Try common property names: Target, FollowTarget, Follow
        string[] propNames = { "Target", "FollowTarget", "Follow" };
        foreach (var pname in propNames)
        {
            var p = t.GetProperty(pname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite && typeof(Transform).IsAssignableFrom(p.PropertyType))
            {
                p.SetValue(followScript, follow, null);
                return true;
            }
        }

        return false;
    }
}
