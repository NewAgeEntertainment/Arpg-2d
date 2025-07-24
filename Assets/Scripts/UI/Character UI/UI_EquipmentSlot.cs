using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquipmentSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;

    private bool isSelectable = true;
    private Coroutine blinkCoroutine;

    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
    }

    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelectable) return;

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI == null) return;

        Debug.Log($"[UI_EquipmentSlot] Attempting to equip: {itemInSlot?.itemData?.itemName}");
        equipmentUI.SwapEquippedItem(itemInSlot);

        StopBlinkingHighlight();
        SetHighlightSolid(true);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable) return;
        StartBlinkingHighlight();

        if (itemInSlot != null && equipmentToolTip != null)
        {
            equipmentToolTip.ShowEquipmentToolTip(true, itemInSlot);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable) return;

        StopBlinkingHighlight();
        SetHighlightSolid(false);

        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    private void StartBlinkingHighlight()
    {
        if (highlighter == null) return;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkHighlight());
    }

    private void StopBlinkingHighlight()
    {
        if (highlighter == null) return;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = null;
    }

    private void SetHighlightSolid(bool on)
    {
        if (highlighter != null)
            highlighter.SetActive(on);
    }

    private System.Collections.IEnumerator BlinkHighlight()
    {
        while (true)
        {
            highlighter.SetActive(!highlighter.activeSelf);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ResetHighlight()
    {
        StopBlinkingHighlight();
        SetHighlightSolid(false);
    }
}
