using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Companion leveling & stats (no external scaling asset).
/// Matches Player_Stats events/signatures so existing UI hooks work.
/// </summary>
public class Companion_Stats : Entity_Stats
{
    // === Events (match Player_Stats) ===
    public event Action<float, float> OnExpChanged;   // (current, nextReq)
    public event Action<int> OnLevelChanged;          // (newLevel)

    // === Progress ===
    public float CurrentEXP { get; private set; } = 0f;
    public int CurrentLevel { get; private set; } = 1;

    // Same curve style as Player_Stats
    private const float BASE_EXP_REQUIREMENT = 100f;
    private const float EXP_GROWTH_RATE = 1.5f;

    private const string LEVEL_UP_TAG_PREFIX = "C_LevelUp_L";

    [Header("On Level Up")]
    [SerializeField] private bool refillHealthOnLevel = true;
    [SerializeField] private bool refillManaOnLevel = true;

    protected override void Awake()
    {
        base.Awake();
        // Optionally force an initial raise so UI paints at start:
        RaiseAllSignals();
    }

    // ========= EXP & Level-Up =========

    public void AddEXP(float amount)
    {
        if (amount <= 0f) return;
        CurrentEXP += amount;
        RaiseExp();

        while (CurrentEXP >= GetNextLevelRequirement())
            LevelUp();
    }

    // alias used by reflection callers (Enemy_Health rewards)
    public void GainEXP(float amount) => AddEXP(amount);

    private void LevelUp()
    {
        float req = GetNextLevelRequirement();
        CurrentEXP = Mathf.Max(0f, CurrentEXP - req);
        CurrentLevel = Mathf.Max(1, CurrentLevel + 1);

        OnLevelChanged?.Invoke(CurrentLevel);
        RaiseExp(); // new threshold

        ApplyCumulativeLevelBonuses();

        if (refillHealthOnLevel)
        {
            var hp = GetComponent<Entity_Health>();
            if (hp != null) hp.SetCurrentHealth(hp.GetMaxHealth());
        }
        if (refillManaOnLevel)
        {
            var mp = GetComponent<Entity_Mana>();
            if (mp != null) mp.SetCurrentMana(mp.GetMaxMana());
        }
    }

    public float GetNextLevelRequirement()
    {
        return BASE_EXP_REQUIREMENT * Mathf.Pow(EXP_GROWTH_RATE, Mathf.Max(0, CurrentLevel - 1));
    }

    private void RaiseExp() => OnExpChanged?.Invoke(CurrentEXP, GetNextLevelRequirement());

    public float GetExpPercent()
    {
        float req = Mathf.Max(0.0001f, GetNextLevelRequirement());
        return Mathf.Clamp01(CurrentEXP / req);
    }

    // ========= Stat bonuses (per level) =========
    private Dictionary<string, float> ApplyCumulativeLevelBonuses()
    {
        var statGains = new Dictionary<string, float>();
        int level = CurrentLevel;

        float hpGain = StatGrowthCalculator.GetMaxHealth(level);
        float mpGain = StatGrowthCalculator.GetMaxMana(level);
        float strGain = StatGrowthCalculator.GetStrength(level);
        float defGain = StatGrowthCalculator.GetDefense(level);
        float intGain = StatGrowthCalculator.GetIntelligence(level);
        float luckGain = StatGrowthCalculator.GetLuck(level);
        float vitGain = StatGrowthCalculator.GetVitality(level);

        string tag = $"{LEVEL_UP_TAG_PREFIX}{level}";

        resources.maxHealth.AddModifier(hpGain, StatModType.Flat, tag);
        resources.maxMana.AddModifier(mpGain, StatModType.Flat, tag);
        major.strength.AddModifier(strGain, StatModType.Flat, tag);
        defense.armor.AddModifier(defGain, StatModType.Flat, tag);
        major.intelligence.AddModifier(intGain, StatModType.Flat, tag);
        major.luck.AddModifier(luckGain, StatModType.Flat, tag);
        major.vitality.AddModifier(vitGain, StatModType.Flat, tag);

        // Optional: return map in case you later want a popup for companions.
        statGains["Max Health"] = hpGain;
        statGains["Max Mana"] = mpGain;
        statGains["Strength"] = strGain;
        statGains["Defense"] = defGain;
        statGains["Intelligence"] = intGain;
        statGains["Luck"] = luckGain;
        statGains["Vitality"] = vitGain;

        return statGains;
    }

    // ========= Save/Load helpers =========
    public void SetLevelAndExp(int level, float exp)
    {
        CurrentLevel = Mathf.Max(1, level);
        CurrentEXP = Mathf.Max(0, exp);
        OnLevelChanged?.Invoke(CurrentLevel);
        RaiseExp();
    }

    public void SetLevel(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
        OnLevelChanged?.Invoke(CurrentLevel);
        RaiseExp();
    }

    public void SetEXP(float exp)
    {
        CurrentEXP = Mathf.Max(0, exp);
        RaiseExp();
    }

    public void RaiseAllSignals()
    {
        OnLevelChanged?.Invoke(CurrentLevel);
        RaiseExp();
    }
}
