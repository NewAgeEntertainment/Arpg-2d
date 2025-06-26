using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    private Player player;
    public List<Inventory_Equipped> equipList;
    public Inventory_Storage storage { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();
        storage = FindFirstObjectByType<Inventory_Storage>();

    }

    public void TryEquipItem(Inventory_Item item)
    {
        var inventoryItem = FindItem(item.itemData);
        var matchingSlots = equipList.FindAll(slot => slot.slotType == item.itemData.itemType);

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

        UnequipItem(itemToUnequip,slotToReplace != null);
        EquipItem(inventoryItem, slotToReplace);
    }



    private void EquipItem(Inventory_Item itemToEquip, Inventory_Equipped slot)
    {
        float savedHealthPercent = player.health.GetHealthPercent();
        float ManaPercent = player.mana.GetManaPercent();

        slot.equipedItem = itemToEquip;
        slot.equipedItem.AddModifiers(player.stats);
        slot.equipedItem.AddItemEffect(player); // Apply item effect to the player

        player.health.SetHealthToPercent(savedHealthPercent); // Restore health to the previous percentage after equipping the item
        player.mana.SetManaToPercent(ManaPercent); // Restore mana to the previous percentage after equipping the item
        RemoveOneItem(itemToEquip);


    }

    public void UnequipItem(Inventory_Item itemToUnequip, bool replacingItem = false)
    {
        if (CanAddItem(itemToUnequip) == false && replacingItem == false)
        {
            Debug.Log("NoSpace");
            return;
        }

        float savedHealthPercent = player.health.GetHealthPercent();
        float ManaPercent = player.mana.GetManaPercent();

        var slotToUnequip = equipList.Find(slot => slot.equipedItem == itemToUnequip);

        if(slotToUnequip != null)
            slotToUnequip.equipedItem = null; // Clear the slot if the item is found

        itemToUnequip.RemoveModifiers(player.stats);
        itemToUnequip.RemoveItemEffect(); // Remove item effect from the player

        player.health.SetHealthToPercent(savedHealthPercent); // Restore health to the previous percentage after unequipping the item
        player.mana.SetManaToPercent(ManaPercent); // Restore mana to the previous percentage after unequipping the item

        AddItem(itemToUnequip);
    }
}
