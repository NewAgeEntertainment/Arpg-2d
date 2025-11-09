using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MinimapUI))]
public class MinimapBlips : MonoBehaviour
{
    [Header("Refs")]
    public MinimapUI minimapUI;          // drag your existing MinimapUI here
    public RectTransform blipContainer;  // the UI parent that holds all blips (under your minimap)
    public Camera minimapCamera;         // usually minimapUI.minimapCamera, but exposed for flexibility

    [Header("Blip Prefab")]
    public RectTransform blipPrefab;     // UI Image prefab with optional child "Arrow" image

    [Header("Settings")]
    [Tooltip("Viewport padding so clamped icons don't overlap the very border. In normalized 0..0.5 range.")]
    [Range(0f, 0.25f)] public float edgePadding01 = 0.06f;

    [Tooltip("When on-screen, hide the arrow child.")]
    public bool hideArrowWhenOnscreen = true;

    [Tooltip("When off-screen, show the arrow child and rotate it to point toward the target.")]
    public bool rotateArrowWhenOffscreen = true;

    [Tooltip("Optional global scaling for blips.")]
    public float blipScale = 1f;

    [Header("Targets")]
    public List<MinimapTarget> targets = new List<MinimapTarget>();

    readonly Dictionary<MinimapTarget, RectTransform> _spawned = new();

    void Reset()
    {
        minimapUI = GetComponent<MinimapUI>();
        if (minimapUI != null) minimapCamera = minimapUI.minimapCamera;
    }

    void OnEnable()
    {
        // Create blips for pre-populated list
        EnsureAllBlipsExist();
    }

    void OnDisable()
    {
        // (Optional) You could destroy here to keep things clean.
    }

    public void RegisterTarget(MinimapTarget t)
    {
        if (!targets.Contains(t)) targets.Add(t);
        EnsureBlipExists(t);
    }

    public void UnregisterTarget(MinimapTarget t)
    {
        if (targets.Remove(t))
        {
            if (_spawned.TryGetValue(t, out var rt) && rt != null)
                Destroy(rt.gameObject);
            _spawned.Remove(t);
        }
    }

    void EnsureAllBlipsExist()
    {
        foreach (var t in targets) EnsureBlipExists(t);
    }

    // Replace your EnsureBlipExists(...) with this:
    void EnsureBlipExists(MinimapTarget t)
    {
        if (t == null || blipPrefab == null || blipContainer == null) return;
        if (_spawned.ContainsKey(t) && _spawned[t] != null) return;

        var rt = Instantiate(blipPrefab, blipContainer);
        rt.localScale = Vector3.one * blipScale;

        // If prefab root has an Image, let per-target sprite/tint override it (optional).
        var rootImg = rt.GetComponent<UnityEngine.UI.Image>();
        if (rootImg != null)
        {
            if (t.iconSprite != null) rootImg.sprite = t.iconSprite;
            if (t.iconTint.a > 0f) rootImg.color = t.iconTint;
            rootImg.raycastTarget = false; // don’t block UI clicks
        }

        // Find a child named "Arrow"; if none, use the root as the arrow (arrow-only prefab).
        Transform arrow = rt.Find("Arrow");
        t.arrowTransform = (arrow != null) ? arrow : rt;

        // If both a root Image and an Arrow child exist, disable the root Image so only Arrow can show.
        if (arrow != null && rootImg != null) rootImg.enabled = false;

        // Ensure the arrow Image (whichever transform it is) won't eat raycasts.
        var arrowImg = t.arrowTransform.GetComponent<UnityEngine.UI.Image>();
        if (arrowImg != null) arrowImg.raycastTarget = false;

        t.blipRect = rt;
        _spawned[t] = rt;
    }


    // Replace your LateUpdate() with this:
    void LateUpdate()
    {
        if (minimapCamera == null || minimapUI == null || minimapUI.mapImage == null) return;
        if (blipContainer == null) return;

        var mapRect = minimapUI.mapImage.rectTransform.rect;
        var mapRectT = minimapUI.mapImage.rectTransform;

        Vector2 center01 = new(0.5f, 0.5f);
        float maxRadius01 = 0.5f - edgePadding01;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var t = targets[i];
            if (t == null)
            {
                targets.RemoveAt(i);
                continue;
            }

            EnsureBlipExists(t);
            var rt = t.blipRect;
            if (rt == null) continue;

            // World -> viewport (0..1)
            Vector3 world = t.transform.position;
            Vector3 v = minimapCamera.WorldToViewportPoint(world);
            Vector2 uv = new(v.x, v.y);

            // Direction from center
            Vector2 dir = uv - center01;

            // Off-screen test (outside [0..1] or behind camera)
            bool offscreen = (v.z <= 0f) || uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f;
            if (v.z <= 0f) dir = -dir;

            // Clamp to edge when off-screen; otherwise keep slight padding
            Vector2 clamped01;
            if (offscreen)
            {
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    clamped01 = center01 + dir * maxRadius01;
                }
                else
                {
                    clamped01 = center01 + Vector2.up * maxRadius01;
                }
            }
            else
            {
                clamped01 = new Vector2(
                    Mathf.Clamp(uv.x, edgePadding01, 1f - edgePadding01),
                    Mathf.Clamp(uv.y, edgePadding01, 1f - edgePadding01)
                );
            }

            // Normalized -> Map local -> BlipContainer local
            float px = Mathf.Lerp(mapRect.xMin, mapRect.xMax, clamped01.x);
            float py = Mathf.Lerp(mapRect.yMin, mapRect.yMax, clamped01.y);

            var worldPoint = mapRectT.TransformPoint(new Vector3(px, py, 0f));
            var localPoint = blipContainer.InverseTransformPoint(worldPoint);
            rt.anchoredPosition = localPoint;

            // --- Arrow show/hide & rotation (explicit) ---
            if (t.arrowTransform != null)
            {
                // Show arrow only when OFF-SCREEN
                bool shouldShowArrow = offscreen;

                if (t.arrowTransform.gameObject.activeSelf != shouldShowArrow)
                    t.arrowTransform.gameObject.SetActive(shouldShowArrow);

                if (shouldShowArrow && rotateArrowWhenOffscreen && t.rotateArrow)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    // Assumes arrow sprite points 'up' (positive Y) in its texture space.
                    t.arrowTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }
            }
        }
    }

}
