using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquipmentSlot : UI_ItemSlot
{
    // This method can be used by the UI panel to populate the slot with an equipment item
    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    // Optional: override OnPointerDown if you want different behavior for equipment-only inventory
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null)
            return;

        // Only allow equipping valid equipment types
        if (itemInSlot.itemData.itemType != ItemType.Weapon &&
            itemInSlot.itemData.itemType != ItemType.Armor &&
            itemInSlot.itemData.itemType != ItemType.trinket)
        {
            Debug.Log("This slot only handles equipment.");
            return;
        }

        // Try to equip from equipment inventory
        Debug.Log($"Attempting to equip item from equipment inventory: {itemInSlot.itemData.itemName}");
        inventory.TryEquipFromEquipmentInventory(itemInSlot);

        // Hide tooltip if successful
        if (itemInSlot == null)
            ui.itemToolTip.ShowToolTip(false, null);
    }

}
