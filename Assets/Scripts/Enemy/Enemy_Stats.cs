using UnityEngine;

public class Enemy_Stats : Entity_Stats
{
    [Header("Enemy Level Scaling")]
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private float growthRate = 1.1f;
    [SerializeField] private bool applyScaling = true;
    [SerializeField] private bool showLevelInName = true;

    private string originalName;

    protected override void Awake()
    {
        base.Awake();
        originalName = gameObject.name;

        if (showLevelInName)
            gameObject.name = $"Lv{enemyLevel} {originalName}";

        ApplyDefaultStatSetup();
    }

    public override void ApplyDefaultStatSetup()
    {
        base.ApplyDefaultStatSetup();

        if (applyScaling)
            ApplyLevelScaling();
    }

    public void ApplyLevelScaling()
    {
        if (enemyLevel <= 1) return;

        float multiplier = Mathf.Pow(growthRate, enemyLevel - 1);

        resources.maxHealth.MultiplyBaseValue(multiplier);
        resources.healthRegen.MultiplyBaseValue(multiplier);
        offense.damage.MultiplyBaseValue(multiplier);
        offense.critPower.MultiplyBaseValue(multiplier);
        offense.critChance.MultiplyBaseValue(multiplier);
        defense.armor.MultiplyBaseValue(multiplier);
        defense.evasion.MultiplyBaseValue(multiplier);
        major.strength.MultiplyBaseValue(multiplier);
        major.vitality.MultiplyBaseValue(multiplier);
        major.intelligence.MultiplyBaseValue(multiplier);
        major.luck.MultiplyBaseValue(multiplier);
        sex.sexualDamage.MultiplyBaseValue(multiplier);
        sex.stroke.MultiplyBaseValue(multiplier);
        sex.resilience.MultiplyBaseValue(multiplier);

        offense.fireDamage.MultiplyBaseValue(multiplier);
        offense.iceDamage.MultiplyBaseValue(multiplier);
        offense.lightningDamage.MultiplyBaseValue(multiplier);
        offense.poisonDamage.MultiplyBaseValue(multiplier);

        defense.fireRes.MultiplyBaseValue(multiplier);
        defense.iceRes.MultiplyBaseValue(multiplier);
        defense.lightningRes.MultiplyBaseValue(multiplier);
        defense.poisonRes.MultiplyBaseValue(multiplier);

        sex.maxArousal.MultiplyBaseValue(multiplier);
        sex.sexualRestraint.MultiplyBaseValue(multiplier);
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Scaling Now")]
    private void ApplyScalingNow()
    {
        ApplyDefaultStatSetup();
        Debug.Log("[Enemy_Stats] Manual scaling applied in Editor.");
    }
#endif

    public int GetEnemyLevel() => enemyLevel;
}
