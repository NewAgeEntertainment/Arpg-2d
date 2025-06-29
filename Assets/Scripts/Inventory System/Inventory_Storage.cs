using System.Collections.Generic;
using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    private Inventory_Player playerInventory;
    public List<Inventory_Item> materialStash = new List<Inventory_Item>();

    public void SetInventory(Inventory_Player inventory) => playerInventory = inventory;

    public void AddMaterialToStash(Inventory_Item itemToAdd)
    {
        var stackableItem = StackableInStash(itemToAdd);

        if (stackableItem != null)
            stackableItem.AddStack();
        else
            materialStash.Add(itemToAdd);

        NotifyInventoryChanged();
    }

    public Inventory_Item StackableInStash(Inventory_Item itemToAdd)
    {
        foreach (var stackableItem in materialStash)
        {
            if (stackableItem.itemData == itemToAdd.itemData && stackableItem.CanAddStack())
                return stackableItem;
        }
        return null;
    }

    public void FromPlayerToStorage(Inventory_Item item, bool transferFullStack)
    {
        int transferAmount = transferFullStack ? item.stackSize : 1;

        for (int i = 0; i < transferAmount; i++)
        {
            if (item.itemData.itemType == ItemType.Material)
            {
                var itemToAdd = new Inventory_Item(item.itemData);
                playerInventory.RemoveOneItem(item);
                AddMaterialToStash(itemToAdd);
            }
            else if (item.itemData.itemType == ItemType.Weapon ||
                     item.itemData.itemType == ItemType.Armor ||
                     item.itemData.itemType == ItemType.trinket)
            {
                if (CanAddItem(item))
                {
                    var itemToAdd = new Inventory_Item(item.itemData);
                    playerInventory.equipmentInventory.RemoveOneItem(item);
                    AddItem(itemToAdd);
                }
                else
                {
                    Debug.LogWarning("No space in storage for this equipment item.");
                    break;
                }
            }
            else
            {
                if (CanAddItem(item))
                {
                    var itemToAdd = new Inventory_Item(item.itemData);
                    playerInventory.RemoveOneItem(item);
                    AddItem(itemToAdd);
                }
                else
                {
                    Debug.LogWarning("No space in storage for this item.");
                    break;
                }
            }
        }

        NotifyInventoryChanged();
    }

    public void FromStorageToPlayer(Inventory_Item item, bool transferFullStack)
    {
        int transferAmount = transferFullStack ? item.stackSize : 1;

        for (int i = 0; i < transferAmount; i++)
        {
            if (playerInventory.CanAddItem(item))
            {
                var itemToAdd = new Inventory_Item(item.itemData);
                RemoveOneItem(item);
                playerInventory.AddItem(itemToAdd);
            }
            else if (item.itemData.itemType == ItemType.Weapon ||
                     item.itemData.itemType == ItemType.Armor ||
                     item.itemData.itemType == ItemType.trinket)
            {
                if (playerInventory.equipmentInventory.CanAddItem(item))
                {
                    var itemToAdd = new Inventory_Item(item.itemData);
                    RemoveOneItem(item);
                    playerInventory.equipmentInventory.AddItem(itemToAdd);
                }
                else
                {
                    Debug.LogWarning("No space in equipment inventory for this equipment item.");
                    break;
                }
            }
            else
            {
                Debug.LogWarning("No space in player inventory for this item.");
                break;
            }
        }

        NotifyInventoryChanged();
    }
}
