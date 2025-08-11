// Inventory_Item.cs
using System;
using System.Linq;            // <-- needed for ToArray()
using System.Text;
using UnityEngine;

[Serializable]
public class Inventory_Item
{
    private string itemId;

    public ItemDataSO itemData;
    public int stackSize = 1;

    // Per-instance modifiers that get saved/loaded (can differ per copy).
    [SerializeField] private ItemModifier[] instanceModifiers;

    public ItemEffect_DataSO itemEffect;
    // put this inside the Inventory_Item class (public section)
    public ItemModifier[] modifiers => instanceModifiers ?? System.Array.Empty<ItemModifier>();


    public int buyPrice { get; private set; }
    public float sellPrice { get; private set; }

    public Inventory_Item(ItemDataSO itemData)
    {
        this.itemData = itemData;
        itemEffect = itemData.itemEffect;
        buyPrice = itemData.itemPtice;
        sellPrice = itemData.itemPtice * 0.35f;

        // Initialize instance modifiers from the SO defaults:
        // 1) If it's EquipmentDataSO, copy its array.
        if (itemData is EquipmentDataSO eq && eq.modifiers != null)
        {
            // Clone so each Inventory_Item has its own independent array
            instanceModifiers = (ItemModifier[])eq.modifiers.Clone();
        }
        // 2) Otherwise, use the generic list on ItemDataSO (convert to array)
        else if (itemData.itemModifiers != null && itemData.itemModifiers.Count > 0)
        {
            instanceModifiers = itemData.itemModifiers.ToArray();
        }
        else
        {
            instanceModifiers = Array.Empty<ItemModifier>();
        }

        itemId = itemData.itemName + " - " + Guid.NewGuid();
    }

    // Accessors used by save/load.
    public ItemModifier[] GetInstanceModifiers() => instanceModifiers;
    public void SetInstanceModifiers(ItemModifier[] mods)
    {
        instanceModifiers = (mods != null && mods.Length > 0) ? (ItemModifier[])mods.Clone() : Array.Empty<ItemModifier>();
    }

    public void AddModifiers(Entity_Stats playerStats)
    {
        if (instanceModifiers == null) return;
        foreach (var mod in instanceModifiers)
        {
            var stat = playerStats.GetStatByType(mod.statType);
            stat.AddModifier(mod.value, StatModType.Flat, itemId);
        }
    }

    public void RemoveModifiers(Entity_Stats playerStats)
    {
        if (instanceModifiers == null) return;
        foreach (var mod in instanceModifiers)
        {
            var stat = playerStats.GetStatByType(mod.statType);
            stat.RemoveModifier(itemId);
        }
    }

    public void AddItemEffect(Player player) => itemEffect?.Subscribe(player);
    public void RemoveItemEffect() => itemEffect?.Unsubscribe();

    private EquipmentDataSO EquipmentData() => itemData as EquipmentDataSO;

    public float GetStatValue(StatType type)
    {
        // For UI/tooltips: read from SO-level defaults
        if (itemData == null || itemData.itemModifiers == null) return 0f;
        foreach (var mod in itemData.itemModifiers)
            if (mod.statType == type) return mod.value;
        return 0f;
    }

    public bool CanAddStack() => stackSize < itemData.maxStackSize;
    public void AddStack(int amount = 1) => stackSize = Mathf.Min(stackSize + amount, itemData.maxStackSize);
    public void RemoveStack(int amount = 1) => stackSize = Mathf.Max(stackSize - amount, 0);
    public bool CanStack() => CanAddStack();

    public string GetItemInfo()
    {
        var sb = new StringBuilder();

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

        if (instanceModifiers != null && instanceModifiers.Length > 0)
        {
            sb.AppendLine("<b>Stats:</b>");
            foreach (var mod in instanceModifiers)
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
            sb.AppendLine("<color=#888888><i>No special properties.</i></color>");

        return sb.ToString();
    }
}
