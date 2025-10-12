using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SelectableHighlight : MonoBehaviour,
    ISelectHandler, IDeselectHandler, IUpdateSelectedHandler
{
    [Header("Highlight")]
    [SerializeField] private Graphic[] graphicsToTint;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private float lerpTime = 0f; // 0 = instant

    [Header("Auto-Scroll (optional)")]
    [Tooltip("If not set, will auto-find the nearest ScrollRect parent.")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [Tooltip("Extra space to keep around the selected item (pixels, in content space).")]
    [SerializeField] private float padding = 8f;
    [Tooltip("Auto-scroll vertically when needed.")]
    [SerializeField] private bool autoScrollVertical = true;
    [Tooltip("Auto-scroll horizontally when needed.")]
    [SerializeField] private bool autoScrollHorizontal = false;

    private bool _selected;
    private float _t;

    private void Reset()
    {
        graphicsToTint = GetComponentsInChildren<Graphic>(true);
        AutoFindScrollRect();
    }

    private void Awake()
    {
        if (graphicsToTint == null || graphicsToTint.Length == 0)
            graphicsToTint = GetComponentsInChildren<Graphic>(true);

        if (scrollRect == null || content == null)
            AutoFindScrollRect();
    }

    private void AutoFindScrollRect()
    {
        scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            content = scrollRect.content;
    }

    private void Update()
    {
        if (lerpTime <= 0f) return;

        var target = _selected ? selectedColor : normalColor;
        _t = Mathf.MoveTowards(_t, 1f, Time.unscaledDeltaTime / lerpTime);
        foreach (var g in graphicsToTint) if (g) g.color = Color.Lerp(g.color, target, _t);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true; _t = 0f;
        if (lerpTime <= 0f) SetAll(selectedColor);
        EnsureVisible(); // scroll immediately on select
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false; _t = 0f;
        if (lerpTime <= 0f) SetAll(normalColor);
    }

    // Called every frame while this is the selected object (keyboard/controller nav)
    public void OnUpdateSelected(BaseEventData data)
    {
        if (_selected) EnsureVisible();
    }

    private void SetAll(Color c)
    {
        foreach (var g in graphicsToTint) if (g) g.color = c;
    }

    /// <summary>Keeps this selectable inside the ScrollRect's viewport.</summary>
    private void EnsureVisible()
    {
        if (scrollRect == null || content == null) return;
        if (!gameObject.activeInHierarchy) return;

        var item = transform as RectTransform;
        if (item == null) return;
        if (!item.IsChildOf(content)) return;

        // The viewport rect to compare against (in content space)
        var view = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.transform as RectTransform;

        Bounds itemB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, item);
        Bounds viewB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, view);

        // Inflate the item bounds by padding to give some breathing room
        itemB.Expand(new Vector3(padding * 2f, padding * 2f, 0f));

        float deltaX = 0f, deltaY = 0f;

        if (autoScrollVertical)
        {
            if (itemB.max.y > viewB.max.y)       // above top -> scroll up (content moves down)
                deltaY = itemB.max.y - viewB.max.y;
            else if (itemB.min.y < viewB.min.y)  // below bottom -> scroll down (content moves up)
                deltaY = itemB.min.y - viewB.min.y;
        }

        if (autoScrollHorizontal)
        {
            if (itemB.min.x < viewB.min.x)       // left of view -> move right
                deltaX = itemB.min.x - viewB.min.x;
            else if (itemB.max.x > viewB.max.x)  // right of view -> move left
                deltaX = itemB.max.x - viewB.max.x;
        }

        if (Mathf.Abs(deltaX) > 0.01f || Mathf.Abs(deltaY) > 0.01f)
        {
            var pos = content.anchoredPosition;
            pos += new Vector2(deltaX, deltaY);
            content.anchoredPosition = pos;
        }
    }
}
