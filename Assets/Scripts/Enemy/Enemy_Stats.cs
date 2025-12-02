using UnityEngine;

public class Enemy_Stats : Entity_Stats
{
    [Header("Enemy Level (cosmetic only)")]
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private bool showLevelInName = true;

    private string originalName;

    // ─────────────────────────────────────────────
    // Per-enemy instance tweaks (Inspector editable)
    // These are *multipliers* applied after the base setup.
    // 1 = no change, 2 = double, 0.5 = half, etc.
    // ─────────────────────────────────────────────
    [Header("Per-Enemy Stat Multipliers")]
    [Tooltip("Multiplies this enemy's max HP and HP regen.")]
    [SerializeField] private float maxHealthMultiplier = 1f;

    [Tooltip("Multiplies this enemy's main damage stat.")]
    [SerializeField] private float damageMultiplier = 1f;

    [Tooltip("Multiplies armor / defenses (optional).")]
    [SerializeField] private float armorMultiplier = 1f;

    [Tooltip("Multiplies elemental damage (fire/ice/lightning/poison).")]
    [SerializeField] private float elementalDamageMultiplier = 1f;

    [Tooltip("Multiplies elemental resistances (fire/ice/lightning/poison).")]
    [SerializeField] private float elementalResMultiplier = 1f;

    protected override void Awake()
    {
        base.Awake();

        originalName = gameObject.name;

        // Just for display – does NOT change stats
        if (showLevelInName)
            gameObject.name = $"Lv{enemyLevel} {originalName}";

        // Apply base data + our per-enemy tweaks
        ApplyDefaultStatSetup();
    }

    public override void ApplyDefaultStatSetup()
    {
        // 1) Base setup from Entity_Stats / StatDataSO
        base.ApplyDefaultStatSetup();

        // 2) Per-enemy multipliers (these are instance-specific)

        // Resources
        resources.maxHealth.MultiplyBaseValue(maxHealthMultiplier);
        resources.healthRegen.MultiplyBaseValue(maxHealthMultiplier);

        // Main damage
        offense.damage.MultiplyBaseValue(damageMultiplier);

        // Crits (optional – comment out if you don’t want to scale them)
        offense.critChance.MultiplyBaseValue(damageMultiplier);
        offense.critPower.MultiplyBaseValue(damageMultiplier);

        // Armor / evasion
        defense.armor.MultiplyBaseValue(armorMultiplier);
        defense.evasion.MultiplyBaseValue(armorMultiplier);

        // Elemental damage
        offense.fireDamage.MultiplyBaseValue(elementalDamageMultiplier);
        offense.iceDamage.MultiplyBaseValue(elementalDamageMultiplier);
        offense.lightningDamage.MultiplyBaseValue(elementalDamageMultiplier);
        offense.poisonDamage.MultiplyBaseValue(elementalDamageMultiplier);

        // Elemental resists
        defense.fireRes.MultiplyBaseValue(elementalResMultiplier);
        defense.iceRes.MultiplyBaseValue(elementalResMultiplier);
        defense.lightningRes.MultiplyBaseValue(elementalResMultiplier);
        defense.poisonRes.MultiplyBaseValue(elementalResMultiplier);
    }

    // Still here in case other code reads it (UI, drops, etc.)
    public int GetEnemyLevel() => enemyLevel;
}
