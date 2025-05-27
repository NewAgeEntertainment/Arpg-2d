using UnityEngine;
using UnityEngine.Rendering;

public class Entity_Stats : MonoBehaviour
{
    public Stat maxHealth; // Base maximum health points of the entity
    public Stat_MajorGroup major; // Major stats group containing strength, agility, intelligence, and vitality
    public Stat_OffenseGroup offense; // Offense stats group containing damage, crit power, crit chance, and elemental damages
    public Stat_DefenseGroup defense; // Defense stats group containing armor, evasion, and elemental resistances
    

    public float GetElementalDamage()
    {
        float fireDamage = offense.fireDamage.GetValue();
        float iceDamage = offense.iceDamage.GetValue();
        float lightningDamage = offense.lightningDamage.GetValue();
        float bonusElementalDamage = major.intelligence.GetValue(); // Bonus Elemental damage from Intelligence +1 per INT

        float highestDamage = fireDamage;

        if (iceDamage > highestDamage)
            highestDamage = iceDamage;

        if (lightningDamage > highestDamage)
            highestDamage = lightningDamage;

        if (highestDamage <= 0)
            return 0;

        float bonusFire = (fireDamage == highestDamage) ? 0 : fireDamage *.5f;
        float bonusIce = (iceDamage == highestDamage) ? 0 : iceDamage * .5f;
        float bonusLightning = (lightningDamage == highestDamage) ? 0 : lightningDamage * .5f;

        float weakerElementalDamage = bonusFire + bonusIce + bonusLightning; // Calculate the weaker elemental damage
        float finalDamage = highestDamage + weakerElementalDamage + bonusElementalDamage;

        return finalDamage;
    
    }

    public float GetPhysicalDamage(out bool isCrit)
    {
        float baseDamage = offense.damage.GetValue();
        float bonusDamage = major.strength.GetValue();
        float totalBaseDamage = baseDamage + bonusDamage;

        float baseCritChance = offense.critchance.GetValue();
        float bonusCritChance = major.Luck.GetValue() * .3f; // bonus crit chance from Agility
        float critChance = baseCritChance + bonusCritChance;

        float baseCritPower = offense.critpower.GetValue();
        float bonusCritPower = major.Luck.GetValue() * .5f;
        float critPower = (baseCritPower + bonusCritPower) / 100; // total crit power as multiplier (e.g., 150% crit power = 1.5 multiplier)

        isCrit = Random.Range(0, 100) < critChance;
        // damage Resualt equals iscrit if yes(?) than take total Base Damge and Multiply it by Critpower. if No that just do Total Base Damge
        float finalDamage = isCrit ? totalBaseDamage * critPower : totalBaseDamage; // 
        

        return finalDamage;
    }
    
    public float GetArmorMitigation(float armorReduction)
    {
        float baseArmor = defense.armor.GetValue();
        float bonusArmor = major.vitality.GetValue(); // bonus armor from Vitality: +1
        float totalArmor = baseArmor + bonusArmor;

        float reductionMultiplier = Mathf.Clamp(1 - armorReduction,0,1); // Calculate the reduction multiplier based on armor reduction
        float effectiveArmor = totalArmor * reductionMultiplier; // Apply the armor reduction to the total armor

        float mitigation = effectiveArmor / (effectiveArmor + 100);
        float mitigationCap = .85f; // Max mitigation will be capped at 85%

        float finalMitigation = Mathf.Clamp(mitigation, 0, mitigationCap);

        return finalMitigation;
    }

    public float GetArmorReduction()
    {
        // total armor reduction as multiplier (e.g., 30 / 100 = 0.3f - multiplier)
        float FinalReduction = offense.armorReduction.GetValue() / 100;

        return FinalReduction; // Return the final armor reduction value
    }

    public float GetEvasion()
    {
        float baseEvasion = defense.evasion.GetValue();
        float bonusEvasion = major.Luck.GetValue() * 0.5f; // each point of Agility gives +0.5% evasion

        float totalEvasion = baseEvasion + bonusEvasion;

        // Clamp the evasion value to a maximum cap to prevent excessive evasion
        float evasionCap = 85f; // Maximum evasion will be capped at 85%

        float finalEvasion = Mathf.Clamp(totalEvasion, 0f, evasionCap); // Clamp the final evasion value between 0 and the evasionCap.

        return finalEvasion;
    }
   
    //  this method calculates the total maxHp we have accorfing to the Vitality stat and the base Health.
    public float GetMaxHealth()
    {
        float baseMaxHealth = maxHealth.GetValue();
        float bonusMaxHealth = major.vitality.GetValue() * 5; // each point of Vitality gives +5 hp
        float finalMaxHealth = baseMaxHealth + bonusMaxHealth;

        return finalMaxHealth; // Return the final maximum health value

    }
}
