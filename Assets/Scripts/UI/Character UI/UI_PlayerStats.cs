using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerStats : MonoBehaviour
{
    private UI_StatSlot[] uiStatSlots;
    private Inventory_Player inventory;
    private Player_Stats playerStats;

    private void Awake()
    {
        uiStatSlots = GetComponentsInChildren<UI_StatSlot>();
        inventory = FindFirstObjectByType<Inventory_Player>();
        playerStats = FindFirstObjectByType<Player_Stats>();

        foreach (var slot in uiStatSlots)
        {
            slot.Setup(playerStats);
        }

        inventory.OnInventoryChange += UpdateStatsUI;
    }

    private void Start()
    {
        UpdateStatsUI();
    }

    public void UpdateStatsUI()
    {
        foreach (var statSlot in uiStatSlots)
        {
            float current = playerStats.GetStatByType(statSlot.StatType)?.GetValue() ?? 0;
            statSlot.UpdateStatDisplay(FormatValue(current, statSlot.StatType));
        }
    }

    public void PreviewItem(Inventory_Item item)
    {
        foreach (var statSlot in uiStatSlots)
        {
            float current = playerStats.GetStatByType(statSlot.StatType)?.GetValue() ?? 0;
            float currentEquippedBonus = GetEquippedItemModifierValue(statSlot.StatType);
            float previewItemBonus = GetModifierValue(item, statSlot.StatType);

            float projected = (current - currentEquippedBonus) + previewItemBonus;

            string display = FormatComparison(current, projected, statSlot.StatType);
            statSlot.UpdateStatDisplay(display);
        }
    }

    public void ClearPreview()
    {
        UpdateStatsUI();
    }

    private string FormatComparison(float current, float projected, StatType statType)
    {
        string unit = IsPercentageStat(statType) ? "%" : "";

        if (projected > current)
            return $"{current}{unit} → <color=green>{projected}{unit} ▲</color>";
        else if (projected < current)
            return $"{current}{unit} → <color=red>{projected}{unit} ▼</color>";
        else
            return $"{current}{unit}";
    }

    private string FormatValue(float value, StatType statType)
    {
        return IsPercentageStat(statType) ? $"{value}%" : value.ToString();
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

    private float GetModifierValue(Inventory_Item item, StatType statType)
    {
        float value = 0;
        if (item != null && item.modifiers != null)
        {
            foreach (var mod in item.modifiers)
            {
                if (mod.statType == statType)
                {
                    value += mod.value;
                }
            }
        }
        return value;
    }

    private float GetEquippedItemModifierValue(StatType statType)
    {
        float value = 0;
        foreach (var equipSlot in inventory.equipList)
        {
            if (equipSlot.equipedItem != null && equipSlot.equipedItem.modifiers != null)
            {
                foreach (var mod in equipSlot.equipedItem.modifiers)
                {
                    if (mod.statType == statType)
                    {
                        value += mod.value;
                    }
                }
            }
        }
        return value;
    }
}
