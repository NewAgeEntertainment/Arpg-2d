using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCinemachineBridge : MonoBehaviour
{
    [Header("Assign either single VCam OR two VCams (toggle by priority)")]
    [Tooltip("Drag a CinemachineCamera (CM3) or CinemachineVirtualCamera (CM2).")]
    public Component singleVCam;          // resize orthographic size on toggle
    public Component miniVCam;            // lower priority when fullscreen is OFF
    public Component fullscreenVCam;      // higher priority when fullscreen is ON

    [Header("If using singleVCam, we’ll resize it")]
    public float orthoSizeMinimap = 8f;
    public float orthoSizeFullscreen = 35f;

    [Header("Auto-Follow")]
    public bool autoBindPlayerLocator = true;  // uses PlayerLocator.Current
    public Transform followOverride;           // optional manual follow

    [Header("Robust binding (added)")]
    [Tooltip("How often to re-check for the player if none is bound yet.")]
    public float retryInterval = 0.25f;
    [Tooltip("Automatically add a CinemachineBrain on this Camera if missing.")]
    public bool ensureBrainOnCamera = true;

    private bool _isFullscreen;
    private Transform _currentFollow;
    private Coroutine _bindLoop;

    private void Awake()
    {
        // Optional: make sure this camera can drive Cinemachine VCams
        if (ensureBrainOnCamera && GetComponent("CinemachineBrain") == null)
        {
            // AddComponent by string avoids direct CM namespace dependency
            gameObject.AddComponent(System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine"));
        }
    }

    private void OnEnable()
    {
        // Listen for player changes if your PlayerLocator exposes OnChanged
        try { PlayerLocator.OnChanged += HandlePlayerChanged; } catch { /* safe if event missing */ }

        // Start a small polling loop to catch late-spawned players
        if (_bindLoop == null) _bindLoop = StartCoroutine(BindLoop());
    }

    private void OnDisable()
    {
        try { PlayerLocator.OnChanged -= HandlePlayerChanged; } catch { }
        if (_bindLoop != null) { StopCoroutine(_bindLoop); _bindLoop = null; }
    }

    private System.Collections.IEnumerator BindLoop()
    {
        // Give other announcers (like PlayerLocator) a frame to set up
        yield return null;

        // Initial bind (keeps your original Start behavior but more robust)
        RefreshFollow(true);

        // Keep checking occasionally in case the player swaps/reloads
        while (true)
        {
            // If we lost the target or it changed, rebind
            var resolved = ResolveFollowTarget();
            if (resolved != _currentFollow && resolved != null)
                ApplyFollow(resolved);

            yield return new WaitForSeconds(retryInterval);
        }
    }

    private void Start()
    {
        // Keep your original start-time bind for immediate cases
        RefreshFollow(true);

        if (singleVCam != null)
            SetLensSize(singleVCam, orthoSizeMinimap);
    }

    /// <summary>Public: manually force a rebind (e.g., after spawning player).</summary>
    public void RefreshFollow(bool snapLens = false)
    {
        var follow = ResolveFollowTarget();
        ApplyFollow(follow);

        if (snapLens && singleVCam != null)
            SetLensSize(singleVCam, _isFullscreen ? orthoSizeFullscreen : orthoSizeMinimap);
    }

    private void HandlePlayerChanged(Transform newPlayer)
    {
        if (!autoBindPlayerLocator || newPlayer == null) return;
        ApplyFollow(newPlayer);
    }

    private void ApplyFollow(Transform follow)
    {
        _currentFollow = follow;
        SetFollow(singleVCam, follow);
        SetFollow(miniVCam, follow);
        SetFollow(fullscreenVCam, follow);
    }

    public void SetFullscreen(bool full)
    {
        _isFullscreen = full;

        if (singleVCam != null)
        {
            SetLensSize(singleVCam, full ? orthoSizeFullscreen : orthoSizeMinimap);
            return;
        }

        // Two-VCam approach: swap by priority
        SetPriority(miniVCam, full ? 0 : 10);
        SetPriority(fullscreenVCam, full ? 10 : 0);
    }

    // --------- Reflection helpers (CM2 & CM3 compatible) ----------
    private static void SetFollow(Component vcam, Transform t)
    {
        if (vcam == null) return;
        var prop = vcam.GetType().GetProperty("Follow");
        if (prop != null && prop.CanWrite) prop.SetValue(vcam, t);
    }

    private static void SetPriority(Component vcam, int p)
    {
        if (vcam == null) return;
        var prop = vcam.GetType().GetProperty("Priority");
        if (prop != null && prop.CanWrite) prop.SetValue(vcam, p);
    }

    private static void SetLensSize(Component vcam, float size)
    {
        if (vcam == null) return;
        var t = vcam.GetType();

        // CM3: property "Lens" (struct) with property "OrthographicSize"
        var lensProp = t.GetProperty("Lens");
        if (lensProp != null && lensProp.CanRead && lensProp.CanWrite)
        {
            var lensObj = lensProp.GetValue(vcam, null);
            if (lensObj != null)
            {
                var lsType = lensObj.GetType();
                var orthoP = lsType.GetProperty("OrthographicSize");
                if (orthoP != null && orthoP.CanWrite)
                {
                    orthoP.SetValue(lensObj, size, null);
                    lensProp.SetValue(vcam, lensObj, null); // assign the boxed struct back
                    return;
                }
            }
        }

        // CM2: field "m_Lens" (struct) with property "OrthographicSize"
        var lensField = t.GetField("m_Lens");
        if (lensField != null)
        {
            var lensObj = lensField.GetValue(vcam);
            if (lensObj != null)
            {
                var lsType = lensObj.GetType();
                var orthoP = lsType.GetProperty("OrthographicSize");
                if (orthoP != null && orthoP.CanWrite)
                {
                    orthoP.SetValue(lensObj, size, null);
                    lensField.SetValue(vcam, lensObj); // reassign struct
                    return;
                }
            }
        }

        // Fallback (rare)
        var direct = t.GetProperty("OrthographicSize");
        if (direct != null && direct.CanWrite)
            direct.SetValue(vcam, size, null);
    }

    private Transform ResolveFollowTarget()
    {
        if (followOverride != null) return followOverride;
        if (autoBindPlayerLocator && PlayerLocator.Current != null) return PlayerLocator.Current;

        // Fallbacks if PlayerLocator isn't ready/available
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        var named = GameObject.Find("Player");
        if (named != null) return named.transform;

        var playerComp = FindObjectOfType<Player>();
        if (playerComp != null) return playerComp.transform;

        return null;
    }
}
