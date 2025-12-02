using System;
using UnityEngine;

[Serializable]
public class AttackData
{
    public float physicalDamage;
    public float elementalDamage;
    public bool isCrit;
    public ElementType element;

    public ElementalEffectData effectData;

    public AttackData(Entity_Stats entityStats, DamageScaleData scaleData)
    {
        // ---- Guard against NULL inputs ----
        if (entityStats == null)
        {
            Debug.LogError("[AttackData] entityStats is NULL. Returning 0 damage.");
            physicalDamage = 0f;
            elementalDamage = 0f;
            isCrit = false;
            element = ElementType.None;
            effectData = null; // or a default ElementalEffectData if you prefer
            return;
        }

        if (scaleData == null)
        {
            Debug.LogWarning("[AttackData] scaleData is NULL. Using default DamageScaleData.");
            scaleData = new DamageScaleData();   // gives you basePower etc at default values
        }

        // ---- Normal calculation ----
        physicalDamage = entityStats.GetPhysicalDamage(out isCrit, scaleData.physical);   // ✅ fixed spelling
        elementalDamage = entityStats.GetElementalDamage(out element, scaleData.elemental);

        // If ElementalEffectData also expects non-null, it's safe now because scaleData isn't null
        effectData = new ElementalEffectData(entityStats, scaleData);
    }
}
