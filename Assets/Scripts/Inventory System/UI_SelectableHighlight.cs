using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SelectableHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Graphic[] graphicsToTint;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private float lerpTime = 0f; // 0 = instant

    private bool _selected;
    private float _t;

    private void Reset()
    {
        graphicsToTint = GetComponentsInChildren<Graphic>(true);
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
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false; _t = 0f;
        if (lerpTime <= 0f) SetAll(normalColor);
    }

    private void SetAll(Color c)
    {
        foreach (var g in graphicsToTint) if (g) g.color = c;
    }
}
