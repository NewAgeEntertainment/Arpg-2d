using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Stats : Entity_Stats
{
    public float CurrentEXP { get; private set; } = 0;
    public int CurrentLevel { get; private set; } = 1;

    private const float BASE_EXP_REQUIREMENT = 100f;
    private const float EXP_GROWTH_RATE = 1.5f;

    private List<string> activeBuff = new List<string>();
    private Inventory_Player inventory;

    protected override void Awake()
    {
        base.Awake();
        inventory = GetComponent<Inventory_Player>();
    }

    public void AddEXP(float amount)
    {
        CurrentEXP += amount;
        Debug.Log($"[Player_Stats] Gained EXP: {amount} | Total EXP: {CurrentEXP}");

        while (CurrentEXP >= GetNextLevelRequirement())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        CurrentEXP -= GetNextLevelRequirement();
        CurrentLevel++;
        Debug.Log($"[Player_Stats] Leveled Up! New Level: {CurrentLevel}");

        // Optionally notify UI or other systems of level up
    }

    public float GetNextLevelRequirement()
    {
        return BASE_EXP_REQUIREMENT * Mathf.Pow(EXP_GROWTH_RATE, CurrentLevel - 1);
    }

    public bool CanApplyBuffOf(string source)
    {
        return activeBuff.Contains(source) == false;
    }

    public void ApplyBuff(BuffEffectData[] buffToApply, float duration, string source)
    {
        StartCoroutine(buffCo(buffToApply, duration, source));
    }

    private IEnumerator buffCo(BuffEffectData[] buffToApply, float duration, string source)
    {
        activeBuff.Add(source);

        foreach (var buff in buffToApply)
        {
            GetStatByType(buff.type).AddModifier(buff.value, source);
        }

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffToApply)
        {
            GetStatByType(buff.type).RemoveModifier(source);
        }

        inventory.NotifyInventoryChanged();
        activeBuff.Remove(source);
    }

    public float GetStatValue(StatType type)
    {
        return GetStatByType(type).GetValue();
    }
}
