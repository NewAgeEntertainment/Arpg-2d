using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Saves and loads player stats: Sex EXP, Sex Level, Gold, Health, Mana, Base Stats, EXP, Level, and Modifiers.
/// </summary>
public class PlayerStatsSaver : Saver
{
    [Tooltip("Reference to the Player script.")]
    public Player player;

    [Serializable]
    public class StatData
    {
        public float baseValue;
        public List<StatModifier> modifiers = new();
    }

    [Serializable]
    public class SaveData
    {
        public int sexLevel;
        public float sexEXP;

        public int normalLevel;
        public float normalEXP;

        public int gold;
        public float currentHealth;
        public float currentMana;

        public StatData baseMaxHealth;
        public StatData baseMaxMana;
    }

    public override string RecordData()
    {
        if (player == null)
        {
            Debug.LogWarning("[PlayerStatsSaver] Player reference is missing!");
            return string.Empty;
        }

        var health = player.health;
        var mana = player.mana;

        var saveData = new SaveData
        {
            sexLevel = player.SexLevel,
            sexEXP = player.CurrentSexExp,

            normalLevel = player.Level,
            normalEXP = player.CurrentExp,

            gold = player.inventory.gold,

            currentHealth = health?.GetCurrentHealth() ?? 0,
            currentMana = mana?.GetCurrentMana() ?? 0,

            baseMaxHealth = new StatData
            {
                baseValue = player.stats.resources.maxHealth.BaseValue,
                modifiers = player.stats.resources.maxHealth.Modifiers
            },

            baseMaxMana = new StatData
            {
                baseValue = player.stats.resources.maxMana.BaseValue,
                modifiers = player.stats.resources.maxMana.Modifiers
            }
        };

        return SaveSystem.Serialize(saveData);
    }

    public override void ApplyData(string s)
    {
        StartCoroutine(DelayedApply(s));
    }

    private IEnumerator DelayedApply(string s)
    {
        yield return null; // Wait 1 frame

        var data = SaveSystem.Deserialize<SaveData>(s);
        if (data == null || player == null) yield break;

        // Sex Stats
        player.SetSexLevel(data.sexLevel);
        player.SetCurrentSexEXP(data.sexEXP);

        // Normal Level & EXP
        player.stats.SetLevel(data.normalLevel);
        player.stats.SetEXP(data.normalEXP);

        // Gold
        player.inventory.gold = data.gold;

        // Base Stat Setup
        var stats = player.stats;

        stats.resources.maxHealth.SetBaseValue(data.baseMaxHealth.baseValue);
        stats.resources.maxHealth.ClearAllModifiers();
        stats.resources.maxHealth.AddModifiers(data.baseMaxHealth.modifiers);

        stats.resources.maxMana.SetBaseValue(data.baseMaxMana.baseValue);
        stats.resources.maxMana.ClearAllModifiers();
        stats.resources.maxMana.AddModifiers(data.baseMaxMana.modifiers);

        // Current HP/MP
        player.health.SetCurrentHealth(data.currentHealth);
        player.mana.SetCurrentMana(data.currentMana);

        // Update UI
        player.ui?.inGameUI?.UpdateGoldDisplay(data.gold);
        player.ui?.inGameUI?.UpdateExpBar();
        player.ui?.inGameUI?.UpdateSexExpBar();
        player.ui?.StatusPanel?.UpdateStatus(player);
    }
}
