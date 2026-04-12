using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipmentProfile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Entity_Stats stats;
    [SerializeField] private Inventory_Equipment equipmentInventory;

    [Header("Optional Player Bridge")]
    [SerializeField] private Inventory_Player playerInventoryBridge;

    [Header("Equipped Slots")]
    [SerializeField] private List<Inventory_Equipped> equipList = new List<Inventory_Equipped>();

    public event Action OnEquipmentChanged;

    public Entity_Stats Stats => stats;
    public Inventory_Equipment EquipmentInventory => equipmentInventory;
    public List<Inventory_Equipped> EquipList => equipList;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<Entity_Stats>();

        EnsureDefaultSlots();
    }

    private void EnsureDefaultSlots()
    {
        if (equipList == null)
            equipList = new List<Inventory_Equipped>();

        EnsureSlot(EquipmentSlotType.Weapon, ItemType.Weapon);
        EnsureSlot(EquipmentSlotType.Armor, ItemType.Armor);
        EnsureSlot(EquipmentSlotType.Trinket1, ItemType.trinket);
        EnsureSlot(EquipmentSlotType.Trinket2, ItemType.trinket);
    }

    private void EnsureSlot(EquipmentSlotType slotType, ItemType acceptedType)
    {
        foreach (var slot in equipList)
        {
            if (slot != null && slot.slotType == slotType)
            {
                slot.acceptedItemType = acceptedType;
                return;
            }
        }

        equipList.Add(new Inventory_Equipped
        {
            slotType = slotType,
            acceptedItemType = acceptedType,
            equipedItem = null
        });
    }

    public void InitializeBridge(Inventory_Player playerInventory, Entity_Stats ownerStats)
    {
        playerInventoryBridge = playerInventory;

        if (playerInventoryBridge != null)
            equipmentInventory = playerInventoryBridge.equipmentInventory;

        if (ownerStats != null)
            stats = ownerStats;

        EnsureDefaultSlots();
    }

    public void SetSharedEquipmentInventory(Inventory_Equipment sharedInventory, Entity_Stats ownerStats = null)
    {
        equipmentInventory = sharedInventory;

        if (ownerStats != null)
            stats = ownerStats;

        EnsureDefaultSlots();
    }

    public Inventory_Item GetEquippedItemBySlot(EquipmentSlotType slotType)
    {
        foreach (var eq in equipList)
        {
            if (eq != null && eq.slotType == slotType && eq.HasItem())
                return eq.equipedItem;
        }

        return null;
    }

    public Inventory_Item GetEquippedItemByType(ItemType type)
    {
        foreach (var eq in equipList)
        {
            if (eq != null && eq.acceptedItemType == type && eq.HasItem())
                return eq.equipedItem;
        }

        return null;
    }

    public void TryEquipFromEquipmentInventory(Inventory_Item item)
    {
        if (item == null || item.itemData == null)
            return;

        // Fallback behavior:
        // find first empty matching slot, otherwise first matching slot
        var matchingSlots = equipList.FindAll(slot => slot != null && slot.acceptedItemType == item.itemData.itemType);
        if (matchingSlots == null || matchingSlots.Count == 0)
        {
            Debug.LogWarning($"[CharacterEquipmentProfile] No slot found for type {item.itemData.itemType}");
            return;
        }

        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                TryEquipFromEquipmentInventory(item, slot.slotType);
                return;
            }
        }

        TryEquipFromEquipmentInventory(item, matchingSlots[0].slotType);
    }

    public void TryEquipFromEquipmentInventory(Inventory_Item item, EquipmentSlotType targetSlotType)
    {
        if (item == null || item.itemData == null || equipmentInventory == null || stats == null)
            return;

        var targetSlot = equipList.Find(slot => slot != null && slot.slotType == targetSlotType);
        if (targetSlot == null)
        {
            Debug.LogWarning($"[CharacterEquipmentProfile] No target slot found for {targetSlotType}");
            return;
        }

        if (targetSlot.acceptedItemType != item.itemData.itemType)
        {
            Debug.LogWarning($"[CharacterEquipmentProfile] Item {item.itemData.itemName} cannot go into slot {targetSlotType}");
            return;
        }

        Inventory_Item inventoryItem = equipmentInventory.FindItem(item.itemData);
        if (inventoryItem == null)
        {
            Debug.LogWarning($"[CharacterEquipmentProfile] Item not found in shared equipment inventory: {item.itemData.itemName}");
            return;
        }

        if (targetSlot.HasItem())
        {
            var oldItem = targetSlot.equipedItem;
            UnequipItem(oldItem, true);
        }

        EquipItem(inventoryItem, targetSlot);
        equipmentInventory.RemoveOneItem(inventoryItem);
        NotifyChanged();

        if (AudioManager.instance != null)
            AudioManager.instance.PlayGlobalSFX("Equipped");
    }

    private void EquipItem(Inventory_Item itemToEquip, Inventory_Equipped slot)
    {
        if (itemToEquip == null || slot == null || stats == null)
            return;

        slot.equipedItem = itemToEquip;
        slot.equipedItem.AddModifiers(stats);

        var player = GetComponent<Player>();
        if (player != null)
            slot.equipedItem.AddItemEffect(player);
    }

    public void UnequipItem(Inventory_Item itemToUnequip, bool replacing = false)
    {
        if (itemToUnequip == null || equipmentInventory == null || stats == null)
            return;

        var slot = equipList.Find(s => s != null && s.equipedItem == itemToUnequip);
        if (slot != null)
            slot.equipedItem = null;

        itemToUnequip.RemoveModifiers(stats);
        itemToUnequip.RemoveItemEffect();

        equipmentInventory.AddItem(itemToUnequip);
        NotifyChanged();

        if (AudioManager.instance != null)
            AudioManager.instance.PlayGlobalSFX("Unequipped");
    }

    public void UnequipItemBySlot(EquipmentSlotType slotType)
    {
        var slot = equipList.Find(s => s != null && s.slotType == slotType && s.HasItem());
        if (slot != null)
            UnequipItem(slot.equipedItem);
    }

    public void UnequipItemByType(ItemType itemType)
    {
        var slot = equipList.Find(s => s != null && s.acceptedItemType == itemType && s.HasItem());
        if (slot != null)
            UnequipItem(slot.equipedItem);
    }

    public void SwapEquippedItem(Inventory_Item oldEquippedItem, Inventory_Item newInventoryItem)
    {
        if (oldEquippedItem == null || newInventoryItem == null)
        {
            Debug.LogWarning("[CharacterEquipmentProfile] SwapEquippedItem got null item.");
            return;
        }

        var equippedSlot = equipList.Find(slot => slot != null && slot.equipedItem == oldEquippedItem);
        if (equippedSlot == null)
        {
            Debug.LogWarning("[CharacterEquipmentProfile] No equipped slot found for old item.");
            return;
        }

        if (equippedSlot.acceptedItemType != newInventoryItem.itemData.itemType)
        {
            Debug.LogWarning("[CharacterEquipmentProfile] New item does not match slot type.");
            return;
        }

        UnequipItem(oldEquippedItem, true);
        EquipItem(newInventoryItem, equippedSlot);

        if (equipmentInventory != null)
            equipmentInventory.RemoveOneItem(newInventoryItem);

        NotifyChanged();
    }

    public void NotifyChanged()
    {
        equipmentInventory?.NotifyInventoryChanged();
        playerInventoryBridge?.NotifyInventoryChanged();
        OnEquipmentChanged?.Invoke();
    }
}