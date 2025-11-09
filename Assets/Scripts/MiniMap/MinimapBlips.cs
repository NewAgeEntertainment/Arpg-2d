using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MinimapUI))]
public class MinimapBlips : MonoBehaviour
{
    [Header("Refs")]
    public MinimapUI minimapUI;
    public RectTransform blipContainer;
    public Camera minimapCamera;

    [Header("Blip Prefab")]
    public RectTransform blipPrefab;

    [Header("Settings")]
    [Range(0f, 0.25f)] public float edgePadding01 = 0.06f;
    public bool hideArrowWhenOnscreen = true;
    public bool rotateArrowWhenOffscreen = true;
    public float blipScale = 1f;

    [Header("Targets")]
    public List<MinimapTarget> targets = new();

    readonly Dictionary<MinimapTarget, RectTransform> _spawned = new();

    void Reset()
    {
        minimapUI = GetComponent<MinimapUI>();
        if (minimapUI != null) minimapCamera = minimapUI.minimapCamera;
    }

    void OnEnable() => EnsureAllBlipsExist();

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

    // Spawn (but we'll disable it if the target isn't visible per rules)
    void EnsureBlipExists(MinimapTarget t)
    {
        if (t == null || blipPrefab == null || blipContainer == null) return;
        if (_spawned.ContainsKey(t) && _spawned[t] != null) return;

        var rt = Instantiate(blipPrefab, blipContainer);
        rt.localScale = Vector3.one * blipScale;

        var rootImg = rt.GetComponent<Image>();
        if (rootImg != null)
        {
            if (t.iconSprite != null) rootImg.sprite = t.iconSprite;
            if (t.iconTint.a > 0f) rootImg.color = t.iconTint;
            rootImg.raycastTarget = false;
        }

        Transform arrow = rt.Find("Arrow");
        t.arrowTransform = (arrow != null) ? arrow : rt;

        if (arrow != null && rootImg != null) rootImg.enabled = false;

        var arrowImg = t.arrowTransform.GetComponent<Image>();
        if (arrowImg != null) arrowImg.raycastTarget = false;

        t.blipRect = rt;
        _spawned[t] = rt;

        // Apply initial active state
        rt.gameObject.SetActive(IsTargetVisible(t));
    }

    // NEW: central visibility rule
    bool IsTargetVisible(MinimapTarget t)
    {
        if (t == null) return false;

        // Must be enabled
        if (!t.enabled) return false;

        // If required, only show when the target object is active in hierarchy
        if (t.requireActiveInHierarchy && !t.gameObject.activeInHierarchy) return false;

        return true;
    }

    void LateUpdate()
    {
        if (minimapCamera == null || minimapUI == null || minimapUI.mapImage == null) return;
        if (blipContainer == null) return;

        var mapRT = minimapUI.mapImage.rectTransform;
        var mapRect = mapRT.rect;

        Vector2 center01 = new(0.5f, 0.5f);
        float maxRadius01 = 0.5f - edgePadding01;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var t = targets[i];
            if (t == null) { targets.RemoveAt(i); continue; }

            EnsureBlipExists(t);
            var rt = t.blipRect;
            if (rt == null) continue;

            // Respect active rules: hide (and skip layout) if not visible
            bool visible = IsTargetVisible(t);
            if (rt.gameObject.activeSelf != visible)
                rt.gameObject.SetActive(visible);
            if (!visible) continue;

            // World -> viewport
            Vector3 v = minimapCamera.WorldToViewportPoint(t.transform.position);
            Vector2 uv = new(v.x, v.y);
            Vector2 dir = uv - center01;

            bool offscreen = (v.z <= 0f) || uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f;
            if (v.z <= 0f) dir = -dir;

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
                    Mathf.Clamp(uv.y, edgePadding01, 1f - edgePadding01));
            }

            float px = Mathf.Lerp(mapRect.xMin, mapRect.xMax, clamped01.x);
            float py = Mathf.Lerp(mapRect.yMin, mapRect.yMax, clamped01.y);

            var worldPoint = mapRT.TransformPoint(new Vector3(px, py, 0f));
            var localPoint = blipContainer.InverseTransformPoint(worldPoint);
            rt.anchoredPosition = localPoint;

            // Arrow visibility/rotation
            if (t.arrowTransform != null)
            {
                bool showArrow = offscreen;
                if (t.arrowTransform.gameObject.activeSelf != showArrow)
                    t.arrowTransform.gameObject.SetActive(showArrow);

                if (showArrow && rotateArrowWhenOffscreen && t.rotateArrow)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    t.arrowTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }
            }
        }
    }
}
