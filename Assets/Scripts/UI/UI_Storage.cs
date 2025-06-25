using Spine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Storage : MonoBehaviour
{
    private Inventory_Player inventory;
    private Inventory_Storage storage;

    [SerializeField] private UI_ItemSlotParent inventoryParent;
    [SerializeField] private UI_ItemSlotParent storageParent;
    [SerializeField] private UI_ItemSlotParent materialStashParent;

    public void SetupStorage(Inventory_Player inventory, Inventory_Storage storage)
    {
        this.inventory = inventory;
        this.storage = storage;
        // Initialize the UI with the current items in storage
        storage.OnInventoryChange += UpdateUI;
        UpdateUI();

        UI_StorageSlot[] storageSlots = GetComponentsInChildren<UI_StorageSlot>();
        
        foreach (var slot in storageSlots)
            slot.SetStorage(storage);
    }

    private void UpdateUI()
    {
        inventoryParent.UpdateSlots(inventory.itemList);
        storageParent.UpdateSlots(storage.itemList);
        materialStashParent.UpdateSlots(storage.materialStash);
    }
}
