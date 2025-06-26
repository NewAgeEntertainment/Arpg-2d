using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Inventory_Equipped
{
    public ItemType slotType;
    public Inventory_Item equipedItem;


    public bool HasItem() => equipedItem != null && equipedItem.itemData != null;
}
