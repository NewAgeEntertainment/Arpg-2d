using TMPro;
using UnityEngine;

public class UI_MerchantStatsPanel : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI ownedCountText;

    [Header("Major Stats")]
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI intelligenceText;
    [SerializeField] private TextMeshProUGUI luckText;

    [Header("Additional Stats")]
    [SerializeField] private TextMeshProUGUI strokeText;
    [SerializeField] private TextMeshProUGUI resilienceText;
    [SerializeField] private TextMeshProUGUI critChanceText;
    [SerializeField] private TextMeshProUGUI critPowerText;

    [Header("Unique Effect")]
    [SerializeField] private TextMeshProUGUI uniqueEffectText;

    private Player_Stats playerStats;

    private void Awake()
    {
        playerStats = FindObjectOfType<Player_Stats>();
    }

    /// <summary>
    /// Show only the name and owned count (no stats).
    /// </summary>
    public void ShowOwnedOnly(Inventory_Item item, Inventory_Player playerInventory)
    {
        if (itemNameText)
            itemNameText.text = item?.itemData?.itemName ?? "";

        if (ownedCountText)
        {
            int owned = playerInventory != null && item?.itemData != null
                ? playerInventory.CountEverywhere(item.itemData)
                : 0;
            ownedCountText.text = $"Owned: {owned}";
        }

        ClearStats();
    }

    public void ShowItem(Inventory_Item item, Inventory_Player playerInventory)
    {
        if (item == null || item.itemData == null)
        {
            ShowPlayer(playerInventory);
            return;
        }

        // Consumables or Materials only show name & owned count
        if (item.itemData.itemType == ItemType.Consumable || item.itemData.itemType == ItemType.Material)
        {
            ShowOwnedOnly(item, playerInventory);
            return;
        }

        // Otherwise, show full stats
        if (itemNameText) itemNameText.text = item.itemData.itemName;
        if (ownedCountText)
        {
            int owned = playerInventory != null
                ? playerInventory.CountEverywhere(item.itemData)
                : 0;
            ownedCountText.text = $"Owned: {owned}";
        }

        UpdateStatLine(strengthText, StatType.Strength, item, playerInventory);
        UpdateStatLine(defenseText, StatType.Defense, item, playerInventory);
        UpdateStatLine(intelligenceText, StatType.Intelligence, item, playerInventory);
        UpdateStatLine(luckText, StatType.Luck, item, playerInventory);

        UpdateStatLine(strokeText, StatType.Stroke, item, playerInventory);
        UpdateStatLine(resilienceText, StatType.Resilience, item, playerInventory);
        UpdateStatLine(critChanceText, StatType.CritChance, item, playerInventory, isPercent: true);
        UpdateStatLine(critPowerText, StatType.CritPower, item, playerInventory, isPercent: true);

        if (uniqueEffectText)
        {
            if (item.itemEffect != null)
            {
                uniqueEffectText.gameObject.SetActive(true);
                uniqueEffectText.text =
                    $"<b>Unique Effect:</b>\n<color=#00FFFF>{item.itemEffect.effectDescription}</color>";
            }
            else
            {
                uniqueEffectText.gameObject.SetActive(false);
                uniqueEffectText.text = "";
            }
        }
    }

    public void ShowPlayer(Inventory_Player playerInventory)
    {
        if (playerStats == null) return;

        if (itemNameText) itemNameText.text = "";
        if (ownedCountText) ownedCountText.text = "";

        SetPlayerStatLine(strengthText, StatType.Strength);
        SetPlayerStatLine(defenseText, StatType.Defense);
        SetPlayerStatLine(intelligenceText, StatType.Intelligence);
        SetPlayerStatLine(luckText, StatType.Luck);

        SetPlayerStatLine(strokeText, StatType.Stroke);
        SetPlayerStatLine(resilienceText, StatType.Resilience);
        SetPlayerStatLine(critChanceText, StatType.CritChance, true);
        SetPlayerStatLine(critPowerText, StatType.CritPower, true);

        if (uniqueEffectText)
        {
            uniqueEffectText.gameObject.SetActive(false);
            uniqueEffectText.text = "";
        }
    }

    private void ClearStats()
    {
        if (strengthText) strengthText.text = "";
        if (defenseText) defenseText.text = "";
        if (intelligenceText) intelligenceText.text = "";
        if (luckText) luckText.text = "";
        if (strokeText) strokeText.text = "";
        if (resilienceText) resilienceText.text = "";
        if (critChanceText) critChanceText.text = "";
        if (critPowerText) critPowerText.text = "";
        if (uniqueEffectText)
        {
            uniqueEffectText.gameObject.SetActive(false);
            uniqueEffectText.text = "";
        }
    }

    private void UpdateStatLine(TextMeshProUGUI text, StatType statType, Inventory_Item item, Inventory_Player playerInventory, bool isPercent = false)
    {
        if (!text) return;
        var cmp = StatCompareUtility.CompareStat(playerStats, playerInventory, item, statType);
        text.text = $"{statType}: {StatCompareUtility.AsValue(cmp.current, isPercent)} → {StatCompareUtility.AsValue(cmp.projected, isPercent)} {StatCompareUtility.ArrowDelta(cmp.delta)}";
    }

    private void SetPlayerStatLine(TextMeshProUGUI text, StatType statType, bool isPercent = false)
    {
        if (!text) return;
        float value = playerStats.GetStatByType(statType)?.GetValue() ?? 0f;
        text.text = $"{statType}: {StatCompareUtility.AsValue(value, isPercent)}";
    }
}
