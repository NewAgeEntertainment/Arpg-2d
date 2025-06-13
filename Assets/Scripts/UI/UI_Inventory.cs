using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    
    private UI_ItemSlot[] uiItemSlots;
    private Inventory_Base inventory;


    private void Awake()
    {
        uiItemSlots = GetComponentsInChildren<UI_ItemSlot>();
        inventory = FindFirstObjectByType<Inventory_Base>();
        inventory.onInventoryChange += UpdateInventorySlots; // Subscribe to the inventory change event
    
        UpdateInventorySlots(); // Initial update to populate the UI with current inventory items
    }

    private void UpdateInventorySlots()
    {
        List<Inventory_Item> itemList = inventory.itemList;

        for (int i = 0; i < uiItemSlots.Length; i++)
        {
            if (i < itemList.Count)
            {
                uiItemSlots[i].UpdateSlot(itemList[i]);
            }
            else
            {
                uiItemSlots[i].UpdateSlot(null); // Clear the slot if no item is present
            }
        }
    }
}
