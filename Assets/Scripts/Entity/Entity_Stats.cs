using UnityEngine;

public class Entity_Stats : MonoBehaviour
{
    public Stat_SetupS0 defaultStatSetup; // Reference to the stat setup ScriptableObject containing base values for stats


    public Stat_ResourceGroup resources; // Resource stats group containing max health, health regen, max mana, and mana regen
    public Stat_OffenseGroup offense; // Offense stats group containing damage, crit power, crit chance, and elemental damages
    public Stat_DefenseGroup defense; // Defense stats group containing armor, evasion, and elemental resistances
    public Stat_MajorGroup major; // Major stats group containing strength, agility, intelligence, and vitality

    public AttackData GetAttackData(DamageScaleData scaleData)
    {
        return new AttackData(this, scaleData);
    }

    public float GetElementalDamage(out ElementType element, float scaleFactor = 1)
    {
        float fireDamage = offense.fireDamage.GetValue();
        float iceDamage = offense.iceDamage.GetValue();
        float lightningDamage = offense.lightningDamage.GetValue();
        float poisonDamage = offense.poisonDamage.GetValue(); // Poison damage, if applicable
        float bonusElementalDamage = major.intelligence.GetValue(); // Bonus Elemental damage from Intelligence +1 per INT

        float highestDamage = fireDamage;
        element = ElementType.Fire; // Default to Fire element

        if (iceDamage > highestDamage)
        {
            highestDamage = iceDamage;
            element = ElementType.Ice; // Set element to Ice if it has the highest damage
        }

        if (poisonDamage > highestDamage)
        {
            highestDamage = poisonDamage;
            element = ElementType.Poison; // Set element to Poison if it has the highest damage
        }


        if (lightningDamage > highestDamage)
        {
            highestDamage = lightningDamage;
            element = ElementType.Lightning; // Set element to Lightning if it has the highest damage
        }

        if (highestDamage <= 0)
        {
            element = ElementType.None; // If all elemental damages are 0 or less, set element to None
            return 0;
        }

        float bonusFire = (fireDamage == highestDamage) ? 0 : fireDamage * .5f;
        float bonusIce = (iceDamage == highestDamage) ? 0 : iceDamage * .5f;
        float bonusLightning = (lightningDamage == highestDamage) ? 0 : lightningDamage * .5f;
        float bonusPoison = (poisonDamage == highestDamage) ? 0 : poisonDamage * .5f; // Bonus damage for weaker elemental types

        float weakerElementalDamage = bonusFire + bonusIce + bonusLightning + poisonDamage; // Calculate the weaker elemental damage
        float finalDamage = highestDamage + weakerElementalDamage + bonusElementalDamage;

        return finalDamage * scaleFactor;

    }

    public float GetElementalResistance(ElementType element)
    {
        float baseResistance = 0;
        float bonusResistance = major.intelligence.GetValue() * .5f; // Bonus resistance from intelligence: +0.5% per INT

        switch (element)
        {
            case ElementType.Fire:
                baseResistance = defense.fireRes.GetValue();
                break;
            case ElementType.Ice:
                baseResistance = defense.iceRes.GetValue();
                break;
            case ElementType.Lightning:
                baseResistance = defense.lightningRes.GetValue();
                break;
            case ElementType.Poison:
                baseResistance = defense.poisonRes.GetValue(); // Poison resistance, if applicable
                break;
        }

        float resistance = baseResistance + bonusResistance;
        float resistanceCap = 75f; // Resistance will be capped at 75%;
        float finalResistance = Mathf.Clamp(resistance, 0, resistanceCap) / 100; // Convert value into 0 to 1 multiplier

        return finalResistance;
    }

    public float GetPhysicalDamage(out bool isCrit, float scaleFactor = 1)
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


        return finalDamage * scaleFactor;
    }

    public float GetArmorMitigation(float armorReduction)
    {
        float baseArmor = defense.armor.GetValue();
        float bonusArmor = major.vitality.GetValue(); // bonus armor from Vitality: +1
        float totalArmor = baseArmor + bonusArmor;

        float reductionMultiplier = Mathf.Clamp(1 - armorReduction, 0, 1); // Calculate the reduction multiplier based on armor reduction
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
        float baseMaxHealth = resources.maxHealth.GetValue();
        float bonusMaxHealth = major.vitality.GetValue() * 5; // each point of Vitality gives +5 hp
        float finalMaxHealth = baseMaxHealth + bonusMaxHealth;

        return finalMaxHealth; // Return the final maximum health value

    }

    public float GetMaxMana()
    {
        float baseMaxMana = resources.maxMana.GetValue();
        float bonusMaxMana = major.intelligence.GetValue() * 5; // each point of Intelligence gives +5 mana
        float finalMaxMana = baseMaxMana + bonusMaxMana;

        return finalMaxMana; // Return the final maximum mana value
    }

    // This method retrieves a specific stat based on its type.
    public Stat GetStatByType(StatType type)
    {
        switch (type) // switch statement to determine the type of stat requested
        {
            case StatType.MaxHealth:
                return resources.maxHealth;
            case StatType.HealthRegen:
                return resources.healthRegen;
            case StatType.MaxMana:
                return resources.maxMana;
            case StatType.ManaRegen:
                return resources.manaRegen;

            case StatType.Strength:
                return major.strength;
            case StatType.Luck:
                return major.Luck;
            case StatType.Intelligence:
                return major.intelligence;
            case StatType.Vitality:
                return major.vitality;

            case StatType.AttackSpeed:
                return offense.attackSpeed;
            case StatType.Damage:
                return offense.damage;
            case StatType.CritPower:
                return offense.critpower;
            case StatType.CritChance:
                return offense.critchance;
            case StatType.ArmorReduction:
                return offense.armorReduction;

            case StatType.FireDamage:
                return offense.fireDamage;
            case StatType.IceDamage:
                return offense.iceDamage;
            case StatType.LightningDamage:
                return offense.lightningDamage;
            case StatType.PoisonDamage:
                return offense.poisonDamage;
            
            case StatType.Armor:
                return defense.armor;
            case StatType.Evasion:
                return defense.evasion;
            
            case StatType.FireResistance:
                return defense.fireRes;
            case StatType.IceResistance:
                return defense.iceRes;
            case StatType.LightningResistance:
                return defense.lightningRes;
            case StatType.PoisonResistance:
                return defense.poisonRes;
            default:
                Debug.LogError("Stat type not found: " + type);
                return null; // Return null if the stat type is not found
        }
    }

    // This method retrieves a specific stat based on its type as a string.
    [ContextMenu("Update Default Stat Setup")]
    //goes with scriptable object Stat_SetupS0.cs
    // This method applies the default stat setup from the assigned ScriptableObject.
    public void ApplyDefaultStatSetup()
    {
        if (defaultStatSetup == null) // check if the defaultStatSetup is assigned
        {
            Debug.Log("No Default stat setup assigned");
            return;
        }

        resources.maxHealth.SetBaseValue(defaultStatSetup.maxHealth);
        resources.healthRegen.SetBaseValue(defaultStatSetup.healthRegen);
        resources.maxMana.SetBaseValue(defaultStatSetup.maxMana);
        resources.manaRegen.SetBaseValue(defaultStatSetup.manaRegen);

        offense.attackSpeed.SetBaseValue(defaultStatSetup.attackSpeed);
        offense.damage.SetBaseValue(defaultStatSetup.damage);
        offense.critchance.SetBaseValue(defaultStatSetup.critChance);
        offense.critpower.SetBaseValue(defaultStatSetup.critPower);
        offense.armorReduction.SetBaseValue(defaultStatSetup.armorReduction);
        //offense.Stroke.SetBaseValue(defaultStatSetup.Stroke);

        offense.fireDamage.SetBaseValue(defaultStatSetup.fireDamage);
        offense.iceDamage.SetBaseValue(defaultStatSetup.iceDamage);
        offense.lightningDamage.SetBaseValue(defaultStatSetup.lightningDamage);
        offense.poisonDamage.SetBaseValue(defaultStatSetup.poisonDamage);

        defense.armor.SetBaseValue(defaultStatSetup.armor);
        defense.evasion.SetBaseValue(defaultStatSetup.evasion);
        //defense.

        defense.fireRes.SetBaseValue(defaultStatSetup.fireResistance);
        defense.iceRes.SetBaseValue(defaultStatSetup.iceResistance);
        defense.lightningRes.SetBaseValue(defaultStatSetup.lightningResistance);
        defense.poisonRes.SetBaseValue(defaultStatSetup.poisonResistance);
    }
}
