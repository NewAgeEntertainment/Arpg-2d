using System.Collections.Generic;
using UnityEngine;

public class UI_Craft : MonoBehaviour
{
    [Header("Backpack & Equip Slots")]
    [SerializeField] private UI_ItemSlotParent inventoryParent; // shows BOTH bags

    private Inventory_Player playerInventory;
    private Inventory_Storage storage;

    private UI_CraftPreview craftPreviewUI;
    private UI_CraftSlot[] craftSlots;
    private UI_CraftListBtn[] craftListButtons;

    public void SetupCraftUI(Inventory_Storage storage)
    {
        this.storage = storage;
        playerInventory = storage.playerInventory;

        Debug.Log($"[UI_Craft] Setup: Backpack {playerInventory.itemList.Count} Equip {playerInventory.equipmentInventory.itemList.Count}");

        craftPreviewUI = GetComponentInChildren<UI_CraftPreview>();
        craftPreviewUI.SetupCraftPreview(storage);

        SetupCraftListButtons();

        playerInventory.OnInventoryChange += UpdateUI;
        playerInventory.equipmentInventory.OnInventoryChange += UpdateUI;
        storage.OnInventoryChange += UpdateUI;

        UpdateUI(); // 👈 refresh right away
    }

    private void SetupCraftListButtons()
    {
        craftSlots = GetComponentsInChildren<UI_CraftSlot>(true);
        craftListButtons = GetComponentsInChildren<UI_CraftListBtn>(true);

        foreach (var slot in craftSlots)
            slot.gameObject.SetActive(false);

        foreach (var btn in craftListButtons)
            btn.setCraftSlots(craftSlots);
    }

    private void OnEnable()
    {
        UpdateUI(); // 👈 refresh if re-activated
    }

    public void UpdateUI()
    {
        var combined = new List<Inventory_Item>();
        combined.AddRange(playerInventory.itemList);
        combined.AddRange(playerInventory.equipmentInventory.itemList);

        Debug.Log($"[UI_Craft] UpdateUI → CombinedItems: {combined.Count}");

        foreach (var slot in inventoryParent.GetComponentsInChildren<UI_StorageSlot>(true))
            slot.SetStorage(storage);

        inventoryParent.UpdateSlots(combined);
    }
}
