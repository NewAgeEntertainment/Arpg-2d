using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public event Action OnEquipmentChanged;
    public Inventory_Equipment equipmentInventory { get; private set; }

    private Player player;
    public List<Inventory_Equipped> equipList;
    public Inventory_Storage storage { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();
        storage = FindFirstObjectByType<Inventory_Storage>();
        equipmentInventory = GetComponent<Inventory_Equipment>();
        {
            Debug.LogWarning("Inventory_Player: equipmentInventory is not assigned!");
        }

        equipList = new List<Inventory_Equipped>
        {
                new Inventory_Equipped { slotType = ItemType.Weapon },
                new Inventory_Equipped { slotType = ItemType.Armor },
                new Inventory_Equipped { slotType = ItemType.trinket }
        };

    }


    public void TryEquipItem(Inventory_Item item)
    {
        var inventoryItem = FindItem(item.itemData);
        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);

        if (matchingSlots.Exists(slot => slot.equipedItem == inventoryItem))
        {
            Debug.Log("Item already equipped.");
            return;
        }

        // step 1 : try to find empty slot and equip item
        foreach (var slot in matchingSlots)
        {
            if (slot.HasItem() == false)
            {
                EquipItem(inventoryItem, slot);
                return;
            }
        }

        var slotToReplace = matchingSlots[0];
        var itemToUnequip = slotToReplace.equipedItem;

        if (slotToReplace != null)
        {
            UnequipItem(itemToUnequip, replacingItem: true);
            EquipItem(inventoryItem, slotToReplace);
            OnEquipmentChanged?.Invoke();
        }
    }

    public void TryEquipFromEquipmentInventory(Inventory_Item item)
    {
        if (item == null || equipmentInventory == null)
            return;

        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);
        Inventory_Item itemInEquipInventory = equipmentInventory.FindItem(item.itemData);

        if (itemInEquipInventory == null)
        {
            Debug.LogWarning("Item not found in equipment inventory.");
            return;
        }

        // Remove it first — no longer remove in EquipItem
        equipmentInventory.RemoveOneItem(itemInEquipInventory);

        // 1. Try to find empty slot
        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                EquipItem(itemInEquipInventory, slot);
                return;
            }
        }

        // 2. Otherwise, replace the first matching slot
        var slotToReplace = matchingSlots[0];
        var itemToUnequip = slotToReplace.equipedItem;

        UnequipItem(itemToUnequip, replacingItem: true);
        EquipItem(itemInEquipInventory, slotToReplace);
    }


    // Inventory_Player.cs (partial - EquipItem method)
    private void EquipItem(Inventory_Item itemToEquip, Inventory_Equipped slot)
    {
        float savedHealthPercent = player.health.GetHealthPercent();
        float manaPercent = player.mana.GetManaPercent();

        slot.equipedItem = itemToEquip;
        slot.equipedItem.AddModifiers(player.stats);
        slot.equipedItem.AddItemEffect(player); // Apply item effect to player

        player.health.SetHealthToPercent(savedHealthPercent);
        player.mana.SetManaToPercent(manaPercent);

        // 🔥 Do not remove item here — ensure it's done before calling this
        // RemoveOneItem(itemToEquip);

        // ✅ Notify UI update manually if needed
        NotifyInventoryChanged();
    }


    public void UnequipItem(Inventory_Item itemToUnequip, bool replacingItem = false)
    {
        if (itemToUnequip == null) return;

        float savedHealthPercent = player.health.GetHealthPercent();
        float manaPercent = player.mana.GetManaPercent();

        var slotToUnequip = equipList.Find(slot => slot.equipedItem == itemToUnequip);
        if (slotToUnequip != null)
            slotToUnequip.equipedItem = null;

        itemToUnequip.RemoveModifiers(player.stats);
        itemToUnequip.RemoveItemEffect();

        player.health.SetHealthToPercent(savedHealthPercent);
        player.mana.SetManaToPercent(manaPercent);

        // Try to store back in equipment inventory
        if (equipmentInventory.CanAddItem(itemToUnequip))
            equipmentInventory.AddItem(itemToUnequip);
        else
            AddItem(itemToUnequip); // fallback to general inventory
    }

}
