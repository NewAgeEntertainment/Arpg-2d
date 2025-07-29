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

        Dictionary<string, float> statGains = ApplyCumulativeLevelBonuses();

        // Trigger level-up popup UI
        var popup = GetComponent<Player>()?.ui?.levelUpPopup;
        popup?.ShowPopup(CurrentLevel, statGains);
    }

    public float GetNextLevelRequirement()
    {
        return BASE_EXP_REQUIREMENT * Mathf.Pow(EXP_GROWTH_RATE, CurrentLevel - 1);
    }

    private Dictionary<string, float> ApplyCumulativeLevelBonuses()
    {
        var statGains = new Dictionary<string, float>();

        if (levelScalingData == null)
        {
            Debug.LogWarning("[Player_Stats] No LevelScalingSO assigned.");
            return statGains;
        }

        float hpGain = levelScalingData.Evaluate(levelScalingData.healthCurve, CurrentLevel);
        float mpGain = levelScalingData.Evaluate(levelScalingData.manaCurve, CurrentLevel);
        float strGain = levelScalingData.Evaluate(levelScalingData.strengthCurve, CurrentLevel);
        float defGain = levelScalingData.Evaluate(levelScalingData.defenseCurve, CurrentLevel);
        float intGain = levelScalingData.Evaluate(levelScalingData.intelligenceCurve, CurrentLevel);
        float luckGain = levelScalingData.Evaluate(levelScalingData.luckCurve, CurrentLevel);
        float vitGain = levelScalingData.Evaluate(levelScalingData.vitalityCurve, CurrentLevel);

        string tag = $"{LEVEL_UP_TAG_PREFIX}{CurrentLevel}";

        // Apply modifiers to correct stat group objects
        resources.maxHealth.AddModifier(hpGain, tag);
        resources.maxMana.AddModifier(mpGain, tag);
        major.strength.AddModifier(strGain, tag);
        defense.armor.AddModifier(defGain, tag);
        major.intelligence.AddModifier(intGain, tag);
        major.luck.AddModifier(luckGain, tag);
        major.vitality.AddModifier(vitGain, tag);

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

    public bool CanApplyBuffOf(string source)
    {
        return activeBuff.Contains(source) == false;
    }

    public void ApplyBuff(BuffEffectData[] buffToApply, float duration, string source)
    {
        StartCoroutine(buffCo(buffToApply, duration, source));
    }

    private IEnumerator buffCo(BuffEffectData[] buffToApply, float duration, string source)
    {
        activeBuff.Add(source);

        foreach (var buff in buffToApply)
            GetStatByType(buff.type).AddModifier(buff.value, source);

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffToApply)
            GetStatByType(buff.type).RemoveModifier(source);

        inventory.NotifyInventoryChanged();
        activeBuff.Remove(source);
    }

    #endregion

    public float GetStatValue(StatType type)
    {
        return GetStatByType(type).GetValue();
    }
}
