using UnityEngine;

public class Entity_Stats : MonoBehaviour
{
    public Stat_SetupS0 defaultStatSetup;

    public Stat_ResourceGroup resources;
    public Stat_OffenseGroup offense;
    public Stat_DefenseGroup defense;
    public Stat_MajorGroup major;
    public Stat_SexGroup sex;

    protected virtual void Awake() { }

    public AttackData GetAttackData(DamageScaleData scaleData) => new AttackData(this, scaleData);


    public float GetElementalDamage(out ElementType element, float scaleFactor = 1)
    {
        float fireDamage = offense.fireDamage.GetValue();
        float iceDamage = offense.iceDamage.GetValue();
        float lightningDamage = offense.lightningDamage.GetValue();
        float poisonDamage = offense.poisonDamage.GetValue();
        float bonusElementalDamage = major.intelligence.GetValue();

        float highestDamage = fireDamage;
        element = ElementType.Fire;

        if (iceDamage > highestDamage) { highestDamage = iceDamage; element = ElementType.Ice; }
        if (poisonDamage > highestDamage) { highestDamage = poisonDamage; element = ElementType.Poison; }
        if (lightningDamage > highestDamage) { highestDamage = lightningDamage; element = ElementType.Lightning; }

        if (highestDamage <= 0) { element = ElementType.None; return 0; }

        float weakerElementalDamage = 0;
        if (fireDamage != highestDamage) weakerElementalDamage += fireDamage * 0.5f;
        if (iceDamage != highestDamage) weakerElementalDamage += iceDamage * 0.5f;
        if (lightningDamage != highestDamage) weakerElementalDamage += lightningDamage * 0.5f;
        if (poisonDamage != highestDamage) weakerElementalDamage += poisonDamage * 0.5f;

        float finalDamage = highestDamage + weakerElementalDamage + bonusElementalDamage;
        return Mathf.Floor(finalDamage * scaleFactor);
    }

    public float GetElementalResistance(ElementType element)
    {
        float baseResistance = 0;
        float bonusResistance = major.intelligence.GetValue() * 0.5f;

        switch (element)
        {
            case ElementType.Fire: baseResistance = defense.fireRes.GetValue(); break;
            case ElementType.Ice: baseResistance = defense.iceRes.GetValue(); break;
            case ElementType.Lightning: baseResistance = defense.lightningRes.GetValue(); break;
            case ElementType.Poison: baseResistance = defense.poisonRes.GetValue(); break;
        }

        float resistance = baseResistance + bonusResistance;
        return Mathf.Clamp(resistance, 0, 75f) / 100;
    }

    public float GetPhysicalDamage(out bool isCrit, float scaleFactor = 1)
    {
        float baseDamage = GetBaseDamage();
        float critChance = GetCritChance();
        float critPower = GetCritPower() / 100;

        isCrit = Random.Range(0, 100) < critChance;
        float finalDamage = isCrit ? baseDamage * critPower : baseDamage;

        return Mathf.Floor(finalDamage * scaleFactor);
    }

    public float GetBaseDamage()
    {
        float rawDamage = offense.damage.GetValue();
        float strength = major.strength.GetValue();
        float strBonus = strength * 0.5f;          // 0.5 damage per 1 STR

        return Mathf.Floor(rawDamage + strBonus);
    }

    public float GetCritChance() => Mathf.Floor(offense.critChance.GetValue() + (major.luck.GetValue() * 0.3f));
    public float GetCritPower() => Mathf.Floor(offense.critPower.GetValue() + (major.strength.GetValue() * 0.5f));

    public float GetSexualDamage(out bool isCrit, float scaleFactor = 1)
    {
        float baseSexDamage = GetBaseSexDamage();
        float critChance = GetCritChance();
        float critPower = GetCritPower();

        isCrit = Random.Range(0, 100) < critChance;
        float finalSexDamage = isCrit ? baseSexDamage * critPower : baseSexDamage;
        return Mathf.Floor(finalSexDamage * scaleFactor);
    }

    // OLD
    // public float GetBaseSexDamage() => Mathf.Floor(sex.sexualDamage.GetValue() + sex.stroke.GetValue());

    // NEW
    public float GetBaseSexDamage()
    {
        float rawSexDamage = sex.sexualDamage.GetValue();
        float stroke = sex.stroke.GetValue();
        float strokeBonus = stroke * 0.5f;          // 0.5 sexual damage per 1 Stroke

        return Mathf.Floor(rawSexDamage + strokeBonus);
    }


    public float GetArmorMitigation(float armorReduction)
    {
        float totalArmor = GetBaseArmor();
        float reductionMultiplier = Mathf.Clamp(1 - armorReduction, 0, 1);
        float effectiveArmor = totalArmor * reductionMultiplier;
        float mitigation = effectiveArmor / (effectiveArmor + 100);
        return Mathf.Clamp(mitigation, 0, 0.85f);
    }

    public float GetBaseArmor() => Mathf.Floor(defense.armor.GetValue() * major.vitality.GetValue());

    public float GetResilienceMitigation(float resilienceReduction)
    {
        float totalResilience = GetBaseResilience();
        float reductionMultiplier = Mathf.Clamp(1 - resilienceReduction, 0, 1);
        float effectiveArmor = totalResilience * reductionMultiplier;
        float mitigation = effectiveArmor / (effectiveArmor + 100);
        return Mathf.Clamp(mitigation, 0, 0.85f);
    }

    public float GetBaseResilience() => Mathf.Floor(sex.resilience.GetValue() * sex.sexualRestraint.GetValue());

    public float GetArmorReduction() => offense.armorReduction.GetValue() / 100f;

    public float GetEvasion()
    {
        float baseEvasion = defense.evasion.GetValue();
        float bonusEvasion = major.luck.GetValue() * 0.5f;
        float totalEvasion = baseEvasion + bonusEvasion;
        return Mathf.Clamp(Mathf.Floor(totalEvasion), 0f, 85f);
    }

    public float GetMaxHealth() => Mathf.Floor(resources.maxHealth.GetValue() + major.vitality.GetValue() * 5);
    public float GetMaxArousel() => Mathf.Floor(sex.maxArousal.GetValue() + major.vitality.GetValue() * 5);
    public float GetMaxMana() => Mathf.Floor(resources.maxMana.GetValue() + major.intelligence.GetValue() * 5);

    public Stat GetStatByType(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return resources.maxHealth;
            case StatType.HealthRegen: return resources.healthRegen;
            case StatType.MaxMana: return resources.maxMana;
            case StatType.ManaRegen: return resources.manaRegen;
            case StatType.Strength: return major.strength;
            case StatType.Luck: return major.luck;
            case StatType.Intelligence: return major.intelligence;
            case StatType.Vitality: return major.vitality;
            case StatType.AttackSpeed: return offense.attackSpeed;
            case StatType.Damage: return offense.damage;
            case StatType.CritPower: return offense.critPower;
            case StatType.CritChance: return offense.critChance;
            case StatType.ArmorReduction: return offense.armorReduction;
            case StatType.MaxArousal: return sex.maxArousal;
            case StatType.Stroke: return sex.stroke;
            case StatType.SexualDamage: return sex.sexualDamage;
            case StatType.SexualRestraint: return sex.sexualRestraint;
            case StatType.Resilience: return sex.resilience;
            case StatType.FireDamage: return offense.fireDamage;
            case StatType.IceDamage: return offense.iceDamage;
            case StatType.LightningDamage: return offense.lightningDamage;
            case StatType.PoisonDamage: return offense.poisonDamage;
            case StatType.Defense: return defense.armor;
            case StatType.Evasion: return defense.evasion;
            case StatType.FireResistance: return defense.fireRes;
            case StatType.IceResistance: return defense.iceRes;
            case StatType.LightningResistance: return defense.lightningRes;
            case StatType.PoisonResistance: return defense.poisonRes;
            default:
                Debug.LogWarning($"StatType {type} not implemented yet.");
                return null;
        }
    }

    [ContextMenu("Update Default Stat Setup")]
    public virtual void ApplyDefaultStatSetup()
    {
        if (defaultStatSetup == null)
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
        offense.critChance.SetBaseValue(defaultStatSetup.critChance);
        offense.critPower.SetBaseValue(defaultStatSetup.critPower);
        offense.armorReduction.SetBaseValue(defaultStatSetup.armorReduction);

        sex.stroke.SetBaseValue(defaultStatSetup.stroke);
        sex.resilience.SetBaseValue(defaultStatSetup.resilience);
        sex.sexualDamage.SetBaseValue(defaultStatSetup.sexualDamge);
        sex.sexualRestraint.SetBaseValue(defaultStatSetup.sexualRestraint);
        sex.maxArousal.SetBaseValue(defaultStatSetup.maxArousal);

        offense.fireDamage.SetBaseValue(defaultStatSetup.fireDamage);
        offense.iceDamage.SetBaseValue(defaultStatSetup.iceDamage);
        offense.lightningDamage.SetBaseValue(defaultStatSetup.lightningDamage);
        offense.poisonDamage.SetBaseValue(defaultStatSetup.poisonDamage);

        defense.armor.SetBaseValue(defaultStatSetup.armor);
        defense.evasion.SetBaseValue(defaultStatSetup.evasion);

        defense.fireRes.SetBaseValue(defaultStatSetup.fireResistance);
        defense.iceRes.SetBaseValue(defaultStatSetup.iceResistance);
        defense.lightningRes.SetBaseValue(defaultStatSetup.lightningResistance);
        defense.poisonRes.SetBaseValue(defaultStatSetup.poisonResistance);
    }

    public float GetBlueBarStrokeValue()
    {
        float baseBlueBarFill = 1f;
        float resilienceReduction = 0f;
        float resilience = GetResilienceMitigation(resilienceReduction);
        float reduction = 1f - Mathf.Clamp(resilience, 0f, 50f) / 100f;
        return baseBlueBarFill * reduction;
    }

    public float GetPinkBarStrokeValue()
    {
        bool isCrit;
        float sexDamage = GetSexualDamage(out isCrit);
        return sexDamage;
    }

    public float GetCastFillReductionMultiplier()
    {
        float resilienceReduction = 0f;
        float resilience = GetResilienceMitigation(resilienceReduction);
        float reduction = 1f - Mathf.Clamp(resilience, 0f, 50f) / 100f;
        return Mathf.Clamp(reduction, 0.5f, 1f);
    }

    public float GetResilienceReductionMultiplier()
    {
        float resilienceReduction = 0f;
        float resilience = GetResilienceMitigation(resilienceReduction);
        return 1f - Mathf.Clamp(resilience, 0f, 50f) / 100f;
    }
}
