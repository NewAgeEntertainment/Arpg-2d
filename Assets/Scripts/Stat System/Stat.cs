using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    [SerializeField] private List<StatModifier> modifiers = new List<StatModifier>();

    private bool needToRecalculate = true;
    private float finalValue;

    public float GetValue()
    {
        if (needToRecalculate)
        {
            finalValue = CalculateFinalValue();
            needToRecalculate = false;
        }
        return finalValue;
    }

    public void SetBaseValue(float value)
    {
        baseValue = value;
        needToRecalculate = true;
    }

    public void MultiplyBaseValue(float multiplier)
    {
        baseValue *= multiplier;
        needToRecalculate = true;
    }

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
    public string source;
    public StatModType type;

    public StatModifier(float value, StatModType type, string source)
    {
        this.value = value;
        this.type = type;
        this.source = source;
    }

    // For Unity serialization (required)
    public StatModifier() { }
}

