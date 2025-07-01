using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public Inventory_Equipment equipmentInventory;  // ✅ your equipment bag stays separate
    public List<Inventory_Equipped> equipList;     // slots where items are equipped
    private Player player;
    public Inventory_Storage storage { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();

        if (equipmentInventory == null)
            equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();

        Debug.Log("[Inventory_Player] equipmentInventory: " + (equipmentInventory != null));


        storage = FindFirstObjectByType<Inventory_Storage>();
        Debug.Log($"storage: {storage}");
    }

    public override void AddItem(Inventory_Item itemToAdd)
    {
        Debug.Log($"[Inventory_Player] Adding {itemToAdd.itemData.itemName} to player inventory.");
        base.AddItem(itemToAdd);
    }

    public void TryEquipItem(Inventory_Item item)
    {
        var inventoryItem = FindItem(item.itemData);
        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);

        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                EquipItem(inventoryItem, slot);
                return;
            }
        }

        var slotToReplace = matchingSlots[0];
        var itemToUnequip = slotToReplace.equipedItem;

        UnequipItem(itemToUnequip, true);
        EquipItem(inventoryItem, slotToReplace);
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
        float savedHealth = player.health.GetHealthPercent();
        float savedMana = player.mana.GetManaPercent();

        slot.equipedItem = itemToEquip;
        slot.equipedItem.AddModifiers(player.stats);
        slot.equipedItem.AddItemEffect(player);

        player.health.SetHealthToPercent(savedHealth);
        player.mana.SetManaToPercent(savedMana);

        NotifyInventoryChanged();
    }

    public void UnequipItem(Inventory_Item itemToUnequip, bool replacing = false)
    {
        float savedHealth = player.health.GetHealthPercent();
        float savedMana = player.mana.GetManaPercent();

        var slot = equipList.Find(slot => slot.equipedItem == itemToUnequip);
        if (slot != null)
            slot.equipedItem = null;

        itemToUnequip.RemoveModifiers(player.stats);
        itemToUnequip.RemoveItemEffect();

        player.health.SetHealthToPercent(savedHealth);
        player.mana.SetManaToPercent(savedMana);

        // Equipment goes back to equipment inventory!
        equipmentInventory.AddItem(itemToUnequip);

        NotifyInventoryChanged();
    }
}
