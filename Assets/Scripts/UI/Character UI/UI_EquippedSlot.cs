using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_EquippedSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType slotType;

    [Header("Equipment Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;
    [SerializeField] private Button button;

    private bool isInteractable = true;

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
            equipmentUI.EnableEquipmentSlotSelection(true); // now enable interaction
            equipmentUI.EquippedSlotsPanel.SetEquippedSlotInteractable(this);
        }
    }


    public override void OnPointerEnter(PointerEventData eventData)
    {
        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null && !equipmentUI.IsInSelectionMode())
        {
            equipmentUI.Open();
            equipmentUI.FilterBySlotType(slotType);
            equipmentUI.EnableEquipmentSlotSelection(false); // disables interactivity
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
    }
}
