using System.Text;
using TMPro;
using UnityEngine;

public class UI_EquipmentToolTip : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject tooltipPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI statInfo;

    private Player_Stats playerStats;
    private Inventory_Player playerInventory;

    private const float EPSILON = 0.0001f;

    // Only these will be compared/shown
    private static readonly StatType[] MajorStats =
    {
        StatType.Strength,
        StatType.Defense,
        StatType.Intelligence,
        StatType.Luck,
        StatType.Stroke,
        StatType.Resilience
    };

    private void Awake()
    {
        playerStats = FindObjectOfType<Player_Stats>();
        playerInventory = FindObjectOfType<Inventory_Player>();

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);   // Always visible
            ShowBaseStats();
        }
        else
        {
            Debug.LogWarning("[UI_EquipmentToolTip] Tooltip panel not assigned!");
        }
    }

    /// <summary>
    /// When show == true and item is valid equipment -> show comparison.
    /// Otherwise, show base player stats.
    /// </summary>
    public void ShowEquipmentToolTip(bool show, Inventory_Item item)
    {
        if (tooltipPanel == null) return;

        if (show && item != null && item.itemData != null &&
            (item.itemData.itemType == ItemType.Weapon ||
             item.itemData.itemType == ItemType.Armor ||
             item.itemData.itemType == ItemType.trinket))
        {
            if (itemName) itemName.text = item.itemData.itemName;
            if (itemType) itemType.text = item.itemData.itemType.ToString();
            if (statInfo) statInfo.text = GetEquipmentInfo(item);
        }
        else
        {
            ShowBaseStats();
        }
    }

    /// <summary>
    /// Show current player values for the major stats, with blank title/type.
    /// </summary>
    public void ShowBaseStats()
    {
        if (itemName) itemName.text = "";
        if (itemType) itemType.text = "";

        if (statInfo)
            statInfo.text = GetBaseStatDisplay();
    }

    private string GetBaseStatDisplay()
    {
        var sb = new StringBuilder();
        if (playerStats == null)
        {
            sb.AppendLine("<i>No player stats found.</i>");
            return sb.ToString();
        }

        foreach (var s in MajorStats)
        {
            float value = playerStats.GetStatByType(s)?.GetValue() ?? 0f;
            sb.AppendLine($"{s}: {value:0.##}");
        }

        return sb.ToString();
    }

    private string GetEquipmentInfo(Inventory_Item newItem)
    {
        var sb = new StringBuilder();

        foreach (var s in MajorStats)
            AppendMajorStatComparison(sb, s, newItem);

        if (newItem.itemEffect != null)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Unique Effect:</b>");
            sb.AppendLine($"<color=#00FFFF>{newItem.itemEffect.effectDescription}</color>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Compares: (player stat WITHOUT currently equipped item of this slot) vs (with new item).
    /// This guarantees we see red ▼ when the new gear is weaker.
    /// </summary>
    private void AppendMajorStatComparison(StringBuilder sb, StatType statType, Inventory_Item newItem)
    {
        float currentWithAllGear = playerStats != null ? playerStats.GetStatByType(statType)?.GetValue() ?? 0f : 0f;

        // Currently equipped item of this same slot type
        Inventory_Item equippedItem = FindEquippedItemOfSameType(newItem.itemData.itemType);

        // Remove the currently equipped item's contribution for this stat
        float equippedModifier = equippedItem != null ? GetTotalModifierFor(equippedItem, statType) : 0f;
        float baselineWithoutThisSlot = currentWithAllGear - equippedModifier;

        // New item contribution
        float newItemModifier = GetTotalModifierFor(newItem, statType);
        float projectedWithNew = baselineWithoutThisSlot + newItemModifier;

        // Delta vs current (i.e., new - equipped)
        float delta = newItemModifier - equippedModifier;

        string arrow = "-";
        string deltaTxt = "";

        if (delta > EPSILON)
        {
            arrow = "<color=green>▲</color>";
            deltaTxt = $" <color=green>(+{delta:0.##})</color>";
        }
        else if (delta < -EPSILON)
        {
            arrow = "<color=red>▼</color>";
            deltaTxt = $" <color=red>({delta:0.##})</color>";
        }

        sb.AppendLine($"{statType}: {currentWithAllGear:0.##} → {projectedWithNew:0.##} {arrow}{deltaTxt}");
    }

    /// <summary>
    /// Sums modifiers that affect the given stat, from BOTH:
    /// - Inventory_Item.modifiers (EquipmentDataSO)
    /// - ItemDataSO.itemModifiers (List<ItemStatModifier>)
    /// </summary>
    private float GetTotalModifierFor(Inventory_Item item, StatType statType)
    {
        float total = 0f;

        // EquipmentDataSO path (array)
        if (item?.modifiers != null)
        {
            foreach (var mod in item.modifiers)
            {
                if (mod.statType == statType)
                    total += mod.value;
            }
        }

        // ItemDataSO path (list)
        if (item?.itemData?.itemModifiers != null)
        {
            foreach (var mod in item.itemData.itemModifiers)
            {
                if (mod.statType == statType)
                    total += mod.value;
            }
        }

        return total;
    }

    private Inventory_Item FindEquippedItemOfSameType(ItemType type)
    {
        if (playerInventory == null) return null;
        return playerInventory.GetEquippedItemByType(type);
    }

}
