using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public int gold = 10000;

    public event Action<int> OnQuickSlotUsed;

    public Inventory_Equipment equipmentInventory;
    public Inventory_Storage storage;

    public List<Inventory_Equipped> equipList = new List<Inventory_Equipped>();

    [Serializable]
    public struct QuickSlot
    {
        public Inventory_Item item;
        public int slotStack;
    }

    public QuickSlot[] quickSlots = new QuickSlot[4];

    protected override void Awake()
    {
        base.Awake();

        // ✅ Always ensure array is the correct size!
        if (quickSlots == null || quickSlots.Length != 4)
        {
            quickSlots = new QuickSlot[4];
            Debug.Log("[Inventory_Player] quickSlots auto-sized to 4 in Awake");
        }

        equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();
        storage = FindFirstObjectByType<Inventory_Storage>();
    }

    public void SetQuickItemInSlot(int slotNumber, Inventory_Item itemToSet, int amount)
    {
        if (slotNumber < 1 || slotNumber > quickSlots.Length)
        {
            Debug.LogWarning($"[Inventory_Player] Invalid quick slot number: {slotNumber}");
            return;
        }

        // 🗝️ Count how many exist in backpack
        int totalOwned = 0;
        foreach (var item in itemList)
        {
            if (item.itemData == itemToSet.itemData)
                totalOwned += item.stackSize;
        }

        if (totalOwned <= 0)
        {
            Debug.LogWarning($"[Inventory_Player] No {itemToSet.itemData.itemName} found in backpack!");
            return;
        }

        // 🗝️ Count how many already assigned across ALL quick slots (except this slot)
        int alreadyAssignedElsewhere = 0;
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (i == slotNumber - 1) continue; // skip this slot

            QuickSlot qs = quickSlots[i];
            if (qs.item != null && qs.item.itemData == itemToSet.itemData)
                alreadyAssignedElsewhere += qs.slotStack;
        }

        // 🗝️ Combine with what this slot currently has if same item
        QuickSlot currentSlot = quickSlots[slotNumber - 1];
        int newStack = amount;

        if (currentSlot.item != null && currentSlot.item.itemData == itemToSet.itemData)
        {
            newStack = currentSlot.slotStack + amount;
        }

        // 🔒 Clamp: You can’t assign more than you actually own minus other slots
        int maxPossible = totalOwned - alreadyAssignedElsewhere;
        newStack = Mathf.Clamp(newStack, 1, maxPossible);

        quickSlots[slotNumber - 1].item = itemToSet;
        quickSlots[slotNumber - 1].slotStack = newStack;

        Debug.Log($"[Inventory_Player] Assigned {itemToSet.itemData.itemName} → New Quick Slot {slotNumber} Stack: {newStack} (Owned: {totalOwned} | Assigned Elsewhere: {alreadyAssignedElsewhere})");

        NotifyInventoryChanged();
    }




    public void TryUseQuickItemInSlot(int slotNumber)
    {
        int index = slotNumber - 1;

        if (index < 0 || index >= quickSlots.Length)
        {
            Debug.LogWarning($"[Inventory_Player] Invalid quick slot index: {index}");
            return;
        }

        var quickSlot = quickSlots[index];

        if (quickSlot.item == null || quickSlot.slotStack <= 0)
        {
            Debug.Log($"[Inventory_Player] Quick Slot {slotNumber} is empty or out of uses");
            return;
        }

        TryUseItem(quickSlot.item, this.player);

        quickSlot.slotStack--;

        if (quickSlot.slotStack <= 0)
        {
            quickSlot.item = null;
            quickSlot.slotStack = 0;
        }

        quickSlots[index] = quickSlot;

        NotifyInventoryChanged();
        OnQuickSlotUsed?.Invoke(index);

        Debug.Log($"[Inventory_Player] Used Quick Slot {slotNumber}. Remaining: {quickSlot.slotStack}");
    }

    public override void AddItem(Inventory_Item itemToAdd)
    {
        Debug.Log($"[Inventory_Player] Adding {itemToAdd.itemData.itemName}");

        if (itemToAdd.itemData.itemType == ItemType.Material && storage != null)
        {
            storage.AddMaterialToStash(itemToAdd);
            return;
        }

        if (itemToAdd.itemData.itemType == ItemType.Weapon ||
            itemToAdd.itemData.itemType == ItemType.Armor ||
            itemToAdd.itemData.itemType == ItemType.trinket)
        {
            if (equipmentInventory.CanAddItem(itemToAdd))
            {
                equipmentInventory.AddItem(itemToAdd);
                return;
            }
        }

        base.AddItem(itemToAdd);
    }

    public void TryEquipFromEquipmentInventory(Inventory_Item item)
    {
        var inventoryItem = equipmentInventory.FindItem(item.itemData);
        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);

        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                EquipItem(inventoryItem, slot);
                equipmentInventory.RemoveOneItem(inventoryItem);
                return;
            }
        }

        var slotToReplace = matchingSlots[0];
        var itemToUnequip = slotToReplace.equipedItem;

        UnequipItem(itemToUnequip, true);
        EquipItem(inventoryItem, slotToReplace);
        equipmentInventory.RemoveOneItem(inventoryItem);
    }

    private void EquipItem(Inventory_Item itemToEquip, Inventory_Equipped slot)
    {
        slot.equipedItem = itemToEquip;
        slot.equipedItem.AddModifiers(player.stats);
        slot.equipedItem.AddItemEffect(player);

        equipmentInventory.RemoveOneItem(itemToEquip);

        NotifyInventoryChanged();
    }

    public void UnequipItem(Inventory_Item itemToUnequip, bool replacing = false)
    {
        var slot = equipList.Find(slot => slot.equipedItem == itemToUnequip);
        if (slot != null)
            slot.equipedItem = null;

        itemToUnequip.RemoveModifiers(player.stats);
        itemToUnequip.RemoveItemEffect();

        equipmentInventory.AddItem(itemToUnequip);

        NotifyInventoryChanged();
    }

    public void SwapEquippedItem(Inventory_Item oldEquippedItem, Inventory_Item newInventoryItem)
    {
        var equippedSlot = equipList.Find(slot => slot.equipedItem == oldEquippedItem);
        if (equippedSlot == null)
        {
            Debug.LogWarning("[Inventory_Player] No equipped slot found for item.");
            return;
        }

        // Unequip current
        UnequipItem(oldEquippedItem, replacing: true);

        // Equip new item
        EquipItem(newInventoryItem, equippedSlot);

        // Remove one of the new item from equipment inventory
        equipmentInventory.RemoveOneItem(newInventoryItem);

        Debug.Log($"[Inventory_Player] Swapped {oldEquippedItem.itemData.itemName} with {newInventoryItem.itemData.itemName}.");
    }
}
