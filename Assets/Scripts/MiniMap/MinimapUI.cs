using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform container;    // panel that holds the map
    public RawImage mapImage;          // shows the camera RenderTexture
    public RawImage fogImage;          // shows the fogTexture on top (black with alpha)

    [Header("Targets")]
    public Camera minimapCamera;
    public MinimapCameraFollow cameraFollow;

    [Header("Layouts")]
    public Vector2 miniAnchorMin = new Vector2(0.75f, 0.75f);
    public Vector2 miniAnchorMax = new Vector2(0.98f, 0.98f);
    public Vector2 fullAnchorMin = new Vector2(0.0f, 0.0f);
    public Vector2 fullAnchorMax = new Vector2(1.0f, 1.0f);

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.M;

    [Header("Fog Visuals")]
    [Tooltip("Tint color for undiscovered. Alpha is ignored because fogTexture alpha drives transparency.")]
    public Color fogTint = new Color(0, 0, 0, 0.85f);

    public MinimapCinemachineBridge cineBridge;

    private bool _isFullscreen;

    private void Start()
    {
        ApplyFogTexture();
        ApplyLayout(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleFullscreen();
    }

    public void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;
        ApplyLayout(_isFullscreen);

        if (cineBridge != null)
            cineBridge.SetFullscreen(_isFullscreen);
        else if (cameraFollow != null)
            cameraFollow.SetFullscreen(_isFullscreen);
    }

    private void ApplyLayout(bool full)
    {
        if (container != null)
        {
            container.anchorMin = full ? fullAnchorMin : miniAnchorMin;
            container.anchorMax = full ? fullAnchorMax : miniAnchorMax;
            container.offsetMin = container.offsetMax = Vector2.zero;
        }

        if (cameraFollow != null)
            cameraFollow.SetFullscreen(full);
    }

    private void ApplyFogTexture()
    {
        if (fogImage == null) return;
        if (MinimapFog.Instance == null) return;

        fogImage.texture = MinimapFog.Instance.fogTexture;
        fogImage.color = fogTint;
        // Default UI material respects the texture alpha = perfect.
    }
}
