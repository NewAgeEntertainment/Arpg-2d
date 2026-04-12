using System;
using UnityEngine;

[Serializable]
public class Inventory_Equipped
{
    public EquipmentSlotType slotType;
    public ItemType acceptedItemType;
    public Inventory_Item equipedItem;

    public bool HasItem() => equipedItem != null && equipedItem.itemData != null;

    public bool CanAccept(Inventory_Item item)
    {
        return item != null &&
               item.itemData != null &&
               item.itemData.itemType == acceptedItemType;
    }
}