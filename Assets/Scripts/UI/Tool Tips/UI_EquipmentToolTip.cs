using System.Text;
using TMPro;
using UnityEngine;

public class UI_EquipmentToolTip : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject tooltipPanel;

    [Header("Header Texts")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;

    [Header("Stat Texts")]
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI intelligenceText;
    [SerializeField] private TextMeshProUGUI luckText;
    [SerializeField] private TextMeshProUGUI strokeText;
    [SerializeField] private TextMeshProUGUI resilienceText;

    [Header("Crit Stat Texts")]
    [SerializeField] private TextMeshProUGUI critChanceText;
    [SerializeField] private TextMeshProUGUI critPowerText;

    [Header("Optional: Unique Effect Text")]
    [SerializeField] private TextMeshProUGUI uniqueEffectText;

    private Player_Stats playerStats;
    private Inventory_Player playerInventory;

    private const float EPSILON = 0.0001f;

    private void Awake()
    {
        playerStats = FindObjectOfType<Player_Stats>();
        playerInventory = FindObjectOfType<Inventory_Player>();

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
            ShowBaseStats();
        }
        else
        {
            Debug.LogWarning("[UI_EquipmentToolTip] Tooltip panel not assigned!");
        }
    }

    public void ShowEquipmentToolTip(bool show, Inventory_Item item)
    {
        if (tooltipPanel == null) return;

        bool validEquip =
            show &&
            item != null &&
            item.itemData != null &&
            (item.itemData.itemType == ItemType.Weapon ||
             item.itemData.itemType == ItemType.Armor ||
             item.itemData.itemType == ItemType.trinket);

        if (validEquip)
        {
            if (itemName) itemName.text = item.itemData.itemName;
            if (itemType) itemType.text = item.itemData.itemType.ToString();

            SetEquipmentStatComparison(item);

            if (uniqueEffectText != null)
            {
                if (item.itemEffect != null)
                {
                    uniqueEffectText.gameObject.SetActive(true);
                    uniqueEffectText.text = $"<b>Unique Effect:</b>\n<color=#00FFFF>{item.itemEffect.effectDescription}</color>";
                }
                else
                {
                    uniqueEffectText.gameObject.SetActive(false);
                    uniqueEffectText.text = "";
                }
            }
        }
        else
        {
            ShowBaseStats();
        }
    }

    public void ShowBaseStats()
    {
        if (itemName) itemName.text = "";
        if (itemType) itemType.text = "";

        SetBaseStat(strengthText, StatType.Strength);
        SetBaseStat(defenseText, StatType.Defense);
        SetBaseStat(intelligenceText, StatType.Intelligence);
        SetBaseStat(luckText, StatType.Luck);
        SetBaseStat(strokeText, StatType.Stroke);
        SetBaseStat(resilienceText, StatType.Resilience);

        SetBaseStat(critChanceText, StatType.CritChance, true);
        SetBaseStat(critPowerText, StatType.CritPower, true);

        if (uniqueEffectText != null)
        {
            uniqueEffectText.gameObject.SetActive(false);
            uniqueEffectText.text = "";
        }
    }

    #region Base Stat Helpers
    private void SetBaseStat(TextMeshProUGUI field, StatType type, bool asPercent = false)
    {
        if (field == null) return;

        float value = playerStats != null
            ? playerStats.GetStatByType(type)?.GetValue() ?? 0f
            : 0f;

        string formattedValue = asPercent ? $"{value:0.##}%" : $"{value:0.##}";
        field.text = $"{type}: {formattedValue}";
    }
    #endregion

    #region Comparison Helpers
    private void SetEquipmentStatComparison(Inventory_Item newItem)
    {
        SetStatComparison(strengthText, StatType.Strength, newItem);
        SetStatComparison(defenseText, StatType.Defense, newItem);
        SetStatComparison(intelligenceText, StatType.Intelligence, newItem);
        SetStatComparison(luckText, StatType.Luck, newItem);
        SetStatComparison(strokeText, StatType.Stroke, newItem);
        SetStatComparison(resilienceText, StatType.Resilience, newItem);

        SetStatComparison(critChanceText, StatType.CritChance, newItem, true);
        SetStatComparison(critPowerText, StatType.CritPower, newItem, true);
    }

    private void SetStatComparison(TextMeshProUGUI field, StatType statType, Inventory_Item newItem, bool asPercent = false)
    {
        if (field == null) return;

        float currentWithAllGear = playerStats != null
            ? playerStats.GetStatByType(statType)?.GetValue() ?? 0f
            : 0f;

        Inventory_Item equippedItem = FindEquippedItemOfSameType(newItem.itemData.itemType);

        float equippedModifier = equippedItem != null
            ? GetTotalModifierFor(equippedItem, statType)
            : 0f;

        float baselineWithoutThisSlot = currentWithAllGear - equippedModifier;

        float newItemModifier = GetTotalModifierFor(newItem, statType);
        float projectedWithNew = baselineWithoutThisSlot + newItemModifier;

        float delta = newItemModifier - equippedModifier;

        string arrow = "-";
        string deltaTxt = "";

        if (delta > EPSILON)
        {
            arrow = "<color=green>▲</color>";
            deltaTxt = asPercent
                ? $" <color=green>(+{delta:0.##}%)</color>"
                : $" <color=green>(+{delta:0.##})</color>";
        }
        else if (delta < -EPSILON)
        {
            arrow = "<color=red>▼</color>";
            deltaTxt = asPercent
                ? $" <color=red>({delta:0.##}%)</color>"
                : $" <color=red>({delta:0.##})</color>";
        }

        string currentStr = asPercent ? $"{currentWithAllGear:0.##}%" : $"{currentWithAllGear:0.##}";
        string projectedStr = asPercent ? $"{projectedWithNew:0.##}%" : $"{projectedWithNew:0.##}";

        field.text = $"{statType}: {currentStr} → {projectedStr} {arrow}{deltaTxt}";
    }

    private float GetTotalModifierFor(Inventory_Item item, StatType statType)
    {
        float total = 0f;

        if (item?.modifiers != null)
        {
            foreach (var mod in item.modifiers)
            {
                if (mod.statType == statType)
                    total += mod.value;
            }
        }

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
    #endregion
}
