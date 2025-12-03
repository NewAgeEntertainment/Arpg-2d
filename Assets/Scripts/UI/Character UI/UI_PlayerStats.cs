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

        if (inventory != null)
            inventory.OnInventoryChange += UpdateStatsUI;
    }

    private void Start()
    {
        UpdateStatsUI();
    }

    // ===================== PUBLIC API =====================

    public void UpdateStatsUI()
    {
        if (playerStats == null) return;

        foreach (var statSlot in uiStatSlots)
        {
            StatType type = statSlot.StatType;

            // use our helper so derived stats are correct
            float current = GetCurrentStatValue(type);

            statSlot.UpdateStatDisplay(FormatValue(current, type));
        }
    }

    /// <summary>
    /// Called when hovering an item to show current → projected comparison.
    /// </summary>
    public void PreviewItem(Inventory_Item item)
    {
        if (playerStats == null) return;

        foreach (var statSlot in uiStatSlots)
        {
            StatType type = statSlot.StatType;

            float current = GetCurrentStatValue(type);
            float projected = GetProjectedStatValue(type, item);

            string display = FormatComparison(current, projected, type);
            statSlot.UpdateStatDisplay(display);
        }
    }

    public void ClearPreview()
    {
        UpdateStatsUI();
    }

    // ===================== STAT HELPERS =====================

    /// <summary>
    /// Returns the *current* value shown for a stat type.
    /// For Damage, SexualDamage, Resilience & SexualRestraint we use derived formulas.
    /// </summary>
    private float GetCurrentStatValue(StatType statType)
    {
        if (playerStats == null) return 0f;

        switch (statType)
        {
            // -------- Derived physical damage: Damage + Strength --------
            case StatType.Damage:
                {
                    float dmg = playerStats.offense.damage.GetValue();
                    float str = playerStats.major.strength.GetValue();
                    return Mathf.Floor(dmg + str * 0.5f);   // 0.5 damage per STR
                }


            // -------- Derived sexual damage: SexualDamage + Stroke --------
            case StatType.SexualDamage:
                {
                    float sexDmg = playerStats.sex.sexualDamage.GetValue();
                    float stroke = playerStats.sex.stroke.GetValue();
                    return Mathf.Floor(sexDmg + stroke * 0.5f);   // 0.5 per Stroke
                }

            // -------- Derived resilience shown in UI (gameplay uses same idea) --------
            case StatType.Resilience:
                {
                    return Mathf.Floor(playerStats.GetBaseResilience());
                }

            // -------- NEW: Sexual Restraint depends on SexualRestraint + Resilience --------
            // This makes the Sexual Restraint number react when Resilience changes.
            case StatType.SexualRestraint:
                {
                    float restraint = playerStats.sex.sexualRestraint.GetValue();
                    float res = playerStats.sex.resilience.GetValue();
                    return Mathf.Floor(restraint + res);
                }

            default:
                {
                    // all other stats: just the base stat value
                    return playerStats.GetStatByType(statType)?.GetValue() ?? 0f;
                }
        }
    }

    /// <summary>
    /// Returns current stat value with the *hovered item applied* (and currently
    /// equipped item on that slot removed). Handles Damage, SexualDamage,
    /// Resilience & SexualRestraint using their linked stats.
    /// </summary>
    private float GetProjectedStatValue(StatType statType, Inventory_Item item)
    {
        if (playerStats == null) return 0f;

        switch (statType)
        {
            case StatType.Damage:
                {
                    float projBaseDmg = GetProjectedBaseStat(StatType.Damage, item);
                    float projBaseStr = GetProjectedBaseStat(StatType.Strength, item);
                    return Mathf.Floor(projBaseDmg + projBaseStr * 0.5f);   // keep UI in sync
                }


            // -------- Sexual damage depends on SexualDamage + Stroke --------
            case StatType.SexualDamage:
                {
                    float projSexDmg = GetProjectedBaseStat(StatType.SexualDamage, item);
                    float projStroke = GetProjectedBaseStat(StatType.Stroke, item);

                    float projected = Mathf.Floor(projSexDmg + projStroke * 0.5f);  // keep UI in sync
                    return projected;
                }

            // -------- Resilience depends on Resilience × SexualRestraint --------
            case StatType.Resilience:
                {
                    // Base (raw) stats, without any formula
                    float baseRes = playerStats.sex.resilience.GetValue();
                    float baseRestraint = playerStats.sex.sexualRestraint.GetValue();

                    // Remove bonuses from currently equipped items
                    float eqResBonus = GetEquippedItemModifierValue(StatType.Resilience);
                    float eqRestraintBonus = GetEquippedItemModifierValue(StatType.SexualRestraint);

                    float newRes = baseRes - eqResBonus;
                    float newRestraint = baseRestraint - eqRestraintBonus;

                    // Apply bonuses from the hovered item
                    if (item != null && item.modifiers != null)
                    {
                        foreach (var mod in item.modifiers)
                        {
                            if (mod.statType == StatType.Resilience)
                                newRes += mod.value;
                            else if (mod.statType == StatType.SexualRestraint)
                                newRestraint += mod.value;
                        }
                    }

                    // Same formula as GetBaseResilience: res * sexualRestraint
                    return Mathf.Floor(newRes * newRestraint);
                }

            // -------- NEW: Sexual Restraint depends on SexualRestraint + Resilience --------
            case StatType.SexualRestraint:
                {
                    float projRestraint = GetProjectedBaseStat(StatType.SexualRestraint, item);
                    float projRes = GetProjectedBaseStat(StatType.Resilience, item);
                    return Mathf.Floor(projRestraint + projRes);
                }

            default:
                {
                    // Normal stats: just project that single stat
                    return GetProjectedBaseStat(statType, item);
                }
        }
    }

    /// <summary>
    /// Raw stat value from Player_Stats for a specific StatType.
    /// (Not the derived damage/resilience formulas.)
    /// </summary>
    private float GetBaseStat(StatType type)
    {
        return playerStats.GetStatByType(type)?.GetValue() ?? 0f;
    }

    /// <summary>
    /// Simulate "unequip current item, equip hovered item" for a single base stat.
    /// </summary>
    private float GetProjectedBaseStat(StatType statType, Inventory_Item item)
    {
        float currentBase = GetBaseStat(statType);
        float currentEquippedBonus = GetEquippedItemModifierValue(statType);
        float previewItemBonus = GetModifierValue(item, statType);

        return (currentBase - currentEquippedBonus) + previewItemBonus;
    }

    // ===================== DISPLAY HELPERS =====================

    private string FormatComparison(float current, float projected, StatType statType)
    {
        string unit = IsPercentageStat(statType) ? "%" : "";

        if (Mathf.Approximately(current, projected))
            return $"{current}{unit}";

        if (projected > current)
            return $"{current}{unit} → <color=green>{projected}{unit} ▲</color>";
        else
            return $"{current}{unit} → <color=red>{projected}{unit} ▼</color>";
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

    // ===================== ITEM MODIFIER HELPERS =====================

    private float GetModifierValue(Inventory_Item item, StatType statType)
    {
        float value = 0;
        if (item != null && item.modifiers != null)
        {
            foreach (var mod in item.modifiers)
            {
                if (mod.statType == statType)
                    value += mod.value;
            }
        }
        return value;
    }

    private float GetEquippedItemModifierValue(StatType statType)
    {
        float value = 0;
        if (inventory == null || inventory.equipList == null) return 0;

        foreach (var equipSlot in inventory.equipList)
        {
            if (equipSlot.equipedItem != null && equipSlot.equipedItem.modifiers != null)
            {
                foreach (var mod in equipSlot.equipedItem.modifiers)
                {
                    if (mod.statType == statType)
                        value += mod.value;
                }
            }
        }
        return value;
    }
}
