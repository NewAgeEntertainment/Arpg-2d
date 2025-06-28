using System;
using UnityEngine;

[Serializable]
public class Inventory_Item 
{
    private string itemId;

    public ItemDataSO itemData;
    public int stackSize = 1; // The current stack size of the item in the inventory

    public ItemModifier[] modifiers { get; private set; } // Array of modifiers that can be applied to the item
    public ItemEffect_DataSO itemEffect;

    public Inventory_Item(ItemDataSO itemData)
    {
        this.itemData = itemData; // the item data for this inventory item

        itemEffect = itemData.itemEffect;
        modifiers = EquipmentData()?.modifiers; // if the item is an equptment, get its modifiers

        itemId = itemData.itemName + " - " + Guid.NewGuid();
    }

    public void AddModifiers(Entity_Stats playerStats)
    {
        foreach (var mod in modifiers) 
        {
            Stat statToModify = playerStats.GetStatByType(mod.statType);
            statToModify.AddModifier(mod.value, itemId);
        }
    }

    public void RemoveModifiers(Entity_Stats playerStats)
    {
        foreach (var mod in modifiers)
        {
            Stat statToModify = playerStats.GetStatByType(mod.statType);
            statToModify.RemoveModifier(itemId);
        }
    }

    public void AddItemEffect(Player player) => itemEffect?.Subscribe(player);
    public void RemoveItemEffect() => itemEffect?.Unsubscribe();

    private EquipmentDataSO EquipmentData()
    {
        if (itemData is EquipmentDataSO equipment)
        {
            return equipment;
        }
        return null; // Ensure all code paths return a value  
    }

    public float GetStatValue(StatType type)
    {
        if (itemData == null || itemData.itemModifiers == null)
            return 0f;

        foreach (var mod in itemData.itemModifiers)
        {
            if (mod.statType == type)
                return mod.value;
        }
        return 0f;
    }

    public bool CanAddStack() => stackSize < itemData.maxStackSize;

    public void AddStack() => stackSize++;
    public void RemoveStack() => stackSize--;
}
