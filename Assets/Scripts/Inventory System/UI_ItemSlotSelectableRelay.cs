using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UI_ItemSlotSelectableRelay : Selectable, ISubmitHandler, IPointerClickHandler
{
    [Tooltip("Drag the UI_ItemSlot on this same slot")]
    public UI_ItemSlot target;

    // Must be public (Selectable's methods are public)
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        if (target != null) target.SetSelected(true);
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        if (target != null) target.SetSelected(false);
        base.OnDeselect(eventData);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // Don’t invoke events directly (C# events can only be raised inside their class)
        target?.SubmitFromKeyboard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (target == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
            target.SubmitFromKeyboard();
        else if (eventData.button == PointerEventData.InputButton.Right)
            target.RightClickFromKeyboard();
    }
}
