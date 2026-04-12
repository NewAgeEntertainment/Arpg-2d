using UnityEngine;
using System;

public enum SkillCategory
{
    Combat,
    Sex
}

[CreateAssetMenu(menuName = "RPG Setup/Skill Data", fileName = "Skill data - ")]
public class Skill_DataSO : ScriptableObject
{
    [Header("Identity")]
    public string id;

    [Header("Skill Description")]
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;

    [Header("Unlock & Upgrade")]
    public int cost;
    public bool unlockedByDefault;
    public SkillType skillType;
    public SkillCategory category;

    [Header("Upgrade & Scaling")]
    public UpgradeData upgradeData;

    [Header("Optional Direct Key Use")]
    public bool allowDirectKeyUse = false;
    public KeyCode directKey = KeyCode.None;

    [Tooltip("Optional Rewired action name. If set, this is checked in addition to directKey.")]
    public string rewiredActionName = "";
}

[Serializable]
public class UpgradeData
{
    public SkillUpgradeType upgradeType;

    [Header("Combat Info")]
    public float cooldown;
    public float manaCost;

    [Header("Stat Scaling")]
    public DamageScaleData damageScale;
}

[Serializable]
public class DamageScaleData
{
    [Header("Damage")]
    public float physical = 1;
    public float elemental = 1;

    [Header("General Scaling")]
    public float basePower = 10f;
    public float scalingMultiplier = 1f;
    public StatType scalingStat = StatType.Strength;

    [Header("Chill")]
    public float chillDuration = 3;
    public float chillSlowMultiplier = 0.2f;

    [Header("Burn")]
    public float burnDuration = 3f;
    public float burnDamageScale = 1f;

    [Header("Poison")]
    public float poisonDuration = 3f;
    public float poisonDamageScale = 1f;

    [Header("Shock")]
    public float shockDuration = 3f;
    public float shockDamageScale = 1f;
    public float shockCharge = 0.4f;
}