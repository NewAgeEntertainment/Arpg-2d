using System;
using System.Text;
using UnityEngine;

[Serializable]
public class Inventory_Item 
{
    private string itemId;

    public ItemDataSO itemData;
    public int stackSize = 1; // The current stack size of the item in the inventory

    public ItemModifier[] modifiers { get; private set; } // Array of modifiers that can be applied to the item
    public ItemEffect_DataSO itemEffect;

    public int buyPrice { get; private set; }
    public float sellPrice { get; private set; }

    public Inventory_Item(ItemDataSO itemData)
    {
        this.itemData = itemData; // the item data for this inventory item

        itemEffect = itemData.itemEffect;

        buyPrice = itemData.itemPtice; // Price of the item in the merchant
        sellPrice = itemData.itemPtice * .35f; // Sell price is 35% of the buy price

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

    public string GetItemInfo()
    {
        StringBuilder sb = new StringBuilder();
        
        
        if (itemData.itemType == ItemType.Material)
        {
            sb.AppendLine("");
            sb.AppendLine("used for crafting");
            sb.AppendLine("");
            sb.AppendLine("");
            return sb.ToString();
        }

        if (itemData.itemType == ItemType.Consumable && itemEffect != null)
        { 
            sb.AppendLine("");
            sb.AppendLine(itemEffect.effectDescription);
            sb.AppendLine("");
            sb.AppendLine("");
            return sb.ToString();
            /*return itemEffect.effectDescription*/;
        }


        foreach (var mod in modifiers)
        {
            string modType = mod.statType.ToString();
            string modValue = mod.value > 0 ? $"+{mod.value}" : mod.value.ToString();
            sb.AppendLine($"{modValue} {modType}");
        }

        if (itemEffect != null)
        {
            sb.AppendLine();
            sb.AppendLine("Unique Effect:");
            sb.AppendLine(itemEffect.effectDescription);
        }

        sb.AppendLine("");
        sb.AppendLine("");

        return sb.ToString();
    }



    private string GetStatNameByType(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return "Max Health";
            case StatType.HealthRegen: return "Health Regen";
            // ➜ keep your other cases here
            default: return "Unknown Stat";
        }
    }

    private bool IsPercentageStat(StatType type)
    {
        switch (type)
        {
            case StatType.CritChance:
            case StatType.CritPower:
            case StatType.ArmorReduction:
            case StatType.FireResistance:
            case StatType.IceResistance:
            case StatType.PoisonResistance:
            case StatType.LightningResistance:
            case StatType.Evasion:
                return true;
            default:
                return false;
        }
    }
}
