using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Base : MonoBehaviour
{
    public event Action onInventoryChange; // Event to notify when the inventory changes

    public int maxInventorySize = 10; // Maximum number of items allowed in the inventory  

    //[SerializeField] // Ensure the list is visible in the Inspector  
    public List<Inventory_Item> itemList = new List<Inventory_Item>(); // List to hold inventory items  

   

    public bool CanAddItem() => itemList.Count < maxInventorySize; // Check if the inventory can accept more items  

    public void AddItem(Inventory_Item itemToAdd) // Method to add an item to the inventory  
    {


        Inventory_Item itemInInventory = FindItem(itemToAdd.itemData); // Check if the item already exists in the inventory

        if (itemInInventory != null)
            itemInInventory.AddStack(); // If it exists, increase the stack size
        else
            itemList.Add(itemToAdd); // If it doesn't exist, add the new item to the inventory

        onInventoryChange?.Invoke(); // Invoke the event to notify subscribers of the change
    }

    public Inventory_Item FindItem(ItemDataSO itemData) // Method to find an item in the inventory by its data  
    {
        return itemList.Find(item => item.itemData == itemData && item.CanAddStack()); // Return the first matching item or null if not found
    }
}
