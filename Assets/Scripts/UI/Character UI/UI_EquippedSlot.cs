using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquippedSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType slotType;

    [Header("Equipment Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;

    private void OnValidate()
    {
        gameObject.name = "UI_EquipmentSlot - " + slotType.ToString();
    }

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null)
            return;

        inventory.UnequipItem(itemInSlot);

        equipmentToolTip?.ShowEquipmentToolTip(false, null);
        ui?.itemToolTip?.ShowToolTip(false, null);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInSlot != null && equipmentToolTip != null)
            equipmentToolTip.ShowEquipmentToolTip(true, itemInSlot);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    /// ✅ Setter if you want to inject from EquipSlotParent:
    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
    }
}
