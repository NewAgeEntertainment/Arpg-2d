using System.Text;
using TMPro;
using UnityEngine;

public class UI_ItemToolTip : UI_ToolTip
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemInfo;


    private Player_Stats playerStats;

    protected override void Awake()
    {
        base.Awake();
        playerStats = FindAnyObjectByType<Player_Stats>();
    }


    public void ShowToolTip(bool show, RectTransform targetRect, Inventory_Item itemToShow)
    {
        base.ShowToolTip(show, targetRect);

        itemName.text = itemToShow.itemData.itemName;
        itemType.text = itemToShow.itemData.itemType.ToString();
        itemInfo.text = GetItemInfo(itemToShow);
    }

    public string GetItemInfo(Inventory_Item item)
    {
        if (item.itemData.itemType == ItemType.Material)
            return "Used for Crafting.";

        if (item.itemData.itemType == ItemType.Consumable)
            return item.itemData.itemEffect.effectDescription;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine();

        foreach (var mod in item.modifiers)
        {
            string modType = GetStatNameByType(mod.statType);
            float current = playerStats.GetStatValue(mod.statType);

            bool isPercentage = IsPercentageStat(mod.statType);

            float value = mod.value;
            string displayValue = isPercentage ? value.ToString() + "%" : value.ToString();

            string prefix = "+";
            string colorTag = "#FFFFFF";

            if (value > current)
            {
                prefix = "+";
                colorTag = "#00FF00"; // Green if better
            }
            else if (value < current)
            {
                prefix = "-";
                colorTag = "#FF0000"; // Red if worse
            }

            sb.AppendLine($"{prefix} <color={colorTag}>{displayValue}</color> {modType} (Current: {current})");
        }


        if (item.itemEffect != null)
        {
            sb.AppendLine();
            sb.AppendLine("Unique Effect:");
            sb.AppendLine(item.itemEffect.effectDescription);
        }

        return sb.ToString();
    }


    private string GetStatNameByType(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return "Max Health";
            case StatType.HealthRegen: return "Health Regen";
            case StatType.MaxMana: return "Max Mana";
            case StatType.ManaRegen: return "Mana Regen";
            case StatType.Strength: return "Strength";
            case StatType.Luck: return "Luck";
            case StatType.Intelligence: return "Intelligence";
            case StatType.Vitality: return "Vitality";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.Damage: return "Damage";
            case StatType.CritChance: return "Crit Chance";
            case StatType.CritPower: return "Crit Power";
            case StatType.ArmorReduction: return "Armor Reduction";
            case StatType.FireDamage: return "Fire Damage";
            case StatType.IceDamage: return "Ice Damage";
            case StatType.PoisonDamage: return "Poison Damage";
            case StatType.LightningDamage: return "Lightning Damage";
            case StatType.Armor: return "Armor";
            case StatType.Evasion: return "Evasion";
            case StatType.IceResistance: return "Ice Resistance";
            case StatType.FireResistance: return "Fire Resistance";
            case StatType.PoisonResistance: return "Poison Resistance";
            case StatType.LightningResistance: return "Lightning Resistance";
            case StatType.SexualDamage: return "Sexual Damage";
            case StatType.Resilience: return "Resilience";
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
