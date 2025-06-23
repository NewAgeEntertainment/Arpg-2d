using System;
using UnityEngine;


[Serializable]
public class ElementalEffectData
{
    public float chillDuration;
    public float chillSlowMultiplier;

    public float burnDuration;
    public float totalBurnDamage;

    public float poisonDuration;
    public float totalPoisonDamage;

    public float shockDuration;
    public float shockDamage;
    public float shockCharge;

    public ElementalEffectData(Entity_Stats entityStats, DamageScaleData damageScale)
    {
        chillDuration = damageScale.chillDuration;
        chillSlowMultiplier = damageScale.chillSlowMulitplier;

        burnDuration = damageScale.burnDuratin;
        totalBurnDamage = entityStats.offense.fireDamage.GetValue() * damageScale.burnDamageScale;

        poisonDuration = damageScale.poisonDuration;
        totalPoisonDamage = entityStats.offense.poisonDamage.GetValue() * damageScale.poisonDamageScale;

        shockDuration = damageScale.shockDuration;
        shockDamage = entityStats.offense.lightningDamage.GetValue() * damageScale.shockDamageScale;
        shockCharge = damageScale.shockCharge;
    }
}
    
