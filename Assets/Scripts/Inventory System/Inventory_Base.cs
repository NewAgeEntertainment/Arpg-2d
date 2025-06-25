using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Base : MonoBehaviour
{
    public event Action OnInventoryChange; // Event to notify when the inventory changes
    [SerializeField] private Player player; // Assign in inspector or via code
    public int maxInventorySize = 10; // Maximum number of items allowed in the inventory  

    //[SerializeField] // Ensure the list is visible in the Inspector  
    public List<Inventory_Item> itemList = new List<Inventory_Item>(); // List to hold inventory items  

    protected virtual void Awake()
    {
      
    }
    
    public void TryUseItem(Inventory_Item itemToUse)
    {
        Inventory_Item consumable = itemList.Find(item => item == itemToUse);

        if (consumable == null)
            return; // If the item is not found in the inventory, exit the method

        consumable.itemEffect.ExecuteEffect(); // 👈 Pass player directly

        if (consumable.stackSize > 1)
            consumable.RemoveStack();
        else
            RemoveOneItem(consumable); // If the item is a consumable and its stack size is 1, remove it from the inventory
    
        OnInventoryChange?.Invoke(); // Invoke the event to notify subscribers of the change
    }

    public bool CanAddItem(Inventory_Item itemToAdd)
    {
        bool hasStackable = FindStackable(itemToAdd) != null; // Check if there is an existing stackable item in the inventory  
        return hasStackable || itemList.Count < maxInventorySize; // Check if the inventory can accept more items  
    }

    public Inventory_Item FindStackable(Inventory_Item itemToAdd)
    {
        List<Inventory_Item> stackableItems = itemList.FindAll(item => item.itemData == itemToAdd.itemData);
    
        foreach (var stackableItem in stackableItems)
        {
            if (stackableItem.CanAddStack())
                return stackableItem;
        }

        return null;
    }

    public void AddItem(Inventory_Item itemToAdd) // Method to add an item to the inventory  
    {


        Inventory_Item itemInInventory = FindStackable(itemToAdd); // Check if the item already exists in the inventory

        if (itemInInventory != null)
            itemInInventory.AddStack(); // If it exists, increase the stack size
        else
            itemList.Add(itemToAdd); // If it doesn't exist, add the new item to the inventory

        OnInventoryChange?.Invoke(); // Invoke the event to notify subscribers of the change
    }

    public void RemoveOneItem(Inventory_Item itemToRemove)
    {
        Inventory_Item itemInInventory = itemList.Find(item => item == itemToRemove); // Find the item in the inventory

        if (itemInInventory.stackSize > 1)
            itemInInventory.RemoveStack();
        else
            itemList.Remove(itemInInventory); // If the stack size is 1, remove the item from the inventory


        OnInventoryChange?.Invoke(); // Invoke the event to notify subscribers of the change
    }

    public Inventory_Item FindItem(ItemDataSO itemData) // Method to find an item in the inventory by its data  
    {
        return itemList.Find(item => item.itemData == itemData); // Return the first matching item or null if not found
    }

    public void TriggerUpdateUI() => OnInventoryChange?.Invoke(); // Method to trigger the UI update event, notifying subscribers that the inventory has changed
}
