using UnityEngine;
using UnityEngine.UI;

public class MapPlayerDotAligned : MonoBehaviour
{
    [Header("Baked map refs")]
    public TilemapMapMetadata metadata;     // the _Meta asset for THIS scene
    public Grid grid;                       // your scene's Grid
    public RawImage mapRawImage;            // RawImage showing the baked PNG
    public RectTransform playerDot;         // UI Image RectTransform (the dot/arrow)

    [Header("Player refs (position + rotation)")]
    public Transform player;                // assign your Player transform here
    public Rigidbody2D playerRb;            // optional, for velocity-based facing

    public enum RotationMode { None, UseTransformZ, UseVelocity2D }
    [Header("Orientation")]
    public RotationMode rotationMode = RotationMode.UseTransformZ;
    [Tooltip("Rotate the dot so it points the way your sprite/arrow art expects. Common values: -90 or 0.")]
    public float headingOffsetDegrees = -90f;

    [Header("Clamping")]
    [Tooltip("Keep the dot inside the visible map rect, even if the player steps slightly outside baked bounds.")]
    public bool clampInsideVisible = true;

    void LateUpdate()
    {
        if (!metadata || !grid || !mapRawImage || !mapRawImage.texture || !playerDot) return;

        // --- 1) POSITION: world -> UV -> anchoredPosition inside the RawImage's visible area
        Vector3 worldPos = GetPlayerWorldPos();
        Vector2 uv = metadata.WorldToMapUV(worldPos, grid); // 0..1
        Rect visible = GetRawImageVisibleRect(mapRawImage); // centered local rect

        if (clampInsideVisible)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }

        float x = Mathf.Lerp(visible.xMin, visible.xMax, uv.x);
        float y = Mathf.Lerp(visible.yMin, visible.yMax, uv.y);
        playerDot.anchoredPosition = new Vector2(x, y);

        // --- 2) ROTATION: match player transform or velocity (optional)
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
                else if (player) // fallback to transform if standing still
                {
                    angle = player.eulerAngles.z + headingOffsetDegrees;
                }
                break;

            case RotationMode.None:
                // leave angle = 0
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

    // Visible sub-rect of the RawImage texture (handles letterbox/pillarbox).
    static Rect GetRawImageVisibleRect(RawImage raw)
    {
        RectTransform rt = raw.rectTransform;
        Rect r = rt.rect; // full rect, bottom-left anchored
        Texture tex = raw.texture;
        if (!tex) return ShiftToCenter(r, rt);

        float rw = r.width, rh = r.height;
        float tw = tex.width, th = tex.height;
        if (rw <= 0 || rh <= 0 || tw <= 0 || th <= 0) return ShiftToCenter(r, rt);

        float rectAspect = rw / rh;
        float texAspect = tw / th;

        Rect content; // bottom-left anchored
        if (rectAspect > texAspect)
        {
            // pillarbox
            float contentW = rh * texAspect;
            float padX = (rw - contentW) * 0.5f;
            content = new Rect(r.xMin + padX, r.yMin, contentW, rh);
        }
        else
        {
            // letterbox
            float contentH = rw / texAspect;
            float padY = (rh - contentH) * 0.5f;
            content = new Rect(r.xMin, r.yMin + padY, rw, contentH);
        }

        return ShiftToCenter(content, rt);
    }

    // Convert a bottom-left anchored rect to a rect centered at (0,0) in the RectTransform's local space.
    static Rect ShiftToCenter(Rect rectBL, RectTransform rt)
    {
        // Center of the full rect
        float fullCx = (rt.rect.xMin + rt.rect.xMax) * 0.5f;
        float fullCy = (rt.rect.yMin + rt.rect.yMax) * 0.5f;
        // Center of the content rect
        float cx = (rectBL.xMin + rectBL.xMax) * 0.5f;
        float cy = (rectBL.yMin + rectBL.yMax) * 0.5f;

        // Offset from full center to content center
        float ox = cx - fullCx;
        float oy = cy - fullCy;

        return new Rect(-rectBL.width * 0.5f + ox, -rectBL.height * 0.5f + oy, rectBL.width, rectBL.height);
    }
}
