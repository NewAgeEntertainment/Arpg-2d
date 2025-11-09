// MinimapAutoTrack.cs  — attach to your MINIMAP VCam object
using UnityEngine;
using System;

public class MinimapAutoTrack : MonoBehaviour
{
    [Tooltip("If set, this wins over PlayerLocator.Current.")]
    public Transform overrideTarget;

    [Tooltip("Add a Position Composer module at runtime if missing (needed for CM3 to move).")]
    public bool ensurePositionComposer = true;

    private Component _vcam; // works for CM2 or CM3

    void Awake()
    {
        // Grab the vcam component on this object (CM3 CinemachineCamera, or CM2 CinemachineVirtualCamera)
        _vcam = GetComponent<Component>();
        TryEnsureComposer();
    }

    void OnEnable()
    {
        PlayerLocator.OnChanged += HandlePlayerChanged;
        HandlePlayerChanged(PlayerLocator.Current); // bind immediately if available
    }

    void OnDisable()
    {
        PlayerLocator.OnChanged -= HandlePlayerChanged;
    }

    void HandlePlayerChanged(Transform t)
    {
        var target = overrideTarget != null ? overrideTarget : t;
        if (target == null || _vcam == null) return;
        SetTargetOnVcam(_vcam, target);
    }

    void TryEnsureComposer()
    {
        if (!ensurePositionComposer) return;

        // CM3 module: Cinemachine.PositionComposer (adds the “Position Control” so the vcam actually tracks)
        var composerType = Type.GetType("Cinemachine.PositionComposer, Cinemachine");
        if (composerType != null && GetComponent(composerType) == null)
        {
            gameObject.AddComponent(composerType);
        }
    }

    // Works for CM3 (Target property) and CM2 (Follow property), plus pushes target into modules that expose "Target".
    static void SetTargetOnVcam(Component vcam, Transform target)
    {
        var t = vcam.GetType();

        // CM3 root Target
        var targetProp = t.GetProperty("Target");
        if (targetProp != null && targetProp.CanWrite) targetProp.SetValue(vcam, target);

        // CM2 Follow
        var followProp = t.GetProperty("Follow");
        if (followProp != null && followProp.CanWrite) followProp.SetValue(vcam, target);

        // Also set on any procedural modules that expose a "Target" property (e.g., PositionComposer)
        foreach (var c in vcam.GetComponents<Component>())
        {
            if (c == null) continue;
            var tp = c.GetType().GetProperty("Target");
            if (tp != null && tp.CanWrite) tp.SetValue(c, target);
        }
    }
}
