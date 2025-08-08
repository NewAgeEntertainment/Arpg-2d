using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    [SerializeField] private List<StatModifier> modifiers = new();

    private bool needToRecalculate = true;
    private float finalValue;

    // Return the final stat value after applying modifiers
    public float GetValue()
    {
        if (needToRecalculate)
        {
            finalValue = CalculateFinalValue();
            needToRecalculate = false;
        }

        return finalValue;
    }

    // Base value control
    public void SetBaseValue(float value)
    {
        baseValue = value;
        needToRecalculate = true;
    }

    public float BaseValue => baseValue;

    public void MultiplyBaseValue(float multiplier)
    {
        baseValue *= multiplier;
        needToRecalculate = true;
    }

    // Modifier control
    public void AddModifier(float value, StatModType type, string source)
    {
        modifiers.Add(new StatModifier(value, type, source));
        needToRecalculate = true;
    }

    public void RemoveModifier(string source)
    {
        modifiers.RemoveAll(m => m.source == source);
        needToRecalculate = true;
    }

    public void ClearAllModifiers()
    {
        modifiers.Clear();
        needToRecalculate = true;
    }

    public void AddModifiers(List<StatModifier> modifiersToAdd)
    {
        if (modifiersToAdd == null || modifiersToAdd.Count == 0)
            return;

        modifiers.AddRange(modifiersToAdd);
        needToRecalculate = true;
    }

    public List<StatModifier> Modifiers => modifiers;

    // Final value calculation logic
    private float CalculateFinalValue()
    {
        float final = baseValue;
        float sumPercentAdd = 0f;
        float sumPercentMult = 1f;

        foreach (var mod in modifiers)
        {
            switch (mod.type)
            {
                case StatModType.Flat:
                    final += mod.value;
                    break;
                case StatModType.PercentAdd:
                    sumPercentAdd += mod.value;
                    break;
                case StatModType.PercentMult:
                    sumPercentMult *= 1 + mod.value;
                    break;
            }
        }

        final *= 1 + sumPercentAdd;
        final *= sumPercentMult;

        return final;
    }
}

[Serializable]
public class StatModifier
{
    public float value;
    public StatModType type;
    public string source;

    public StatModifier(float value, StatModType type, string source)
    {
        this.value = value;
        this.type = type;
        this.source = source;
    }

    // Needed for Unity serialization
    public StatModifier() { }
}

//public enum StatModType
//{
//    Flat,
//    PercentAdd,
//    PercentMult
//}
