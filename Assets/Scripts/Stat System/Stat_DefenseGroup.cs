using System;
using UnityEngine;

[Serializable]
public class Stat_DefenseGroup
{
    // physical defense
    public Stat armor; // Base armor of the entity, which reduces physical damage taken
    public Stat evasion; // Evasion of the entity, which affects the probability of dodging attacks
    

    // Elemental Resistance;
    public Stat fireRes; // Base fire resistance of the entity, which reduces fire damage taken
    public Stat iceRes; // Base ice resistance of the entity, which reduces ice damage taken
    public Stat lightningRes; // Base lightning resistance of the entity, which reduces lightning damage taken
    public Stat poisonRes; // Base poison resistance of the entity, which reduces poison damage taken

    
}
