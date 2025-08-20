using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Stats : Entity_Stats
{
    public float CurrentEXP { get; private set; } = 0;
    public int CurrentLevel { get; private set; } = 1;

    [Header("Level Scaling")]
    [SerializeField] private LevelScalingSO levelScalingData;

    private const float BASE_EXP_REQUIREMENT = 100f;
    private const float EXP_GROWTH_RATE = 1.5f;

    private const string LEVEL_UP_TAG_PREFIX = "LevelUp_L";

    private List<string> activeBuff = new List<string>();
    private Inventory_Player inventory;

    protected override void Awake()
    {
        base.Awake();
        inventory = GetComponent<Inventory_Player>();
    }

    #region Normal EXP & Level-Up

    public void AddEXP(float amount)
    {
        CurrentEXP += amount;
        Debug.Log($"[Player_Stats] Gained EXP: {amount} | Total EXP: {CurrentEXP}");

        while (CurrentEXP >= GetNextLevelRequirement())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        CurrentEXP -= GetNextLevelRequirement();
        CurrentLevel++;
        Debug.Log($"[Player_Stats] Leveled Up! New Level: {CurrentLevel}");

        // Calculate and apply gains:
        Dictionary<string, float> statGains = ApplyCumulativeLevelBonuses();

        // SHOW POPUP via the service (no local reference needed):
        LevelUpPopupService.Show(CurrentLevel, statGains);
    }

    public float GetNextLevelRequirement()
    {
        return BASE_EXP_REQUIREMENT * Mathf.Pow(EXP_GROWTH_RATE, CurrentLevel - 1);
    }

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

        // Apply to stats:
        resources.maxHealth.AddModifier(hpGain, StatModType.Flat, tag);
        resources.maxMana.AddModifier(mpGain, StatModType.Flat, tag);
        major.strength.AddModifier(strGain, StatModType.Flat, tag);
        defense.armor.AddModifier(defGain, StatModType.Flat, tag);
        major.intelligence.AddModifier(intGain, StatModType.Flat, tag);
        major.luck.AddModifier(luckGain, StatModType.Flat, tag);
        major.vitality.AddModifier(vitGain, StatModType.Flat, tag);

        // For popup:
        statGains["Max Health"] = hpGain;
        statGains["Max Mana"] = mpGain;
        statGains["Strength"] = strGain;
        statGains["Defense"] = defGain;
        statGains["Intelligence"] = intGain;
        statGains["Luck"] = luckGain;
        statGains["Vitality"] = vitGain;

        return statGains;
    }

    #endregion

    #region Sex EXP / Sex Level

    public float CurrentSexEXP { get; private set; } = 0;
    public int CurrentSexLevel { get; private set; } = 1;

    private const float SEX_EXP_BASE_REQ = 75f;
    private const float SEX_EXP_GROWTH = 1.4f;

    public void AddSexEXP(float amount)
    {
        CurrentSexEXP += amount;
        Debug.Log($"[Player_Stats] Gained Sex EXP: {amount} | Total: {CurrentSexEXP}");

        while (CurrentSexEXP >= GetNextSexLevelRequirement())
        {
            LevelUpSex();
        }
    }

    private void LevelUpSex()
    {
        CurrentSexEXP -= GetNextSexLevelRequirement();
        CurrentSexLevel++;
        Debug.Log($"[Player_Stats] Sex Level Up! New Sex Level: {CurrentSexLevel}");
    }

    public float GetNextSexLevelRequirement()
    {
        return SEX_EXP_BASE_REQ * Mathf.Pow(SEX_EXP_GROWTH, CurrentSexLevel - 1);
    }

    #endregion

    #region Buff System

    public bool CanApplyBuffOf(string source) => activeBuff.Contains(source) == false;

    public void ApplyBuff(BuffEffectData[] buffToApply, float duration, string source)
    {
        StartCoroutine(buffCo(buffToApply, duration, source));
    }

    private IEnumerator buffCo(BuffEffectData[] buffToApply, float duration, string source)
    {
        activeBuff.Add(source);

        foreach (var buff in buffToApply)
            GetStatByType(buff.type).AddModifier(buff.value, StatModType.Flat, source);

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffToApply)
            GetStatByType(buff.type).RemoveModifier(source);

        inventory?.NotifyInventoryChanged();
        activeBuff.Remove(source);
    }

    #endregion

    // Save/Load helpers
    public void SetLevelAndExp(int level, float exp)
    {
        CurrentLevel = Mathf.Max(1, level);
        CurrentEXP = Mathf.Max(0, exp);
    }

    public void SetSexLevelAndExp(int sexLevel, float sexExp)
    {
        CurrentSexLevel = Mathf.Max(1, sexLevel);
        CurrentSexEXP = Mathf.Max(0, sexExp);
    }

    public void SetLevel(int level) => CurrentLevel = level;
    public void SetEXP(float exp) => CurrentEXP = exp;

    public float GetStatValue(StatType type) => GetStatByType(type).GetValue();
}
