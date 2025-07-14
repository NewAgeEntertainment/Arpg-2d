using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UI_EquippedSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType slotType;

    [Header("Equipment Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;
    [SerializeField] private Button button;

    private bool isInteractable = true;
    private Coroutine blinkCoroutine;

    private void OnValidate()
    {
        gameObject.name = "UI_EquippedSlot - " + slotType.ToString();
    }

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable) return;

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null)
        {
            equipmentUI.EnterSelectionMode(slotType);
            equipmentUI.SetSelectionEnabled(true);
            equipmentUI.EnableEquipmentSlotSelection(true);
            equipmentUI.EquippedSlotsPanel.SetEquippedSlotInteractable(this);
        }

        StopBlinkingHighlight();
        SetHighlightSolid(true);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;

        StartBlinkingHighlight();

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null && !equipmentUI.IsInSelectionMode())
        {
            equipmentUI.Open();
            equipmentUI.FilterBySlotType(slotType);
            equipmentUI.EnableEquipmentSlotSelection(false);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;

        StopBlinkingHighlight();
        SetHighlightSolid(false);
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        if (button != null)
            button.interactable = interactable;
    }

    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
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

    private IEnumerator BlinkHighlight()
    {
        while (true)
        {
            highlighter.SetActive(!highlighter.activeSelf);
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ResetHighlight()
    {
        StopBlinkingHighlight();
        SetHighlightSolid(false);
    }
}
