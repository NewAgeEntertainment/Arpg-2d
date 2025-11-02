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

    private bool _isFullscreen;

    private void Start()
    {
        var follow = ResolveFollowTarget();
        SetFollow(singleVCam, follow);
        SetFollow(miniVCam, follow);
        SetFollow(fullscreenVCam, follow);

        if (singleVCam != null)
            SetLensSize(singleVCam, orthoSizeMinimap);
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
        if (!autoBindPlayerLocator) return null;
        return PlayerLocator.Current;
    }
}
