using UnityEngine;

[CreateAssetMenu(menuName = "Minimap/Settings", fileName = "MinimapSettings")]
public class MinimapSettings : ScriptableObject
{
    [Header("World Bounds (orthographic 2D or top-down)")]
    public Vector2 worldMin = new Vector2(-50, -50);
    public Vector2 worldMax = new Vector2(50, 50);

    [Header("Fog Mask Resolution")]
    [Tooltip("Pixels for the fog mask width/height. Higher = sharper edges, more cost.")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    [Header("Reveal")]
    [Tooltip("Default reveal radius in world units if not specified by a revealer.")]
    public float defaultRevealRadius = 4f;

    [Header("Persistence")]
    public bool saveToPlayerPrefs = true;
    public string playerPrefsKey = "Minimap_FogMask_v1";
}
