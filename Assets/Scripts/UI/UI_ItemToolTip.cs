using System.Text;
using TMPro;
using UnityEngine;

public class UI_ItemToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemInfo;

    public void ShowToolTip(bool show, Inventory_Item itemToShow)
    {
        if (show && itemToShow != null && itemToShow.itemData != null)
        {
            itemName.text = itemToShow.itemData.itemName;
            itemType.text = itemToShow.itemData.itemType.ToString();
            itemInfo.text = GetItemInfo(itemToShow);

            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private string GetItemInfo(Inventory_Item item)
    {
        if (item.itemData.itemType == ItemType.Material)
            return "Used for crafting.";

        if (item.itemData.itemType == ItemType.Consumable && item.itemEffect != null)
            return item.itemEffect.effectDescription;

        StringBuilder sb = new StringBuilder();

        foreach (var mod in item.modifiers)
        {
            string modType = mod.statType.ToString();
            string modValue = mod.value > 0 ? $"+{mod.value}" : mod.value.ToString();
            sb.AppendLine($"{modValue} {modType}");
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
