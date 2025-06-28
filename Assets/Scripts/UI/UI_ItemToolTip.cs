using System.Text;
using TMPro;
using UnityEngine;

public class UI_ItemToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemInfo;

    private Player_Stats playerStats;

    private void Awake()
    {
        playerStats = FindAnyObjectByType<Player_Stats>();
    }

    public void ShowToolTip(bool show, Inventory_Item itemToShow)
    {
        gameObject.SetActive(show);

        if (!show || itemToShow == null)
        {
            itemName.text = "";
            itemType.text = "";
            itemInfo.text = "";
            return;
        }

        itemName.text = itemToShow.itemData.itemName;
        itemType.text = itemToShow.itemData.itemType.ToString();
        itemInfo.text = GetItemInfo(itemToShow);
    }

    private string GetItemInfo(Inventory_Item item)
    {
        if (item.itemData.itemType == ItemType.Material)
            return "Used for Crafting.";

        if (item.itemData.itemType == ItemType.Consumable)
            return item.itemData.itemEffect.effectDescription;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine();

        foreach (var mod in item.modifiers)
        {
            string modType = mod.statType.GetStatName();
            float current = playerStats.GetStatValue(mod.statType);

            bool isPercentage = IsPercentageStat(mod.statType);
            float value = mod.value;

            string displayValue = isPercentage ? value + "%" : value.ToString();
            string prefix = "+";
            string colorTag = "#FFFFFF";

            if (value > current)
            {
                prefix = "+";
                colorTag = "#00FF00";
            }
            else if (value < current)
            {
                prefix = "-";
                colorTag = "#FF0000";
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
