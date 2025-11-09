using UnityEngine;
using UnityEngine.UI;

public class MapPlayerDot : MonoBehaviour
{
    [Header("Baked map refs")]
    public TilemapMapMetadata metadata;
    public Grid grid;
    public RawImage mapRawImage;          // <-- reference the RawImage component
    public RectTransform playerDot;       // UI Image RectTransform

    void LateUpdate()
    {
        if (!metadata || !grid || !mapRawImage || !mapRawImage.texture || !playerDot) return;

        // 1) Convert player world -> UV (0..1) on baked texture
        Vector2 uv = metadata.WorldToMapUV(PlayerWorldPos(), grid);

        // 2) Compute the *visible* sub-rect where the texture is actually drawn
        RectTransform rt = mapRawImage.rectTransform;
        Rect visible = GetRawImageVisibleRect(mapRawImage); // in local space (centered)

        // 3) Place the dot inside that visible rect
        float x = Mathf.Lerp(visible.xMin, visible.xMax, uv.x);
        float y = Mathf.Lerp(visible.yMin, visible.yMax, uv.y);
        playerDot.anchoredPosition = new Vector2(x, y);
    }

    Vector3 PlayerWorldPos()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        return p ? p.transform.position : Vector3.zero;
    }

    // Returns the rect (in local/anchored space, pivot center) where the texture is drawn
    static Rect GetRawImageVisibleRect(RawImage raw)
    {
        RectTransform rt = raw.rectTransform;
        Rect r = rt.rect; // this is the full rect
        Texture tex = raw.texture;
        if (tex == null) return r;

        // If you keep "Preserve Aspect" off on RawImage, Unity stretches to r,
        // but many people use an AspectRatioFitter or a parent layout.
        // Compute letterboxing/pillarboxing to find the actual content area:
        float rw = r.width, rh = r.height;
        float tw = tex.width, th = tex.height;
        if (rw <= 0 || rh <= 0 || tw <= 0 || th <= 0) return r;

        float rectAspect = rw / rh;
        float texAspect = tw / th;

        if (rectAspect > texAspect)
        {
            // Pillarbox: height fits, width letterboxed
            float contentW = rh * texAspect;
            float padX = (rw - contentW) * 0.5f;
            return new Rect(r.xMin + padX, r.yMin, contentW, rh).ShiftToCenter(rt);
        }
        else
        {
            // Letterbox: width fits, height letterboxed
            float contentH = rw / texAspect;
            float padY = (rh - contentH) * 0.5f;
            return new Rect(r.xMin, r.yMin + padY, rw, contentH).ShiftToCenter(rt);
        }
    }
}

// Small helper to convert bottom-left anchored rect to centered local space.
// (RectTransform.rect is bottom-left anchored; our anchoredPosition expects center origin.)
static class RectExtensions
{
    public static Rect ShiftToCenter(this Rect r, RectTransform rt)
    {
        // Convert to a rect centered at (0,0) with same size and offsets
        // For anchoredPosition math, we want min/max relative to center.
        float cx = (r.xMin + r.xMax) * 0.5f - (rt.rect.xMin + rt.rect.xMax) * 0.5f;
        float cy = (r.yMin + r.yMax) * 0.5f - (rt.rect.yMin + rt.rect.yMax) * 0.5f;
        return new Rect(-r.width * 0.5f + cx, -r.height * 0.5f + cy, r.width, r.height);
    }
}
