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

        EnsureSlot(ItemType.Weapon);
        EnsureSlot(ItemType.Armor);
        EnsureSlot(ItemType.trinket);
    }

    private void EnsureSlot(ItemType type)
    {
        foreach (var slot in equipList)
        {
            if (slot != null && slot.slotType == type)
                return;
        }

        equipList.Add(new Inventory_Equipped
        {
            slotType = type,
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

    public Inventory_Item GetEquippedItemByType(ItemType type)
    {
        foreach (var eq in equipList)
        {
            if (eq != null && eq.slotType == type && eq.HasItem())
                return eq.equipedItem;
        }

        return null;
    }

    public void TryEquipFromEquipmentInventory(Inventory_Item item)
    {
        if (item == null || item.itemData == null || equipmentInventory == null || stats == null)
            return;

        Inventory_Item inventoryItem = equipmentInventory.FindItem(item.itemData);
        if (inventoryItem == null)
        {
            Debug.LogWarning($"[CharacterEquipmentProfile] Item not found in shared equipment inventory: {item.itemData.itemName}");
            return;
        }

        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);
        if (matchingSlots == null || matchingSlots.Count == 0)
        {
            Debug.LogWarning($"[CharacterEquipmentProfile] No slot found for type {item.itemData.itemType}");
            return;
        }

        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                EquipItem(inventoryItem, slot);
                equipmentInventory.RemoveOneItem(inventoryItem);
                NotifyChanged();
                return;
            }
        }

        var slotToReplace = matchingSlots[0];
        var oldItem = slotToReplace.equipedItem;

        UnequipItem(oldItem, true);
        EquipItem(inventoryItem, slotToReplace);
        equipmentInventory.RemoveOneItem(inventoryItem);
        NotifyChanged();
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

        var slot = equipList.Find(s => s.equipedItem == itemToUnequip);
        if (slot != null)
            slot.equipedItem = null;

        itemToUnequip.RemoveModifiers(stats);
        itemToUnequip.RemoveItemEffect();

        equipmentInventory.AddItem(itemToUnequip);
        NotifyChanged();
    }

    public void UnequipItemByType(ItemType slotType)
    {
        var slot = equipList.Find(s => s.slotType == slotType && s.HasItem());
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

        // Remove old item bonuses/effects and put it back into the shared bag
        UnequipItem(oldEquippedItem, true);

        // Equip the new item into the same slot
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