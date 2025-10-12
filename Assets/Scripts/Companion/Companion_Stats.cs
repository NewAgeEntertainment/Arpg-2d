using System;
using UnityEngine;

public class Companion_Stats : Entity_Stats
{
    public event Action<float, float> OnExpChanged;

    [SerializeField] private float currentEXP = 0f;
    [SerializeField] private float baseNextLevel = 100f;
    [SerializeField] private float nextLevelGrowth = 1.25f;
    [SerializeField] private int currentLevel = 1;

    public float CurrentEXP => currentEXP;
    public int CurrentLevel => currentLevel;

    public float GetNextLevelRequirement()
    {
        // Example curve (replace with yours)
        return baseNextLevel * Mathf.Pow(nextLevelGrowth, currentLevel - 1);
    }

    public float GetExpPercent()
    {
        var next = Mathf.Max(0.0001f, GetNextLevelRequirement());
        return Mathf.Clamp01(CurrentEXP / next);
    }

    public void GainEXP(float amount)
    {
        if (amount <= 0f) return;

        currentEXP += amount;

        // simple level-up loop
        while (currentEXP >= GetNextLevelRequirement())
        {
            currentEXP -= GetNextLevelRequirement();
            currentLevel++;
            // TODO: call your level-up logic here if you have one
        }

        OnExpChanged?.Invoke(CurrentEXP, GetNextLevelRequirement());
    }
}
