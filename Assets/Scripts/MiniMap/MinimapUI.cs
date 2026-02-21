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

    [Header("Mini Layout")]
    public Vector2 miniAnchorMin = new Vector2(0.75f, 0.75f);
    public Vector2 miniAnchorMax = new Vector2(0.98f, 0.98f);

    public enum MiniLayoutMode { PercentOfScreen, FixedPixels }

    [Header("Mini Layout Mode")]
    public MiniLayoutMode miniLayoutMode = MiniLayoutMode.FixedPixels;

    [Tooltip("Used when MiniLayoutMode = FixedPixels")]
    public Vector2 miniSizePx = new Vector2(260, 260);

    [Tooltip("Margin from the top-right in pixels when FixedPixels")]
    public Vector2 miniMarginPx = new Vector2(16, 16);

    [Header("Fog Visuals")]
    [Tooltip("Tint color for undiscovered. Alpha is ignored because fogTexture alpha drives transparency.")]
    public Color fogTint = new Color(0, 0, 0, 0.85f);

    [Header("Optional Cinemachine Bridge (minimap only)")]
    public MinimapCinemachineBridge cineBridge;

    // ─────────────────────────────────────────────
    // SexyTime integration
    [Header("SexyTime Integration")]
    [Tooltip("If true, minimap hides automatically whenever SexyTimeLogic.isSexyTimeGoingOn is true.")]
    [SerializeField] private bool disableDuringSexyTime = true;

    private bool _wasSexyTime;
    private bool _wasVisibleBeforeSexy;
    // ─────────────────────────────────────────────

    private void Start()
    {
        ApplyFogTexture();
        ApplyMiniLayout();

        // Ensure minimap camera/bridge are set to minimap mode (no fullscreen exists now)
        if (cineBridge != null) cineBridge.RefreshFollow(snapLens: true);
    }

    private void Update()
    {
        HandleSexyTimeAutoHide();
        // No fullscreen input, ever.
    }

    // Expose this so persistent controller can call it:
    public void ReapplyFogTexture()
    {
        if (fogImage == null) return;
        if (MinimapFog.Instance == null) return;

        fogImage.texture = MinimapFog.Instance.fogTexture;
        fogImage.color = fogTint;
    }

    private void ApplyMiniLayout()
    {
        if (container == null) return;

        if (miniLayoutMode == MiniLayoutMode.PercentOfScreen)
        {
            container.anchorMin = miniAnchorMin;
            container.anchorMax = miniAnchorMax;
            container.pivot = new Vector2(0.5f, 0.5f);
            container.offsetMin = container.offsetMax = Vector2.zero;
            container.anchoredPosition = Vector2.zero;
        }
        else // FixedPixels
        {
            container.anchorMin = container.anchorMax = new Vector2(1f, 1f); // top-right
            container.pivot = new Vector2(1f, 1f);
            container.sizeDelta = miniSizePx;
            container.anchoredPosition = new Vector2(-miniMarginPx.x, -miniMarginPx.y);
        }

        // If your MinimapCameraFollow had fullscreen zoom logic, make sure it stays in minimap zoom:
        // (If MinimapCameraFollow requires a call, expose a SetMinimap() in that script instead.)
        // For now we do nothing here.
    }

    private void ApplyFogTexture()
    {
        if (fogImage == null) return;
        if (MinimapFog.Instance == null) return;

        fogImage.texture = MinimapFog.Instance.fogTexture;
        fogImage.color = fogTint;
    }

    // ─────────────────────────────────────────────
    // SexyTime auto-hide / restore
    private void HandleSexyTimeAutoHide()
    {
        if (!disableDuringSexyTime)
            return;

        bool sexyNow = SexyTimeLogic.isSexyTimeGoingOn;

        if (sexyNow == _wasSexyTime)
            return;

        _wasSexyTime = sexyNow;

        if (sexyNow)
        {
            _wasVisibleBeforeSexy = container != null && container.gameObject.activeSelf;

            if (container != null)
                container.gameObject.SetActive(false);

            if (minimapCamera != null)
                minimapCamera.enabled = false;
        }
        else
        {
            if (container != null)
                container.gameObject.SetActive(_wasVisibleBeforeSexy);

            if (minimapCamera != null)
                minimapCamera.enabled = _wasVisibleBeforeSexy;

            if (_wasVisibleBeforeSexy)
            {
                ApplyMiniLayout();
                if (cineBridge != null) cineBridge.RefreshFollow(snapLens: true);
            }
        }
    }
    // ─────────────────────────────────────────────
}
