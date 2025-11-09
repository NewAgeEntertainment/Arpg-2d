using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCinemachineBridge : MonoBehaviour
{
    [Header("Assign either single VCam OR two VCams (toggle by priority)")]
    [Tooltip("Drag a CinemachineCamera (CM3) or CinemachineVirtualCamera (CM2).")]
    public Component singleVCam;          // we resize orthographic size on toggle
    public Component miniVCam;            // lower priority when fullscreen is OFF
    public Component fullscreenVCam;      // higher priority when fullscreen is ON

    [Header("If using singleVCam, we’ll resize it")]
    public float orthoSizeMinimap = 8f;
    public float orthoSizeFullscreen = 35f;

    [Header("Auto-Follow")]
    [Tooltip("Use PlayerLocator.Current automatically when it changes.")]
    public bool autoBindPlayerLocator = true;
    [Tooltip("Manual override. If set, this wins over PlayerLocator.")]
    public Transform followOverride;

    [Header("Robust binding")]
    [Tooltip("How often to re-check for the player if none is bound yet.")]
    public float retryInterval = 0.25f;
    [Tooltip("Automatically add a CinemachineBrain on this Camera if missing.")]
    public bool ensureBrainOnCamera = true;

    private bool _isFullscreen;
    private Transform _currentFollow;
    private Coroutine _bindLoop;

    private void Awake()
    {
        // Make sure this camera can drive Cinemachine VCams (CM2 or CM3)
        if (ensureBrainOnCamera && GetComponent("CinemachineBrain") == null)
        {
            var brainType = System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
            if (brainType != null) gameObject.AddComponent(brainType);
        }
    }

    private void OnEnable()
    {
        if (autoBindPlayerLocator)
        {
            // Subscribe to PlayerLocator changes (your provided class)
            PlayerLocator.OnChanged += HandlePlayerLocatorChanged;
        }

        // Small polling loop to catch late-spawned players or scene swaps
        if (_bindLoop == null) _bindLoop = StartCoroutine(BindLoop());
    }

    private void OnDisable()
    {
        if (autoBindPlayerLocator)
        {
            PlayerLocator.OnChanged -= HandlePlayerLocatorChanged;
        }

        if (_bindLoop != null) { StopCoroutine(_bindLoop); _bindLoop = null; }
    }

    private System.Collections.IEnumerator BindLoop()
    {
        // Give other announcers a frame to initialize
        yield return null;

        // Initial bind
        RefreshFollow(true);

        // Keep checking occasionally in case the player swaps/reloads
        while (true)
        {
            var resolved = ResolveFollowTarget();
            if (resolved != _currentFollow && resolved != null)
                ApplyFollow(resolved);

            yield return new WaitForSeconds(retryInterval);
        }
    }

    private void Start()
    {
        // Immediate bind in case everything already exists
        RefreshFollow(true);

        // Set initial lens for single vcam
        if (singleVCam != null)
            SetLensSize(singleVCam, orthoSizeMinimap);
    }

    // ========================= Public API =========================

    /// <summary>Manually re-resolve and bind the follow target.</summary>
    public void RefreshFollow(bool snapLens = false)
    {
        var follow = ResolveFollowTarget();
        ApplyFollow(follow);

        if (snapLens && singleVCam != null)
            SetLensSize(singleVCam, _isFullscreen ? orthoSizeFullscreen : orthoSizeMinimap);
    }

    /// <summary>Force a specific follow target (e.g., when you have the player Transform).</summary>
    public void SetFollowTarget(Transform t, bool snapLens = false)
    {
        followOverride = t;
        ApplyFollow(t);

        if (snapLens && singleVCam != null)
            SetLensSize(singleVCam, _isFullscreen ? orthoSizeFullscreen : orthoSizeMinimap);
    }

    /// <summary>Called by MinimapUI when toggling mini/full.</summary>
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

    // ========================= Internals ==========================

    private void HandlePlayerLocatorChanged(Transform newPlayer)
    {
        if (!autoBindPlayerLocator) return;
        if (newPlayer != null) SetFollowTarget(newPlayer, snapLens: true);
    }

    private void ApplyFollow(Transform follow)
    {
        _currentFollow = follow;
        SetFollow(singleVCam, follow);
        SetFollow(miniVCam, follow);
        SetFollow(fullscreenVCam, follow);
    }

    private Transform ResolveFollowTarget()
    {
        // 1) Manual override wins
        if (followOverride != null) return followOverride;

        // 2) Your PlayerLocator
        if (autoBindPlayerLocator && PlayerLocator.Current != null)
            return PlayerLocator.Current;

        // 3) Fallbacks (nice to have)
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        var named = GameObject.Find("Player");
        if (named != null) return named.transform;

        return null;
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
}
