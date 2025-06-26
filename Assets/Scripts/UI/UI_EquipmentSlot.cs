using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_EquipmentSlot : UI_ItemSlot
{
    // This method can be used by the UI panel to populate the slot with an equipment item
    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    // Optional: override OnPointerDown if you want different behavior for equipment-only inventory
    public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (itemInSlot == null) return;

        // Prevent using non-equipment types from this UI
        var type = itemInSlot.itemData.itemType;
        if (type != ItemType.Weapon && type != ItemType.Armor && type != ItemType.trinket)
        {
            Debug.Log("This slot only handles equipment.");
            return;
        }

        // Try to equip item
        inventory.TryEquipItem(itemInSlot);

        if (itemInSlot == null)
            ui.itemToolTip.ShowToolTip(false, null);
    }
}
