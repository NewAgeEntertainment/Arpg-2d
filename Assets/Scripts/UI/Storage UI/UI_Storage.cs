using UnityEngine;
using System.Collections.Generic;

public class UI_Storage : MonoBehaviour
{
    [Header("Slot Parents")]
    [SerializeField] private UI_ItemSlotParent playerInventorySlotParent;   // Backpack + unequipped gear
    [SerializeField] private UI_ItemSlotParent storageSlotParent;           // Storage (non-materials)
    [SerializeField] private UI_ItemSlotParent materialStashSlotParent;     // Materials-only

    private Inventory_Player playerInventory;
    private Inventory_Equipment equipmentInventory;
    private Inventory_Storage storage;

    // prevent double-subscriptions if SetupStorageUI is called more than once
    private bool subscribed;

    /// <summary>
    /// Call this when opening the storage panel (or once you have a storage reference).
    /// </summary>
    public void SetupStorageUI(Inventory_Storage storage)
    {
        UnhookEvents();

        this.storage = storage;
        playerInventory = storage != null ? storage.playerInventory : null;
        equipmentInventory = playerInventory != null ? playerInventory.equipmentInventory : null;

        // pass storage ref to any child storage slots (for move buttons, etc.)
        var storageSlots = GetComponentsInChildren<UI_StorageSlot>(true);
        for (int i = 0; i < storageSlots.Length; i++)
            storageSlots[i].SetStorage(this.storage);

        HookEvents();
        ForceRefresh();
    }

    private void OnEnable()
    {
        HookEvents();
        ForceRefresh();
    }

    private void OnDisable() => UnhookEvents();
    private void OnDestroy() => UnhookEvents();

    private void HookEvents()
    {
        if (subscribed) return;

        if (storage != null)
            storage.OnInventoryChange += UpdateUI;

        if (playerInventory != null)
            playerInventory.OnInventoryChange += UpdateUI;

        if (equipmentInventory != null)
            equipmentInventory.OnInventoryChange += UpdateUI;

        subscribed = true;
    }

    private void UnhookEvents()
    {
        if (!subscribed) return;

        if (storage != null)
            storage.OnInventoryChange -= UpdateUI;

        if (playerInventory != null)
            playerInventory.OnInventoryChange -= UpdateUI;

        if (equipmentInventory != null)
            equipmentInventory.OnInventoryChange -= UpdateUI;

        subscribed = false;
    }

    /// <summary>External nudge (e.g., from GameDataSaver after load).</summary>
    public void ForceRefresh() => UpdateUI();

    /// <summary>Rebuild all three lists: Player (backpack + unequipped), Storage, Materials.</summary>
    public void UpdateUI()
    {
        if (playerInventorySlotParent == null || storageSlotParent == null || materialStashSlotParent == null)
        {
            Debug.LogWarning("[UI_Storage] Slot parents are not assigned.");
            return;
        }

        // Player: backpack + unequipped gear
        var combinedPlayer = new List<Inventory_Item>();
        if (playerInventory != null)
        {
            if (playerInventory.itemList != null)
                combinedPlayer.AddRange(playerInventory.itemList);
            if (playerInventory.equipmentInventory != null && playerInventory.equipmentInventory.itemList != null)
                combinedPlayer.AddRange(playerInventory.equipmentInventory.itemList);
        }
        playerInventorySlotParent.UpdateSlots(combinedPlayer);

        // Storage items (non-materials)
        if (storage != null && storage.itemList != null)
            storageSlotParent.UpdateSlots(storage.itemList);
        else
            storageSlotParent.UpdateSlots(new List<Inventory_Item>());

        // Materials stash
        if (storage != null && storage.materialStash != null)
            materialStashSlotParent.UpdateSlots(storage.materialStash);
        else
            materialStashSlotParent.UpdateSlots(new List<Inventory_Item>());
    }
}
