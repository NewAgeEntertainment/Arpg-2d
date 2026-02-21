using UnityEngine;
using UnityEngine.UI;

public class MapPlayerDotAligned : MonoBehaviour
{
    [Header("Baked map refs")]
    public TilemapMapMetadata metadata;
    public Grid grid;
    public RawImage mapRawImage;
    public RectTransform playerDot;

    [Header("Player refs (position + rotation)")]
    public Transform player;
    public Rigidbody2D playerRb;

    public enum RotationMode { None, UseTransformZ, UseVelocity2D }

    [Header("Orientation")]
    public RotationMode rotationMode = RotationMode.UseTransformZ;
    public float headingOffsetDegrees = -90f;

    [Header("Clamping")]
    public bool clampInsideVisible = true;

    [Header("Minimap Only")]
    [Tooltip("Optional: Assign the minimap panel root. If it's inactive, we stop updating.")]
    public GameObject minimapRoot;

    [Tooltip("Optional: lock the minimap size so it can never become fullscreen by accident.")]
    public bool enforceFixedSize = true;

    [Tooltip("Size of the minimap panel in pixels (only used if Enforce Fixed Size is on).")]
    public Vector2 fixedSize = new Vector2(220, 220);

    [Tooltip("Optional: where the minimap sits on screen (anchored position).")]
    public Vector2 anchoredPosition = new Vector2(-20, 20); // e.g. bottom-right with proper anchors

    private RectTransform mapRT;

    void Awake()
    {
        if (mapRawImage) mapRT = mapRawImage.rectTransform;
    }

    void LateUpdate()
    {
        // If minimap root exists and is disabled, do nothing.
        if (minimapRoot != null && !minimapRoot.activeInHierarchy) return;

        if (!metadata || !grid || !mapRawImage || !mapRawImage.texture || !playerDot) return;

        // Hard prevent fullscreen by forcing size (optional).
        if (enforceFixedSize && mapRT != null)
        {
            mapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fixedSize.x);
            mapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedSize.y);
            mapRT.anchoredPosition = anchoredPosition;
        }

        // 1) Position
        Vector3 worldPos = GetPlayerWorldPos();
        Vector2 uv = metadata.WorldToMapUV(worldPos, grid);

        Rect visible = GetRawImageVisibleRect(mapRawImage);

        if (clampInsideVisible)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }

        float x = Mathf.Lerp(visible.xMin, visible.xMax, uv.x);
        float y = Mathf.Lerp(visible.yMin, visible.yMax, uv.y);
        playerDot.anchoredPosition = new Vector2(x, y);

        // 2) Rotation
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

    Vector3 GetPlayerWorldPos()
    {
        if (player) return player.position;
        var p = GameObject.FindGameObjectWithTag("Player");
        return p ? p.transform.position : Vector3.zero;
    }

    static Rect GetRawImageVisibleRect(RawImage raw)
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

    static Rect ShiftToCenter(Rect rectBL, RectTransform rt)
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
