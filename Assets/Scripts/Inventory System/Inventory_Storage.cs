using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    private Inventory_Player playerInventory;
    public List<Inventory_Item> materialStash;

    public void AddMaterialToStash(Inventory_Item itemToAdd)
    {
        var stackableItem = StackableInStash(itemToAdd);

        if (stackableItem != null)
            stackableItem.AddStack(); // If a stackable item is found, increase its stack size
        else
            materialStash.Add(itemToAdd); // Otherwise, add the new item to the stash

        NotifyInventoryChanged(); // ✅ This will trigger UI update via the event
        
    }

    public Inventory_Item StackableInStash(Inventory_Item itemToAdd)
    {
        List<Inventory_Item> stackableItems = materialStash.FindAll(item => item.itemData == itemToAdd.itemData);

        foreach (var stackableItem in stackableItems)
        {
            if (stackableItem.CanAddStack())
                return stackableItem;
        }
        return null;
    }

    public void SetInventory(Inventory_Player inventory) => this.playerInventory = inventory;

    public void FromPlayerToStorage(Inventory_Item item,bool transferFullStack)
    {
        int transferAmount = transferFullStack ? item.stackSize : 1; // Determine how many items to transfer based on the transferFullStack flag

        for (int i = 0; i < transferAmount; i++)
        {
            if (CanAddItem(item))
            {
                var itemToAdd = new Inventory_Item(item.itemData); // Create a new item instance to avoid modifying the original item
            
                playerInventory.RemoveOneItem(item); // Remove the item from the player's inventory
                AddItem(itemToAdd); // Add the item to the storage inventory
            }

        }

        NotifyInventoryChanged(); // ✅ This will trigger UI update via the event
        
    }

    public void FromStorageToPlayer(Inventory_Item item,bool transferFullStack)
    {
        int transferAmount = transferFullStack ? item.stackSize : 1; // Determine how many items to transfer based on the transferFullStack flag

        for (int i = 0; i < transferAmount; i++)
        {
            if (playerInventory.CanAddItem(item))
            {
                var itemToAdd = new Inventory_Item(item.itemData); // Create a new item instance to avoid modifying the original item

                RemoveOneItem(item); // Remove the item from the storage inventory
                playerInventory.AddItem(itemToAdd); // Add the item to the player's inventory
            }

        }


        NotifyInventoryChanged();
    } 
}

