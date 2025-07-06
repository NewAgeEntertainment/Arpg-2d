using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquipmentSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;

    /// ✅ Equipment inventory calls this to update the slot
    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    /// ✅ Equipment inventory injects this!
    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null) return;

        // Equip it
        inventory.TryEquipFromEquipmentInventory(itemInSlot);

        // Hide both tooltips for safety
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
}
