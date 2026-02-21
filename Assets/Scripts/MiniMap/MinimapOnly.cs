using UnityEngine;
using UnityEngine.UI;

public class MinimapOnly : MonoBehaviour
{
    [Header("Baked map refs (this scene)")]
    public TilemapMapMetadata metadata;     // _Meta asset for THIS scene
    public Grid grid;                       // this scene's Grid
    public RawImage mapRawImage;            // RawImage showing the baked PNG
    public RectTransform playerDot;         // UI RectTransform (dot/arrow)

    [Header("Player refs")]
    public Transform player;                // assign Player transform (optional)
    public Rigidbody2D playerRb;            // optional for velocity facing

    public enum RotationMode { None, UseTransformZ, UseVelocity2D }

    [Header("Orientation")]
    public RotationMode rotationMode = RotationMode.UseTransformZ;
    [Tooltip("Rotate dot so it points the way your sprite/arrow art expects. Common: -90 or 0.")]
    public float headingOffsetDegrees = -90f;

    [Header("Clamping")]
    [Tooltip("Keep the dot inside the visible map rect.")]
    public bool clampInsideVisible = true;

    [Header("Minimap Layout (no fullscreen)")]
    [Tooltip("Panel/root to hide/show the minimap. If null, script always updates.")]
    public GameObject minimapRoot;

    public enum MiniLayoutMode { PercentOfScreen, FixedPixels }

    [Tooltip("How the minimap panel is sized/anchored.")]
    public MiniLayoutMode miniLayoutMode = MiniLayoutMode.FixedPixels;

    [Tooltip("Used when PercentOfScreen.")]
    public Vector2 miniAnchorMin = new Vector2(0.75f, 0.75f);
    [Tooltip("Used when PercentOfScreen.")]
    public Vector2 miniAnchorMax = new Vector2(0.98f, 0.98f);

    [Tooltip("Used when FixedPixels.")]
    public Vector2 miniSizePx = new Vector2(260, 260);
    [Tooltip("Used when FixedPixels. Margin from top-right in pixels.")]
    public Vector2 miniMarginPx = new Vector2(16, 16);

    [Header("Optional: Auto-hide during SexyTime")]
    [SerializeField] private bool disableDuringSexyTime = true;

    private RectTransform _mapRT;
    private bool _wasSexyTime;
    private bool _wasVisibleBeforeSexy;

    private void Awake()
    {
        if (mapRawImage) _mapRT = mapRawImage.rectTransform;

        // Apply minimap layout once on startup (no fullscreen exists).
        ApplyMiniLayout();
    }

    private void Update()
    {
        HandleSexyTimeAutoHide();
    }

    private void LateUpdate()
    {
        // If minimap is hidden, don't update.
        if (minimapRoot != null && !minimapRoot.activeInHierarchy) return;
        if (disableDuringSexyTime && SexyTimeLogic.isSexyTimeGoingOn) return;

        if (!metadata || !grid || !mapRawImage || !mapRawImage.texture || !playerDot) return;

        // 1) POSITION: world -> UV -> anchoredPosition inside RawImage visible rect
        Vector3 worldPos = GetPlayerWorldPos();
        Vector2 uv = metadata.WorldToMapUV(worldPos, grid);

        if (clampInsideVisible)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }

        Rect visible = GetRawImageVisibleRect(mapRawImage);

        float x = Mathf.Lerp(visible.xMin, visible.xMax, uv.x);
        float y = Mathf.Lerp(visible.yMin, visible.yMax, uv.y);
        playerDot.anchoredPosition = new Vector2(x, y);

        // 2) ROTATION
        float angle = 0f;
        switch (rotationMode)
        {
            case RotationMode.UseTransformZ:
                if (player) angle = player.eulerAngles.z + headingOffsetDegrees;
                break;

            case RotationMode.UseVelocity2D:
                if (playerRb && playerRb.velocity.sqrMagnitude > 0.0001f)
                {
                    Vector2 v = playerRb.velocity.normalized;
                    angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + headingOffsetDegrees;
                }
                else if (player)
                {
                    angle = player.eulerAngles.z + headingOffsetDegrees;
                }
                break;

            case RotationMode.None:
                break;
        }

        playerDot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ------------------ Layout (NO FULLSCREEN) ------------------

    [ContextMenu("Apply Mini Layout")]
    public void ApplyMiniLayout()
    {
        if (_mapRT == null && mapRawImage != null) _mapRT = mapRawImage.rectTransform;
        if (_mapRT == null) return;

        // If your container is a parent panel, you can assign minimapRoot and size that instead.
        // Here we size/anchor the RawImage itself.
        if (miniLayoutMode == MiniLayoutMode.PercentOfScreen)
        {
            _mapRT.anchorMin = miniAnchorMin;
            _mapRT.anchorMax = miniAnchorMax;
            _mapRT.pivot = new Vector2(0.5f, 0.5f);
            _mapRT.offsetMin = _mapRT.offsetMax = Vector2.zero;
            _mapRT.anchoredPosition = Vector2.zero;
        }
        else // FixedPixels (top-right)
        {
            _mapRT.anchorMin = _mapRT.anchorMax = new Vector2(1f, 1f);
            _mapRT.pivot = new Vector2(1f, 1f);
            _mapRT.sizeDelta = miniSizePx;
            _mapRT.anchoredPosition = new Vector2(-miniMarginPx.x, -miniMarginPx.y);
        }
    }

    // ------------------ SexyTime Hide/Restore ------------------

    private void HandleSexyTimeAutoHide()
    {
        if (!disableDuringSexyTime) return;

        bool sexyNow = SexyTimeLogic.isSexyTimeGoingOn;
        if (sexyNow == _wasSexyTime) return;
        _wasSexyTime = sexyNow;

        if (sexyNow)
        {
            _wasVisibleBeforeSexy = minimapRoot != null && minimapRoot.activeSelf;

            if (minimapRoot != null) minimapRoot.SetActive(false);
        }
        else
        {
            if (minimapRoot != null) minimapRoot.SetActive(_wasVisibleBeforeSexy);
            if (_wasVisibleBeforeSexy) ApplyMiniLayout();
        }
    }

    // ------------------ Helpers ------------------

    private Vector3 GetPlayerWorldPos()
    {
        if (player) return player.position;
        var p = GameObject.FindGameObjectWithTag("Player");
        return p ? p.transform.position : Vector3.zero;
    }

    // Visible sub-rect of the RawImage texture (handles letterbox/pillarbox).
    private static Rect GetRawImageVisibleRect(RawImage raw)
    {
        RectTransform rt = raw.rectTransform;
        Rect r = rt.rect;
        Texture tex = raw.texture;
        if (!tex) return ShiftToCenter(r, rt);

        float rw = r.width, rh = r.height;
        float tw = tex.width, th = tex.height;
        if (rw <= 0 || rh <= 0 || tw <= 0 || th <= 0) return ShiftToCenter(r, rt);

        float rectAspect = rw / rh;
        float texAspect = tw / th;

        Rect content;
        if (rectAspect > texAspect)
        {
            float contentW = rh * texAspect;
            float padX = (rw - contentW) * 0.5f;
            content = new Rect(r.xMin + padX, r.yMin, contentW, rh);
        }
        else
        {
            float contentH = rw / texAspect;
            float padY = (rh - contentH) * 0.5f;
            content = new Rect(r.xMin, r.yMin + padY, rw, contentH);
        }

        return ShiftToCenter(content, rt);
    }

    // Convert a bottom-left anchored rect to a rect centered at (0,0) in RectTransform local space.
    private static Rect ShiftToCenter(Rect rectBL, RectTransform rt)
    {
        float fullCx = (rt.rect.xMin + rt.rect.xMax) * 0.5f;
        float fullCy = (rt.rect.yMin + rt.rect.yMax) * 0.5f;

        float cx = (rectBL.xMin + rectBL.xMax) * 0.5f;
        float cy = (rectBL.yMin + rectBL.yMax) * 0.5f;

        float ox = cx - fullCx;
        float oy = cy - fullCy;

        return new Rect(-rectBL.width * 0.5f + ox, -rectBL.height * 0.5f + oy, rectBL.width, rectBL.height);
    }
}
