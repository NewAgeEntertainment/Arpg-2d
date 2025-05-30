using System;
using UnityEngine;

[Serializable]
public class Stat_OffenseGroup
{
    public Stat attackSpeed; // Attack speed of the entity, affecting how quickly it can perform actions like attacking or casting spells

    // physical damage
    public Stat damage; // Base damage of the entity
    public Stat critpower; // Critical power of the entity, which affects the damage dealt on critical hits
    public Stat critchance; // Critical chance of the entity, which affects the probability of landing a critical hit
    public Stat armorReduction; // Armor reduction, which reduces the target's armor when dealing damage

    // Elemental damage
    public Stat fireDamage; // Base fire damage of the entity
    public Stat iceDamage; // Base ice damage of the entity
    public Stat lightningDamage; // Base lightning damage of the entity
    public Stat poisonDamage; // Base poison damage of the entity
}
