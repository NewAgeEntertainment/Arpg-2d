using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] private float baseValue; // Base value of the stat
    [SerializeField] private List<StatModifier> modifiers = new List<StatModifier>(); // List of modifiers affecting the stat

    private bool needToCalculate = true; // Flag to check if the stat has been modified
    private float finalValue; // Final value after applying modifiers

    public float GetValue()
    {
        if (needToCalculate)
        {
            finalValue = GetFinalValue(); // Recalculate the final value if needed
            needToCalculate = false; // Reset the flag after recalculation
        }

        return finalValue; // Returns the base value of the stat
    }

    public void AddModifier(float value, string source)
    {
        // Adds a new modifier to the stat
        StatModifier modToAdd = new StatModifier(value, source);
        modifiers.Add(modToAdd);
        needToCalculate = true; // Set the flag to true to indicate that the stat needs to be recalculated
    }

    public void RemoveModifier(string source)
    {
        // Removes a modifier from the stat based on its source
        modifiers.RemoveAll(modifier => modifier.source == source);
        needToCalculate = true; // Set the flag to true to indicate that the stat needs to be recalculated
    }

    private float GetFinalValue()
    {
        float finalValue = baseValue; // Start with the base value

        foreach (var modifier in modifiers)
        {
            finalValue = finalValue + modifier.value;
        }

        return finalValue; // Return the final value after applying all modifiers
    }

    // goes with ScriptableObject Stat_SetupS0
    // Sets the base value of the stat
    public void SetBaseValue(float value) => baseValue = value;

}

[Serializable]
public class StatModifier
{
    public float value; // Value of the modifier
    public string source; // Source of the modifier (e.g., item name, buff name)


    public StatModifier(float value, string source)
    {
        this.value = value; // Initialize the value of the modifier
        this.source = source; // Initialize the source of the modifier
    }

}
