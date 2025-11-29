using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStartingEquipment : MonoBehaviour
{
    [Header("References (auto-filled if left empty)")]
    [SerializeField] private Inventory_Player playerInventory;
    [SerializeField] private Inventory_Equipment equipmentInventory;

    [Header("Starting Equipment (auto-equipped)")]
    [Tooltip("Items the player should start with equipped (Weapon/Armor/trinket only).")]
    [SerializeField] private List<ItemDataSO> startingEquipment = new();

    [Header("Guard")]
    [Tooltip("If true, only applies if the player has nothing equipped yet (good with save/load).")]
    [SerializeField] private bool onlyIfNoEquipment = true;

    private bool _applied = false;

    private void Start()
    {
        // Make sure references are set
        if (playerInventory == null)
            playerInventory = GetComponent<Inventory_Player>();

        if (playerInventory == null)
        {
            Debug.LogWarning("[PlayerStartingEquipment] No Inventory_Player found on this object.");
            return;
        }

        if (equipmentInventory == null)
            equipmentInventory = playerInventory.equipmentInventory;

        if (equipmentInventory == null)
            equipmentInventory = GetComponent<Inventory_Equipment>();

        if (equipmentInventory == null)
        {
            Debug.LogWarning("[PlayerStartingEquipment] No Inventory_Equipment found.");
            return;
        }

        TryApplyStartingEquipment();
    }

    private void TryApplyStartingEquipment()
    {
        if (_applied) return; // safety if Start somehow runs twice
        _applied = true;

        // Optional: don’t touch anything if we already have gear equipped
        if (onlyIfNoEquipment && HasAnyEquipped(playerInventory))
        {
            Debug.Log("[PlayerStartingEquipment] Skipping: player already has equipment.");
            return;
        }

        if (startingEquipment == null || startingEquipment.Count == 0)
        {
            Debug.Log("[PlayerStartingEquipment] No starting equipment configured.");
            return;
        }

        foreach (var data in startingEquipment)
        {
            if (data == null) continue;

            // Only handle equipment types your equipment inventory supports
            if (data.itemType != ItemType.Weapon &&
                data.itemType != ItemType.Armor &&
                data.itemType != ItemType.trinket)
            {
                Debug.LogWarning($"[PlayerStartingEquipment] '{data.itemName}' is not Weapon/Armor/trinket. Skipping.");
                continue;
            }

            // Create a single instance
            var item = new Inventory_Item(data) { stackSize = 1 };

            // Put it into the equipment inventory (no pickup toast, no backpack)
            bool added = equipmentInventory.AddItem(item);
            if (!added)
            {
                Debug.LogWarning($"[PlayerStartingEquipment] Could not add starting item '{data.itemName}' to equipment inventory.");
                continue;
            }

            // Use your existing equip logic to place it in the correct slot
            playerInventory.TryEquipFromEquipmentInventory(item);
        }

        // Let HUD/UI know inventories changed
        playerInventory.NotifyInventoryChanged();
    }

    private bool HasAnyEquipped(Inventory_Player inv)
    {
        if (inv == null || inv.equipList == null) return false;

        foreach (var slot in inv.equipList)
        {
            if (slot != null && slot.HasItem())
                return true;
        }
        return false;
    }
}

