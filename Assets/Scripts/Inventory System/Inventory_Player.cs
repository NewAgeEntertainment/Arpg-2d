using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public int gold = 10000;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnQuickSlotUsed;

    // 🔸 NEW: generic “inventory changed” event
    public event Action OnInventoryChange;

    [Header("Assigned References")]
    [SerializeField] private Inventory_Equipment equipmentInventoryRef;
    [SerializeField] private Inventory_Storage storageRef;

    public Inventory_Equipment equipmentInventory { get; private set; }
    public Inventory_Storage storage { get; private set; }

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

        if (quickSlots == null || quickSlots.Length != 4)
        {
            quickSlots = new QuickSlot[4];
            Debug.Log("[Inventory_Player] quickSlots auto-sized to 4 in Awake");
        }

        equipmentInventory = equipmentInventoryRef != null ? equipmentInventoryRef : FindFirstObjectByType<Inventory_Equipment>();
        storage = storageRef != null ? storageRef : FindFirstObjectByType<Inventory_Storage>();

        if (equipmentInventory == null)
            Debug.LogWarning("[Inventory_Player] Equipment inventory not found. Assign via Inspector.");
        if (storage == null)
            Debug.LogWarning("[Inventory_Player] Storage not found. Assign via Inspector.");
    }

    // 🔸 helper all UIs call
    public void TriggerUpdateUI() => OnInventoryChange?.Invoke();

    // If Inventory_Base already has a NotifyInventoryChanged(), keep using it,
    // but also raise our event here by shadowing with 'new' (safe if base is virtual/none).
    protected new void NotifyInventoryChanged()
    {
        // call base if it exists & is accessible
        try { base.NotifyInventoryChanged(); } catch { /* base may be non-virtual/private */ }
        OnInventoryChange?.Invoke();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        OnInventoryChange?.Invoke();

        var ui = FindFirstObjectByType<UI_InGame>();
        if (ui != null)
            ui.ShowGoldPickup(amount);
    }

    public void SetQuickItemInSlot(int slotNumber, Inventory_Item itemToSet, int amount)
    {
        if (slotNumber < 1 || slotNumber > quickSlots.Length)
        {
            Debug.LogWarning($"[Inventory_Player] Invalid quick slot number: {slotNumber}");
            return;
        }

        int totalOwned = 0;
        foreach (var item in itemList)
            if (item.itemData == itemToSet.itemData)
                totalOwned += item.stackSize;

        if (totalOwned <= 0)
        {
            Debug.LogWarning($"[Inventory_Player] No {itemToSet.itemData.itemName} found in backpack!");
            return;
        }

        int alreadyAssignedElsewhere = 0;
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (i == slotNumber - 1) continue;
            var qs = quickSlots[i];
            if (qs.item != null && qs.item.itemData == itemToSet.itemData)
                alreadyAssignedElsewhere += qs.slotStack;
        }

        QuickSlot currentSlot = quickSlots[slotNumber - 1];
        int newStack = amount;

        if (currentSlot.item != null && currentSlot.item.itemData == itemToSet.itemData)
            newStack = currentSlot.slotStack + amount;

        int maxPossible = totalOwned - alreadyAssignedElsewhere;
        newStack = Mathf.Clamp(newStack, 1, maxPossible);

        quickSlots[slotNumber - 1].item = itemToSet;
        quickSlots[slotNumber - 1].slotStack = newStack;

        Debug.Log($"[Inventory_Player] Assigned {itemToSet.itemData.itemName} → Slot {slotNumber} Stack: {newStack} (Owned: {totalOwned}, Assigned: {alreadyAssignedElsewhere})");
        NotifyInventoryChanged();
    }

    public Inventory_Item GetEquippedItemByType(ItemType type)
    {
        if (equipList == null) return null;

        foreach (var eq in equipList)
        {
            if (eq == null) continue;
            var item = eq.equipedItem;
            if (item != null && item.itemData != null && item.itemData.itemType == type)
                return item;
        }

        return null;
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

    public override bool AddItem(Inventory_Item itemToAdd)
    {
        if (itemToAdd == null || itemToAdd.itemData == null)
        {
            Debug.LogWarning("[Inventory_Player] Tried to add null item.");
            return false;
        }

        Debug.Log($"[Inventory_Player] Adding {itemToAdd.itemData.itemName}");

        if (itemToAdd.itemData.itemType == ItemType.Material && storage != null)
        {
            var addedToStorage = storage.AddMaterialToStash(itemToAdd);
            if (addedToStorage) NotifyInventoryChanged();
            return addedToStorage;
        }

        if ((itemToAdd.itemData.itemType == ItemType.Weapon ||
             itemToAdd.itemData.itemType == ItemType.Armor ||
             itemToAdd.itemData.itemType == ItemType.trinket) &&
            equipmentInventory != null && equipmentInventory.CanAddItem(itemToAdd))
        {
            var addedToEquip = equipmentInventory.AddItem(itemToAdd);
            if (addedToEquip) NotifyInventoryChanged();
            return addedToEquip;
        }

        var added = base.AddItem(itemToAdd);
        if (added) NotifyInventoryChanged();
        return added;
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

        UnequipItem(oldEquippedItem, true);
        EquipItem(newInventoryItem, equippedSlot);
        equipmentInventory.RemoveOneItem(newInventoryItem);

        Debug.Log($"[Inventory_Player] Swapped {oldEquippedItem.itemData.itemName} with {newInventoryItem.itemData.itemName}.");
        NotifyInventoryChanged();
    }

    public void UnequipItemByType(ItemType slotType)
    {
        var equippedSlot = equipList.Find(slot => slot.slotType == slotType && slot.HasItem());
        if (equippedSlot != null)
        {
            UnequipItem(equippedSlot.equipedItem);
            Debug.Log($"[Inventory_Player] Unequipped item from slot type: {slotType}");
        }
        else
        {
            Debug.Log($"[Inventory_Player] No item equipped in slot type: {slotType}");
        }
    }

    public int CountEverywhere(ItemDataSO targetData)
    {
        int total = 0;

        total += CountItem(targetData);

        if (equipmentInventory != null)
        {
            foreach (var it in equipmentInventory.itemList)
                if (it.itemData == targetData)
                    total += it.stackSize;
        }

        if (equipList != null)
        {
            foreach (var eq in equipList)
                if (eq != null && eq.HasItem() && eq.equipedItem.itemData == targetData)
                    total += 1;
        }

        if (storage != null)
        {
            foreach (var it in storage.itemList)
                if (it.itemData == targetData)
                    total += it.stackSize;
        }

        return total;
    }

    public int CountItem(ItemDataSO targetData)
    {
        int count = 0;
        foreach (var item in itemList)
            if (item.itemData == targetData)
                count += item.stackSize;
        return count;
    }

    public int CountAssigned(ItemDataSO targetData)
    {
        int assigned = 0;
        foreach (var slot in quickSlots)
            if (slot.item != null && slot.item.itemData == targetData)
                assigned += slot.slotStack;
        return assigned;
    }
}
