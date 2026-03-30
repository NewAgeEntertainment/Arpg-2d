// Assets/Scripts/Inventory/Inventory_Player.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public int gold = 10000;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnQuickSlotUsed;
    public new event Action OnInventoryChange; // HUD listeners

    [Header("Assigned References")]
    [SerializeField] private Inventory_Equipment equipmentInventoryRef;
    [SerializeField] private Inventory_Storage storageRef;

    public Inventory_Equipment equipmentInventory { get; private set; }
    public Inventory_Storage storage { get; private set; }

    public List<Inventory_Equipped> equipList = new List<Inventory_Equipped>();

    private CharacterEquipmentProfile equipmentProfile;

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
        equipmentProfile = GetComponent<CharacterEquipmentProfile>();
        if (equipmentProfile == null)
            equipmentProfile = gameObject.AddComponent<CharacterEquipmentProfile>();

        equipmentProfile.InitializeBridge(this, player != null ? player.stats : GetComponent<Entity_Stats>());
        storage = storageRef != null ? storageRef : FindFirstObjectByType<Inventory_Storage>();

        if (equipmentInventory == null) Debug.LogWarning("[Inventory_Player] Equipment inventory not found. Assign via Inspector.");
        if (storage == null) Debug.LogWarning("[Inventory_Player] Storage not found. Assign via Inspector.");
    }

    // ---- Notify helpers ----
    public new void NotifyInventoryChanged()
    {
        try { base.NotifyInventoryChanged(); } catch { }
        OnInventoryChange?.Invoke();
    }

    public void TriggerUpdateUI() => OnInventoryChange?.Invoke();

    // ---- Currency ----
    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        OnInventoryChange?.Invoke();

        var ui = FindFirstObjectByType<UI_InGame>();
        if (ui != null) ui.ShowGoldPickup(amount);
    }

    // ---- Quick Slots API ----
    public void SetQuickItemInSlot(int slotNumber, Inventory_Item sourceItem, int amount, bool useFreshInstance = true)
    {
        if (slotNumber < 1 || slotNumber > quickSlots.Length)
        {
            Debug.LogWarning($"[Inventory_Player] Invalid quick slot number: {slotNumber}");
            return;
        }
        if (sourceItem == null || sourceItem.itemData == null)
        {
            Debug.LogWarning("[Inventory_Player] SetQuickItemInSlot: null item");
            return;
        }

        int available = CountItem(sourceItem.itemData);
        if (available <= 0)
        {
            Debug.LogWarning($"[Inventory_Player] No {sourceItem.itemData.itemName} in backpack to reserve.");
            return;
        }

        int toReserve = Mathf.Clamp(amount, 1, available);
        int actuallyReserved = RemoveFromBackpack(sourceItem.itemData, toReserve);
        if (actuallyReserved <= 0)
        {
            Debug.LogWarning($"[Inventory_Player] Failed to reserve {toReserve}x {sourceItem.itemData.itemName}.");
            return;
        }

        int idx = slotNumber - 1;
        var current = quickSlots[idx];

        if (current.item != null && current.item.itemData == sourceItem.itemData)
        {
            current.slotStack += actuallyReserved;
        }
        else
        {
            Inventory_Item slotItem = useFreshInstance ? CloneItemInstance(sourceItem) : sourceItem;
            current.item = slotItem;
            current.slotStack = actuallyReserved;
        }

        quickSlots[idx] = current;

        Debug.Log($"[Inventory_Player] Reserved {actuallyReserved}x {sourceItem.itemData.itemName} -> QuickSlot {slotNumber}. (Backpack -{actuallyReserved})");
        NotifyInventoryChanged();
    }

    public void ClearQuickSlot(int slotNumber)
    {
        int idx = slotNumber - 1;
        if (idx < 0 || idx >= quickSlots.Length) return;
        quickSlots[idx].item = null;
        quickSlots[idx].slotStack = 0;
        NotifyInventoryChanged();
    }

    public QuickSlot GetQuickSlot(int slotNumber)
    {
        int idx = slotNumber - 1;
        if (idx < 0 || idx >= quickSlots.Length) return default;
        return quickSlots[idx];
    }

    private static Inventory_Item CloneItemInstance(Inventory_Item src)
    {
        var copy = new Inventory_Item(src.itemData);
        copy.SetInstanceModifiers(src.GetInstanceModifiers());
        return copy;
    }

    public void TryUseQuickItemInSlot(int slotNumber)
    {
        int index = slotNumber - 1;
        if (index < 0 || index >= quickSlots.Length)
        {
            Debug.LogWarning($"[Inventory_Player] Invalid quick slot index: {index}");
            return;
        }

        var qs = quickSlots[index];
        if (qs.item == null || qs.slotStack <= 0)
        {
            Debug.Log($"[Inventory_Player] Quick Slot {slotNumber} empty/out.");
            return;
        }

        bool used = ApplyQuickSlotItemEffectWithoutChangingBackpack(qs.item);
        if (!used) return;

        qs.slotStack--;
        if (qs.slotStack <= 0) { qs.item = null; qs.slotStack = 0; }
        quickSlots[index] = qs;

        NotifyInventoryChanged();
        OnQuickSlotUsed?.Invoke(index);

        Debug.Log($"[Inventory_Player] Used Quick Slot {slotNumber}. Reserved left: {qs.slotStack}");
    }

    // -------- Helpers --------
    private int RemoveFromBackpack(ItemDataSO data, int amount)
    {
        int remaining = amount;
        for (int i = itemList.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var it = itemList[i];
            if (it == null || it.itemData != data) continue;

            int take = Mathf.Min(it.stackSize, remaining);
            it.stackSize -= take;
            remaining -= take;

            if (it.stackSize <= 0) itemList.RemoveAt(i);
        }

        int removed = amount - remaining;
        if (removed > 0) NotifyInventoryChanged();
        if (remaining > 0)
            Debug.LogWarning($"[Inventory_Player] Tried to reserve {amount} {data.name} but only {removed} were available.");
        return removed;
    }

    private bool ApplyQuickSlotItemEffectWithoutChangingBackpack(Inventory_Item item)
    {
        if (item == null || item.itemData == null) return false;

        var existing = itemList.Find(i => i != null && i.itemData == item.itemData);
        bool createdTemp = false;

        if (existing == null)
        {
            existing = new Inventory_Item(item.itemData);
            itemList.Add(existing);
            createdTemp = true;
        }
        else
        {
            existing.AddStack(1); // net-zero after successful use
        }

        bool used = TryUseItemChecked(existing, this.player);

        if (!used)
        {
            if (createdTemp) itemList.Remove(existing);
            else existing.RemoveStack(1);

            NotifyInventoryChanged();
        }

        return used;
    }

    // ---- Add / Equip / Storage ----
    // Inventory_Player.cs
    public override bool AddItem(Inventory_Item itemToAdd)
    {
        if (itemToAdd == null || itemToAdd.itemData == null)
        {
            Debug.LogWarning("[Inventory_Player] Tried to add null item.");
            return false;
        }

        Debug.Log($"[Inventory_Player] Adding {itemToAdd.itemData.itemName}");

        // MATERIAL → storage
        if (itemToAdd.itemData.itemType == ItemType.Material && storage != null)
        {
            var addedToStorage = storage.AddMaterialToStash(itemToAdd);
            if (addedToStorage)
            {
                NotifyInventoryChanged();
                var ui = FindFirstObjectByType<UI_InGame>();
                if (ui != null) ui.ShowItemPickup(itemToAdd.itemData.itemIcon, itemToAdd.itemData.itemName, itemToAdd.stackSize); // <-- ADD THIS
            }
            return addedToStorage;
        }

        // EQUIPMENT → equipment inventory
        if ((itemToAdd.itemData.itemType == ItemType.Weapon ||
             itemToAdd.itemData.itemType == ItemType.Armor ||
             itemToAdd.itemData.itemType == ItemType.trinket) &&
            equipmentInventory != null && equipmentInventory.CanAddItem(itemToAdd))
        {
            var addedToEquip = equipmentInventory.AddItem(itemToAdd);
            if (addedToEquip)
            {
                NotifyInventoryChanged();
                var ui = FindFirstObjectByType<UI_InGame>();
                if (ui != null) ui.ShowItemPickup(itemToAdd.itemData.itemIcon, itemToAdd.itemData.itemName, 1); // <-- ADD THIS
            }
            return addedToEquip;
        }

        // default → backpack
        var added = base.AddItem(itemToAdd);
        if (added)
        {
            NotifyInventoryChanged();
            var ui = FindFirstObjectByType<UI_InGame>();
            if (ui != null) ui.ShowItemPickup(itemToAdd.itemData.itemIcon, itemToAdd.itemData.itemName, itemToAdd.stackSize); // <-- ADD THIS
        }
        return added;
    }



    // Inventory_Player.cs  — add this convenience overload
    public bool AddItem(ItemDataSO data, int count = 1)
    {
        if (data == null || count <= 0) return false;

        var item = new Inventory_Item(data);
        item.stackSize = Mathf.Max(1, count);
        bool ok = AddItem(item);

        if (ok)
        {
            var ui = FindFirstObjectByType<UI_InGame>();
            if (ui != null) ui.ShowItemPickup(data.itemIcon, data.itemName, count); // <-- ADD THIS
        }
        return ok;
    }




    public Inventory_Item GetEquippedItemByType(ItemType type)
    {
        if (equipmentProfile == null)
            return null;

        return equipmentProfile.GetEquippedItemByType(type);
    }

    public void TryEquipFromEquipmentInventory(Inventory_Item item)
    {
        if (equipmentProfile == null)
        {
            Debug.LogWarning("[Inventory_Player] CharacterEquipmentProfile missing.");
            return;
        }

        equipmentProfile.TryEquipFromEquipmentInventory(item);
        NotifyInventoryChanged();
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
        if (equipmentProfile == null)
        {
            Debug.LogWarning("[Inventory_Player] CharacterEquipmentProfile missing.");
            return;
        }

        equipmentProfile.UnequipItem(itemToUnequip, replacing);
        NotifyInventoryChanged();
    }

    public void SwapEquippedItem(Inventory_Item oldEquippedItem, Inventory_Item newInventoryItem)
    {
        if (equipmentProfile == null)
        {
            Debug.LogWarning("[Inventory_Player] CharacterEquipmentProfile missing.");
            return;
        }

        equipmentProfile.SwapEquippedItem(oldEquippedItem, newInventoryItem);
        NotifyInventoryChanged();
    }

    public void UnequipItemByType(ItemType slotType)
    {
        if (equipmentProfile == null)
        {
            Debug.LogWarning("[Inventory_Player] CharacterEquipmentProfile missing.");
            return;
        }

        equipmentProfile.UnequipItemByType(slotType);
        NotifyInventoryChanged();
    }

    // ---- Counting helpers ----
    public int CountEverywhere(ItemDataSO targetData)
    {
        int total = 0;

        total += CountItem(targetData);

        if (equipmentInventory != null)
            foreach (var it in equipmentInventory.itemList)
                if (it.itemData == targetData) total += it.stackSize;

        if (equipList != null)
            foreach (var eq in equipList)
                if (eq != null && eq.HasItem() && eq.equipedItem.itemData == targetData)
                    total += 1;

        if (storage != null)
            foreach (var it in storage.itemList)
                if (it.itemData == targetData) total += it.stackSize;

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
