using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Stats : Entity_Stats
{
    // UI/event hooks
    public event Action<float, float> OnExpChanged;                // (current, nextReq)
    public event Action<float, float, int> OnSexExpChanged;        // (current, nextReq, level)
    public event Action<int> OnLevelChanged;                       // (newLevel)

    // Progress
    public float CurrentEXP { get; private set; } = 0f;
    public int CurrentLevel { get; private set; } = 1;

    [Header("Level Scaling")]
    [SerializeField] private LevelScalingSO levelScalingData;

    private const float BASE_EXP_REQUIREMENT = 100f;
    private const float EXP_GROWTH_RATE = 1.5f;

    private const string LEVEL_UP_TAG_PREFIX = "LevelUp_L";

    // Sex EXP
    public float CurrentSexEXP { get; private set; } = 0f;
    public int CurrentSexLevel { get; private set; } = 1;

    private const float SEX_EXP_BASE_REQ = 75f;
    private const float SEX_EXP_GROWTH = 1.4f;

    // Buffs
    private readonly List<string> activeBuff = new List<string>();
    private Inventory_Player inventory;

    protected override void Awake()
    {
        base.Awake();
        inventory = GetComponent<Inventory_Player>();
    }

    // ========= Normal EXP & Level-Up =========

    public void AddEXP(float amount)
    {
        CurrentEXP += amount;
        RaiseExp();

        while (CurrentEXP >= GetNextLevelRequirement())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        CurrentEXP -= GetNextLevelRequirement();
        CurrentLevel++;

        OnLevelChanged?.Invoke(CurrentLevel); // notify listeners first
        RaiseExp();                           // refresh EXP bar for new threshold

        var statGains = ApplyCumulativeLevelBonuses();

        // 🔹 NEW: force HUD HP/MP/EXP bars to refresh immediately on level-up
        var ui = UI.Instance;
        if (ui != null && ui.inGameUI != null)
        {
            ui.inGameUI.ForceRefreshFromCurrentState();
        }

        // Compatible with UI_LevelUpPopup.Show(int, IDictionary<string,float>)
        GetComponent<Player>()?.ui?.levelUpPopup?.Show(CurrentLevel, statGains);
    }

    public float GetNextLevelRequirement()
    {
        return BASE_EXP_REQUIREMENT * Mathf.Pow(EXP_GROWTH_RATE, Mathf.Max(0, CurrentLevel - 1));
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

        // Apply to stats
        resources.maxHealth.AddModifier(hpGain, StatModType.Flat, tag);
        resources.maxMana.AddModifier(mpGain, StatModType.Flat, tag);
        major.strength.AddModifier(strGain, StatModType.Flat, tag);
        defense.armor.AddModifier(defGain, StatModType.Flat, tag);
        major.intelligence.AddModifier(intGain, StatModType.Flat, tag);
        major.luck.AddModifier(luckGain, StatModType.Flat, tag);
        major.vitality.AddModifier(vitGain, StatModType.Flat, tag);

        // For popup
        statGains["Max Health"] = hpGain;
        statGains["Max Mana"] = mpGain;
        statGains["Strength"] = strGain;
        statGains["Defense"] = defGain;
        statGains["Intelligence"] = intGain;
        statGains["Luck"] = luckGain;
        statGains["Vitality"] = vitGain;

        return statGains;
    }

    private void RaiseExp() => OnExpChanged?.Invoke(CurrentEXP, GetNextLevelRequirement());

    // ========= Sex EXP / Sex Level =========

    public void AddSexEXP(float amount)
    {
        CurrentSexEXP += amount;
        RaiseSex();

        while (CurrentSexEXP >= GetNextSexLevelRequirement())
        {
            LevelUpSex();
        }
    }

    private void LevelUpSex()
    {
        CurrentSexEXP -= GetNextSexLevelRequirement();
        CurrentSexLevel++;
        RaiseSex();
    }

    public float GetNextSexLevelRequirement()
    {
        return SEX_EXP_BASE_REQ * Mathf.Pow(SEX_EXP_GROWTH, Mathf.Max(0, CurrentSexLevel - 1));
    }

    private void RaiseSex() => OnSexExpChanged?.Invoke(CurrentSexEXP, GetNextSexLevelRequirement(), CurrentSexLevel);

    // ========= Buff System =========

    public bool CanApplyBuffOf(string source) => !activeBuff.Contains(source);

    public void ApplyBuff(BuffEffectData[] buffToApply, float duration, string source)
    {
        if (buffToApply == null || buffToApply.Length == 0) return;
        if (!CanApplyBuffOf(source)) return;
        StartCoroutine(buffCo(buffToApply, duration, source));
    }

    private IEnumerator buffCo(BuffEffectData[] buffToApply, float duration, string source)
    {
        activeBuff.Add(source);

        foreach (var buff in buffToApply)
            GetStatByType(buff.type).AddModifier(buff.value, StatModType.Flat, source);

        inventory?.NotifyInventoryChanged();

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffToApply)
            GetStatByType(buff.type).RemoveModifier(source);

        inventory?.NotifyInventoryChanged();
        activeBuff.Remove(source);
    }

    // ========= Save/Load helpers =========

    /// <summary>Set level & exp and immediately notify listeners (UI refresh).</summary>
    public void SetLevelAndExp(int level, float exp)
    {
        CurrentLevel = Mathf.Max(1, level);
        CurrentEXP = Mathf.Max(0, exp);
        OnLevelChanged?.Invoke(CurrentLevel);
        RaiseExp();
    }

    /// <summary>Set sex level & exp and immediately notify listeners (UI refresh).</summary>
    public void SetSexLevelAndExp(int sexLevel, float sexExp)
    {
        CurrentSexLevel = Mathf.Max(1, sexLevel);
        CurrentSexEXP = Mathf.Max(0, sexExp);
        RaiseSex();
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

    /// <summary>Use this after a New Game / Load if you need to force all bars to repaint.</summary>
    public void ForceRaiseAllExpSignals()
    {
        OnLevelChanged?.Invoke(CurrentLevel);
        RaiseExp();
        RaiseSex();
    }

    public float GetStatValue(StatType type) => GetStatByType(type).GetValue();
}
