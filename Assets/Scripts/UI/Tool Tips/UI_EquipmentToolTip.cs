using System.Text;
using TMPro;
using UnityEngine;

public class UI_EquipmentToolTip : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject tooltipPanel; // ✅ Just the visual box

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI statInfo;

    private Player_Stats playerStats;

    private void Awake()
    {
        playerStats = FindObjectOfType<Player_Stats>();
    }

    public void ShowEquipmentToolTip(bool show, Inventory_Item item)
    {
        if (tooltipPanel == null)
        {
            Debug.LogWarning("[UI_EquipmentToolTip] Tooltip panel not assigned!");
            return;
        }

        if (show && item != null && item.itemData != null)
        {
            if (item.itemData.itemType != ItemType.Weapon &&
                item.itemData.itemType != ItemType.Armor &&
                item.itemData.itemType != ItemType.trinket)
            {
                Debug.Log("[UI_EquipmentToolTip] Not equipment, hiding.");
                tooltipPanel.SetActive(false);
                return;
            }

            itemName.text = item.itemData.itemName;
            itemType.text = item.itemData.itemType.ToString();
            statInfo.text = GetEquipmentInfo(item);

            tooltipPanel.SetActive(true);
        }
        else
        {
            tooltipPanel.SetActive(false);
        }
    }

    private string GetEquipmentInfo(Inventory_Item item)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<b>Major Stats:</b>");
        AppendMajorStatComparison(sb, StatType.Strength, item);
        AppendMajorStatComparison(sb, StatType.Defense, item);
        AppendMajorStatComparison(sb, StatType.Intelligence, item);
        AppendMajorStatComparison(sb, StatType.Luck, item);

        if (item.modifiers != null && item.modifiers.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Additional Stats:</b>");
            foreach (var mod in item.modifiers)
            {
                if (IsMajorStat(mod.statType)) continue;

                float currentPlayerStat = playerStats != null ? playerStats.GetStatByType(mod.statType)?.GetValue() ?? 0 : 0;
                float projectedStat = currentPlayerStat + mod.value;

                string modType = mod.statType.ToString();
                string change = projectedStat > currentPlayerStat ? "<color=green>▲</color>" : projectedStat < currentPlayerStat ? "<color=red>▼</color>" : "-";
                sb.AppendLine($"{modType}: {currentPlayerStat} → {projectedStat} {change}");
            }
        }

        if (item.itemEffect != null)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Unique Effect:</b>");
            sb.AppendLine($"<color=#00FFFF>{item.itemEffect.effectDescription}</color>");
        }

        if (sb.Length == 0)
            sb.AppendLine("<i>No additional stats.</i>");

        return sb.ToString();
    }

    private void AppendMajorStatComparison(StringBuilder sb, StatType statType, Inventory_Item item)
    {
        float currentStat = playerStats != null ? playerStats.GetStatByType(statType)?.GetValue() ?? 0 : 0;
        float modifierValue = 0;

        if (item.modifiers != null)
        {
            foreach (var mod in item.modifiers)
            {
                if (mod.statType == statType)
                {
                    modifierValue += mod.value;
                }
            }
        }

        float projectedStat = currentStat + modifierValue;
        string change = projectedStat > currentStat ? "<color=green>▲</color>" : projectedStat < currentStat ? "<color=red>▼</color>" : "-";
        sb.AppendLine($"{statType}: {currentStat} → {projectedStat} {change}");
    }

    private bool IsMajorStat(StatType statType)
    {
        return statType == StatType.Strength || statType == StatType.Defense || statType == StatType.Intelligence || statType == StatType.Luck;
    }
}
