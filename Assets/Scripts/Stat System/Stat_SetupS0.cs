using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Default Stat Setup", fileName = "Default Stat Setup")]
public class Stat_SetupS0 : ScriptableObject
{
    [Header("Resources")]
    public float maxHealth = 100;
    public float healthRegen;
    public float maxMana = 50;
    public float manaRegen;

    [Header("Offense - Physical Damage")]
    public float attackSpeed = 1;
    public float damage = 10;
    public float critChance;
    public float critPower;
    public float armorReduction;

    [Header("Offense - Elemental Damage")]
    public float fireDamage;
    public float iceDamage;
    public float lightningDamage;
    public float poisonDamage;

    [Header("Defense - Physical Damage")]
    public float armor;
    public float evasion;

    [Header("Defense - Elemental Damage")]
    public float fireResistance;
    public float iceResistance;
    public float lightningResistance;
    public float poisonResistance;

    [Header("Major Stats")]
    public float strength;
    public float luck;
    public float intelligence;
    public float Vitality;

    [Header("Sex Stats")]
    public float Stroke;
    public float Defiance;
}
