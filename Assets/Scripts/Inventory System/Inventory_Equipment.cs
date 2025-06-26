using System.Collections.Generic;
using UnityEngine;

public class Inventory_Equipment : Inventory_Base
{
    private readonly HashSet<ItemType> allowedEquipmentTypes = new HashSet<ItemType>
    {
        ItemType.Weapon,
        ItemType.Armor,
        ItemType.trinket
    };

    // Override AddItem to restrict to equipment only
    public new void AddItem(Inventory_Item itemToAdd)
    {
        if (!IsEquipment(itemToAdd))
        {
            Debug.LogWarning($"Item '{itemToAdd.itemData.name}' is not equipment and cannot be added to the equipment inventory.");
            return;
        }

        base.AddItem(itemToAdd);
    }

    // Override CanAddItem to reject non-equipment types
    public new bool CanAddItem(Inventory_Item itemToAdd)
    {
        return IsEquipment(itemToAdd) && base.CanAddItem(itemToAdd);
    }

    private bool IsEquipment(Inventory_Item item)
    {
        return item != null &&
               item.itemData != null &&
               allowedEquipmentTypes.Contains(item.itemData.itemType);
    }

    // Optional helper method
    public List<Inventory_Item> GetAllEquippedItems()
    {
        return new List<Inventory_Item>(itemList);
    }
}
