using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquipmentSlot : UI_ItemSlot
{
    /// ✅ So `UI_EquipmentInventory` can call this:
    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null) return;

        // Try to equip this item FROM the equipment inventory bag
        inventory.TryEquipFromEquipmentInventory(itemInSlot);

        // Hide tooltip if needed
        ui.itemToolTip.ShowToolTip(false, null);
    }
}
