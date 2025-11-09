using UnityEngine;

[CreateAssetMenu(menuName = "Minimap/Scene Config")]
public class MinimapSceneConfig : ScriptableObject
{
    public float orthographicSize = 20f;
    public LayerMask cullingMask;
    public Transform confiner;   // e.g., bounding object for the minimap camera
    public bool showFog = true;
}

public class MinimapSceneAdapter : MonoBehaviour
{
    public MinimapSceneConfig config;

    void Start()
    {
        var mm = MinimapPersistent.Instance;   // your persistent root
        if (mm == null || config == null) return;

        var cam = mm.minimapCamera;
        if (cam != null)
        {
            cam.orthographicSize = config.orthographicSize;
            cam.cullingMask = config.cullingMask;
        }

        // If you use a confiner/limits component on the minimap camera, set it here.
        // Example: GetComponentOnYourMinimapConfiner().SetBounds(config.confiner);

        // Fog toggle
        if (mm.minimapUI != null && mm.minimapUI.fogImage != null)
            mm.minimapUI.fogImage.enabled = config.showFog;

        // Re-hook follow (useful when scenes load additively)
        mm.TryHookFollowTarget();
    }
}
