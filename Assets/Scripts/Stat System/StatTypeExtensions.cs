using UnityEngine;

public static class StatTypeExtensions
{
    public static string GetStatName(this StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return "Max Health";
            case StatType.HealthRegen: return "Health Regen";
            case StatType.MaxMana: return "Max Mana";
            case StatType.ManaRegen: return "Mana Regen";
            case StatType.Strength: return "Strength";
            case StatType.Luck: return "Luck";
            case StatType.Intelligence: return "Intelligence";
            case StatType.Vitality: return "Vitality";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.Damage: return "Damage";
            case StatType.CritChance: return "Crit Chance";
            case StatType.CritPower: return "Crit Power";
            case StatType.ArmorReduction: return "Armor Reduction";
            case StatType.FireDamage: return "Fire Damage";
            case StatType.IceDamage: return "Ice Damage";
            case StatType.PoisonDamage: return "Poison Damage";
            case StatType.LightningDamage: return "Lightning Damage";
            case StatType.Armor: return "Armor";
            case StatType.Evasion: return "Evasion";
            case StatType.IceResistance: return "Ice Resistance";
            case StatType.FireResistance: return "Fire Resistance";
            case StatType.PoisonResistance: return "Poison Resistance";
            case StatType.LightningResistance: return "Lightning Resistance";
            case StatType.SexualDamage: return "Sexual Damage";
            case StatType.Resilience: return "Resilience";
            default: return "Unknown Stat";
        }
    }
}

