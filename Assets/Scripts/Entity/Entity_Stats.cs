using UnityEngine;
using UnityEngine.Rendering;

public class Entity_Stats : MonoBehaviour
{
    public Stat maxHealth; // Base maximum health points of the entity
    public Stat_MajorGroup major; // Major stats group containing strength, agility, intelligence, and vitality
    public Stat_OffenseGroup offense; // Offense stats group containing damage, crit power, crit chance, and elemental damages
    public Stat_DefenseGroup defense; // Defense stats group containing armor, evasion, and elemental resistances
    
    
    //  this method calculates the total maxHp we have accorfing to the Vitality stat and the base Health.
    public float GetMaxHealth()
    {
        float baseHp = maxHealth.GetValue();
        float bonusHp = major.vitality.GetValue() * 5; // each point of Vitality gives +5 hp

        return baseHp + bonusHp;

    }

    public float GetEvasion()
    {
        float baseEvasion = defense.evasion.GetValue();
        float bonusEvasion = major.agility.GetValue() * 0.5f; // each point of Agility gives +0.5% evasion

        float totalEvasion = baseEvasion + bonusEvasion;

        // Clamp the evasion value to a maximum cap to prevent excessive evasion
        float evasionCap = 85f; // Maximum evasion cap, can be adjusted as needed

        float finalEvasion = Mathf.Clamp(totalEvasion, 0f, evasionCap); // Clamp the final evasion value between 0 and the evasionCap.

        return finalEvasion;
    }
}
