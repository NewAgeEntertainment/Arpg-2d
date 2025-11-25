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

    public enum MiniLayoutMode { PercentOfScreen, FixedPixels }
    [Header("Mini Layout Mode")]
    public MiniLayoutMode miniLayoutMode = MiniLayoutMode.FixedPixels;

    [Tooltip("Used when MiniLayoutMode = FixedPixels")]
    public Vector2 miniSizePx = new Vector2(260, 260);

    [Tooltip("Margin from the top-right in pixels when FixedPixels")]
    public Vector2 miniMarginPx = new Vector2(16, 16);


    public bool IsFullscreen => _isFullscreen;


    [Header("Input")]
    public KeyCode toggleKey = KeyCode.M;

    [Header("Fog Visuals")]
    [Tooltip("Tint color for undiscovered. Alpha is ignored because fogTexture alpha drives transparency.")]
    public Color fogTint = new Color(0, 0, 0, 0.85f);

    public MinimapCinemachineBridge cineBridge;

    private bool _isFullscreen;

    // ─────────────────────────────────────────────
    // SexyTime integration
    [Header("SexyTime Integration")]
    [Tooltip("If true, minimap hides automatically whenever SexyTimeLogic.isSexyTimeGoingOn is true.")]
    [SerializeField] private bool disableDuringSexyTime = true;

    private bool _wasSexyTime;               // last frame's SexyTime state
    private bool _wasVisibleBeforeSexy;      // did we have the minimap visible?
    private bool _wasFullscreenBeforeSexy;   // were we fullscreen or mini?
    // ─────────────────────────────────────────────

    private void Start()
    {
        ApplyFogTexture();
        ApplyLayout(false);
    }

    private void Update()
    {
        HandleSexyTimeAutoHide();

        // If SexyTime is currently active and we're configured to disable during SexyTime,
        // ignore input so the player can't open the minimap over the minigame.
        if (disableDuringSexyTime && SexyTimeLogic.isSexyTimeGoingOn)
            return;

        if (Input.GetKeyDown(toggleKey))
            ToggleFullscreen();
    }

    public void SetFullscreen(bool full)
    {
        if (_isFullscreen == full) return;
        _isFullscreen = full;
        ApplyLayout(full);

        if (cineBridge != null) cineBridge.SetFullscreen(full);
        else if (cameraFollow != null) cameraFollow.SetFullscreen(full);
    }

    // already in your file; expose this so persistent controller can call it:
    public void ReapplyFogTexture()
    {
        if (fogImage == null) return;
        if (MinimapFog.Instance == null) return;
        fogImage.texture = MinimapFog.Instance.fogTexture;
        fogImage.color = fogTint;
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
            if (full)
            {
                // Fullscreen = stretch to whole canvas
                container.anchorMin = fullAnchorMin; // (0,0)
                container.anchorMax = fullAnchorMax; // (1,1)
                container.pivot = new Vector2(0.5f, 0.5f);
                container.offsetMin = container.offsetMax = Vector2.zero;
                container.anchoredPosition = Vector2.zero;
            }
            else
            {
                if (miniLayoutMode == MiniLayoutMode.PercentOfScreen)
                {
                    // Old behavior: percentage anchors (shrinks with screen/aspect)
                    container.anchorMin = miniAnchorMin;
                    container.anchorMax = miniAnchorMax;
                    container.pivot = new Vector2(0.5f, 0.5f);
                    container.offsetMin = container.offsetMax = Vector2.zero;
                    container.anchoredPosition = Vector2.zero;
                }
                else // FixedPixels
                {
                    // Top-right fixed pixel size
                    container.anchorMin = container.anchorMax = new Vector2(1f, 1f); // top-right
                    container.pivot = new Vector2(1f, 1f);
                    container.sizeDelta = miniSizePx;
                    container.anchoredPosition = new Vector2(-miniMarginPx.x, -miniMarginPx.y);
                }
            }
        }

        if (cameraFollow != null) cameraFollow.SetFullscreen(full);
    }

    private void ApplyFogTexture()
    {
        if (fogImage == null) return;
        if (MinimapFog.Instance == null) return;

        fogImage.texture = MinimapFog.Instance.fogTexture;
        fogImage.color = fogTint;
        // Default UI material respects the texture alpha = perfect.
    }

    // ─────────────────────────────────────────────
    // SexyTime auto-hide / restore
    private void HandleSexyTimeAutoHide()
    {
        if (!disableDuringSexyTime)
            return;

        bool sexyNow = SexyTimeLogic.isSexyTimeGoingOn;

        // No change since last frame -> nothing to do
        if (sexyNow == _wasSexyTime)
            return;

        _wasSexyTime = sexyNow;

        if (sexyNow)
        {
            // SexyTime just started: remember current state, then hide.
            _wasVisibleBeforeSexy = container != null && container.gameObject.activeSelf;
            _wasFullscreenBeforeSexy = _isFullscreen;

            if (container != null)
                container.gameObject.SetActive(false);

            if (minimapCamera != null)
                minimapCamera.enabled = false;
        }
        else
        {
            // SexyTime just ended: restore what we had.
            if (container != null)
                container.gameObject.SetActive(_wasVisibleBeforeSexy);

            if (minimapCamera != null)
                minimapCamera.enabled = _wasVisibleBeforeSexy;

            if (_wasVisibleBeforeSexy)
            {
                // Restore layout and fullscreen state
                _isFullscreen = _wasFullscreenBeforeSexy;
                ApplyLayout(_isFullscreen);

                if (cineBridge != null)
                    cineBridge.SetFullscreen(_isFullscreen);
                else if (cameraFollow != null)
                    cameraFollow.SetFullscreen(_isFullscreen);
            }
        }
    }
    // ─────────────────────────────────────────────
}
