using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    
    private Inventory_Player inventory;
    private UI_ItemSlot[] uiItemSlots;
    private UI_EquipSlot[] uiEquipSlots;

    [SerializeField] private Transform uiItemSlotParent;
    [SerializeField] private Transform uiEquipSlotParent;

    private void Awake()
    {
        uiItemSlots = uiItemSlotParent.GetComponentsInChildren<UI_ItemSlot>();
        uiEquipSlots = uiEquipSlotParent.GetComponentsInChildren<UI_EquipSlot>();

        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.onInventoryChange += UpdateUI; // Subscribe to the inventory change event
    
        UpdateUI(); // Initial update to populate the UI with current inventory items
    }

    private void UpdateUI()
    {
        UpdateInventorySlots(); // Update the inventory slots
        UpdateEquipmentSlots(); // Update the equipment slots
    }

    private void UpdateEquipmentSlots()
    {
        List<Inventory_EquipmentSlot> playerEquipList = inventory.equipList;
    
        for (int i = 0; i < uiEquipSlots.Length; i++)
        {
            var playerEquipSlot = playerEquipList[i];

            if (playerEquipSlot.HasItem() == false)
                uiEquipSlots[i].UpdateSlot(null); // Clear the slot if no item is present
            else
                uiEquipSlots[i].UpdateSlot(playerEquipSlot.equipedItem); // Update the slot with the equipped item
        }

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
