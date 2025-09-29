using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollToSelection : MonoBehaviour, ISelectHandler, IUpdateSelectedHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    void Reset()
    {
        scrollRect = GetComponentInParent<ScrollRect>();
        content = scrollRect ? scrollRect.content : null;
    }

    public void OnSelect(BaseEventData eventData) => EnsureVisible();
    public void OnUpdateSelected(BaseEventData data) => EnsureVisible();

    private void EnsureVisible()
    {
        if (scrollRect == null || content == null) return;

        var selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null) return;

        var item = selected.transform as RectTransform;
        if (item == null) return;

        // Only act if the item lives under the content
        if (!item.IsChildOf(content)) return;

        // Viewport rect to compare against
        var view = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;

        // Bounds in CONTENT's local space
        Bounds itemB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, item);
        Bounds viewB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, view);

        // Compute how far the item sits outside the view (in content space)
        float deltaY = 0f;
        if (itemB.max.y > viewB.max.y) deltaY = itemB.max.y - viewB.max.y;   // scroll up
        else if (itemB.min.y < viewB.min.y) deltaY = itemB.min.y - viewB.min.y;   // scroll down

        float deltaX = 0f;
        if (itemB.min.x < viewB.min.x) deltaX = itemB.min.x - viewB.min.x;   // scroll right
        else if (itemB.max.x > viewB.max.x) deltaX = itemB.max.x - viewB.max.x;   // scroll left

        if (Mathf.Abs(deltaX) > 0.01f || Mathf.Abs(deltaY) > 0.01f)
        {
            // Move content by the offset (signs are correct for content-local space)
            var pos = content.anchoredPosition;
            pos += new Vector2(deltaX, deltaY);
            content.anchoredPosition = pos;
        }
    }
}
