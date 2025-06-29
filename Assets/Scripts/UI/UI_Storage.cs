using UnityEngine;
using System.Collections.Generic;

public class UI_Storage : MonoBehaviour
{
    [SerializeField] private UI_ItemSlotParent playerInventorySlotParent; // Shows BOTH backpack + unequipped gear
    [SerializeField] private UI_ItemSlotParent storageSlotParent;
    [SerializeField] private UI_ItemSlotParent materialStashSlotParent;

    private Inventory_Player playerInventory;
    private Inventory_Storage storage;

    public void SetupStorage(Inventory_Player player, Inventory_Storage storage)
    {
        this.playerInventory = player;
        this.storage = storage;

        storage.SetInventory(player);

        storage.OnInventoryChange += UpdateUI;
        playerInventory.OnInventoryChange += UpdateUI;
        playerInventory.equipmentInventory.OnInventoryChange += UpdateUI;

        foreach (var slot in GetComponentsInChildren<UI_StorageSlot>())
            slot.SetStorage(storage);

        UpdateUI();
    }

    public void UpdateUI()
    {
        var combined = new List<Inventory_Item>();
        combined.AddRange(playerInventory.itemList);  // backpack items
        combined.AddRange(playerInventory.equipmentInventory.itemList);  // unequipped gear too

        playerInventorySlotParent.UpdateSlots(combined);
        storageSlotParent.UpdateSlots(storage.itemList);
        materialStashSlotParent.UpdateSlots(storage.materialStash);
    }

    private void OnDisable()
    {
        if (storage != null) storage.OnInventoryChange -= UpdateUI;
        if (playerInventory != null) playerInventory.OnInventoryChange -= UpdateUI;
        if (playerInventory?.equipmentInventory != null) playerInventory.equipmentInventory.OnInventoryChange -= UpdateUI;
    }
}
