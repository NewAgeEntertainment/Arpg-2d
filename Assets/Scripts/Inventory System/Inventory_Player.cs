using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public int gold = 10000;

    public Inventory_Equipment equipmentInventory;  // ✅ your equipment bag stays separate
    public List<Inventory_Equipped> equipList = new List<Inventory_Equipped>();
    private Player player;
    public Inventory_Storage storage { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();

        if (equipmentInventory == null)
            equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();

        if (equipList.Count == 0)
        {
            Debug.LogWarning("[Inventory_Player] equipList is EMPTY! Add equipped slots in the Inspector.");

            Debug.Log("[Inventory_Player] equipmentInventory: " + (equipmentInventory != null));


            if (storage == null)
                storage = FindFirstObjectByType<Inventory_Storage>();

            if (storage == null)
                Debug.LogError("[Inventory_Player] No Inventory_Storage found in scene!");
        } 
    }

    public override void AddItem(Inventory_Item itemToAdd)
    {
        Debug.Log($"[Inventory_Player] Adding {itemToAdd.itemData.itemName} to player inventory.");

        if (itemToAdd.itemData.itemType == ItemType.Material && storage != null)
        {
            storage.AddMaterialToStash(itemToAdd);
            return; // ✅ we’re done — don’t also add to normal itemList!
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

        // default fallback → normal backpack
        base.AddItem(itemToAdd);
    }


    public bool IsEquipped(Inventory_Item item)
    {
        return equipList.Exists(slot => slot.equipedItem == item);
    }


    public void TryEquipItem(Inventory_Item item)
    {
        var inventoryItem = FindItem(item.itemData);
        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);

        if (matchingSlots == null || matchingSlots.Count == 0)
        {
            Debug.LogError($"[Inventory_Player] No equip slots found for type: {item.itemData.itemType}!");
            return;
        }

        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                EquipItem(inventoryItem, slot);
                return;
            }
        }

        var slotToReplace = matchingSlots[0];
        if (slotToReplace == null)
        {
            Debug.LogError("[Inventory_Player] slotToReplace is null!");
            return;
        }

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

        // Remove from equipment bag if present
        if (equipmentInventory.FindItem(itemToEquip.itemData) != null)
            equipmentInventory.RemoveOneItem(itemToEquip);

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
