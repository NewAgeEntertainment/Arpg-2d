// Inventory_Item.cs
using System;
using System.Text;
using UnityEngine;

[Serializable]
public class Inventory_Item
{
    private string itemId;

    public ItemDataSO itemData;
    public int stackSize = 1;
    public ItemModifier[] modifiers { get; private set; }
    public ItemEffect_DataSO itemEffect;

    public int buyPrice { get; private set; }
    public float sellPrice { get; private set; }

    public Inventory_Item(ItemDataSO itemData)
    {
        this.itemData = itemData;
        itemEffect = itemData.itemEffect;
        buyPrice = itemData.itemPtice;
        sellPrice = itemData.itemPtice * 0.35f;
        modifiers = EquipmentData()?.modifiers;
        itemId = itemData.itemName + " - " + Guid.NewGuid();
    }

    public void AddModifiers(Entity_Stats playerStats)
    {
        foreach (var mod in modifiers)
        {
            Stat statToModify = playerStats.GetStatByType(mod.statType);
            statToModify.AddModifier(mod.value, StatModType.Flat, itemId);
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
        return itemData as EquipmentDataSO;
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
    public void AddStack(int amount = 1)
    {
        stackSize = Mathf.Min(stackSize + amount, itemData.maxStackSize);
    }

    public void RemoveStack(int amount = 1)
    {
        stackSize = Mathf.Max(stackSize - amount, 0);
    }

    public bool CanStack() => CanAddStack();

    public string GetItemInfo()
    {
        StringBuilder sb = new StringBuilder();

        if (itemData.itemType == ItemType.Material)
        {
            sb.AppendLine("<color=#AAAAAA><i>Used for crafting.</i></color>");
            return sb.ToString();
        }

        if (itemData.itemType == ItemType.Consumable && itemEffect != null)
        {
            sb.AppendLine($"<color=#00FF00>{itemEffect.effectDescription}</color>");
            return sb.ToString();
        }

        if (modifiers != null && modifiers.Length > 0)
        {
            sb.AppendLine("<b>Stats:</b>");
            foreach (var mod in modifiers)
            {
                string modType = mod.statType.ToString();
                string modValue = mod.value > 0 ? $"+{mod.value}" : mod.value.ToString();
                sb.AppendLine($"<color=#FFD700>{modValue}</color> <color=#FFFFFF>{modType}</color>");
            }
        }

        if (itemEffect != null)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Unique Effect:</b>");
            sb.AppendLine($"<color=#00FFFF>{itemEffect.effectDescription}</color>");
        }

        if (sb.Length == 0)
        {
            sb.AppendLine("<color=#888888><i>No special properties.</i></color>");
        }

        return sb.ToString();
    }
}