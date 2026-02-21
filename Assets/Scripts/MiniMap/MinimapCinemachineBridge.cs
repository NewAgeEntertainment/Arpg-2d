using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCinemachineBridge : MonoBehaviour
{
    [Header("Minimap VCam (single only)")]
    [Tooltip("Drag a CinemachineCamera (CM3) or CinemachineVirtualCamera (CM2).")]
    public Component minimapVCam;

    [Header("Minimap Lens")]
    public float orthoSizeMinimap = 8f;

    [Header("Auto-Follow")]
    public bool autoBindPlayerLocator = true;
    public Transform followOverride;

    [Header("Robust binding")]
    public float retryInterval = 0.25f;
    public bool ensureBrainOnCamera = true;

    private Transform _currentFollow;
    private Coroutine _bindLoop;

    private void Awake()
    {
        // Ensure this Camera can drive Cinemachine VCams (CM2/CM3)
        if (ensureBrainOnCamera && GetComponent("CinemachineBrain") == null)
        {
            var brainType = System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
            if (brainType != null) gameObject.AddComponent(brainType);
        }
    }

    private void OnEnable()
    {
        if (autoBindPlayerLocator)
            PlayerLocator.OnChanged += HandlePlayerLocatorChanged;

        if (_bindLoop == null) _bindLoop = StartCoroutine(BindLoop());
    }

    private void OnDisable()
    {
        if (autoBindPlayerLocator)
            PlayerLocator.OnChanged -= HandlePlayerLocatorChanged;

        if (_bindLoop != null) { StopCoroutine(_bindLoop); _bindLoop = null; }
    }

    private System.Collections.IEnumerator BindLoop()
    {
        yield return null;

        RefreshFollow(snapLens: true);

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
        RefreshFollow(snapLens: true);

        // Always minimap size (no fullscreen)
        if (minimapVCam != null)
            SetLensSize(minimapVCam, orthoSizeMinimap);

        // Optional safety: keep minimap vcam priority high so it always wins
        SetPriority(minimapVCam, 10);
    }

    // ------------------- Public API (minimap only) -------------------

    public void RefreshFollow(bool snapLens = false)
    {
        var follow = ResolveFollowTarget();
        ApplyFollow(follow);

        if (snapLens && minimapVCam != null)
            SetLensSize(minimapVCam, orthoSizeMinimap);
    }

    public void SetFollowTarget(Transform t, bool snapLens = false)
    {
        followOverride = t;
        ApplyFollow(t);

        if (snapLens && minimapVCam != null)
            SetLensSize(minimapVCam, orthoSizeMinimap);
    }

    // ------------------------- Internals -----------------------------

    private void HandlePlayerLocatorChanged(Transform newPlayer)
    {
        if (!autoBindPlayerLocator) return;
        if (newPlayer != null) SetFollowTarget(newPlayer, snapLens: true);
    }

    private void ApplyFollow(Transform follow)
    {
        _currentFollow = follow;
        SetFollow(minimapVCam, follow);
    }

    private Transform ResolveFollowTarget()
    {
        if (followOverride != null) return followOverride;

        if (autoBindPlayerLocator && PlayerLocator.Current != null)
            return PlayerLocator.Current;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        var named = GameObject.Find("Player");
        if (named != null) return named.transform;

        return null;
    }

    // ---------------- Reflection helpers (CM2 & CM3) -----------------

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
                    lensProp.SetValue(vcam, lensObj, null);
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
                    lensField.SetValue(vcam, lensObj);
                    return;
                }
            }
        }

        // Fallback
        var direct = t.GetProperty("OrthographicSize");
        if (direct != null && direct.CanWrite)
            direct.SetValue(vcam, size, null);
    }
}
