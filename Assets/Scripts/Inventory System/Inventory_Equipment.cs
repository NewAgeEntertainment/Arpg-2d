using System.Collections.Generic;
using UnityEngine;

public class Inventory_Equipment : Inventory_Base
{
    private readonly HashSet<ItemType> allowedEquipmentTypes = new()
    {
        ItemType.Weapon,
        ItemType.Armor,
        ItemType.trinket
    };

    public override bool AddItem(Inventory_Item item)
    {
        if (item == null || item.itemData == null)
        {
            Debug.LogWarning("[Inventory_Equipment] Tried to add null item.");
            return false;
        }

        if (!CanAddItem(item))
        {
            Debug.LogWarning("[Inventory_Equipment] No space to add item.");
            return false;
        }

        itemList.Add(item);
        NotifyInventoryChanged();
        return true;
    }

    private bool IsEquipment(Inventory_Item item)
    {
        return item != null && allowedEquipmentTypes.Contains(item.itemData.itemType);
    }

    public List<Inventory_Item> GetAllEquippedItems()
    {
        return new List<Inventory_Item>(itemList);
    }
}