// Entity_Health.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Health : MonoBehaviour, IDamageable
{
    public event Action OnTakingDamage;
    public event Action OnHealthUpdate;
    public event System.Action Died;

    private Slider healthBar;
    private Entity entity;
    private Entity_VFX entityVfx;
    private Entity_Stats entityStats;
    private Entity_DropManager dropManager;

    [Header("EXP Reward")]
    [SerializeField] private float expReward = 25f;
    private float GetEXPReward() => expReward;

    private Transform _lastDamageDealer;
    private bool miniHealthBarActive;

    [SerializeField] protected float currentHealth;
    [SerializeField] protected bool isDead;

    [Header("Health regen")]
    [SerializeField] private float regenInterval = 1f;
    [SerializeField] private bool canRegenerateHealth = true;

    [Header("On Damage Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float onDamageKnockback = 4f;

    [Header("Heavy Damage Knockback")]
    [Tooltip("If ≤ 1, treated as a FRACTION of max HP (e.g., 0.2 = 20%). If > 1, treated as absolute damage.")]
    [SerializeField] private float heavyDamageThreshold = 0.2f;
    [SerializeField] private float heavyKnockbackDuration = 0.35f;
    [SerializeField] private float onHeavyDamageKnockback = 7f;

    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        entityVfx = GetComponent<Entity_VFX>();
        entityStats = GetComponent<Entity_Stats>();
        healthBar = GetComponentInChildren<Slider>();
        dropManager = GetComponent<Entity_DropManager>();

        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        if (currentHealth <= 0f) currentHealth = max;

        OnHealthUpdate += UpdateHealthBar;
        UpdateHealthBar();

        if (regenInterval > 0f)
            InvokeRepeating(nameof(RegenerateHealth), 0f, regenInterval);
    }

    public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (isDead) return false;
        if (AttackEvaded()) return false;

        _lastDamageDealer = damageDealer;

        // Mitigation & resistances
        float physicalDamageTaken = damage;
        float elementalDamageTaken = elementalDamage;

        if (entityStats != null)
        {
            var attackerStats = damageDealer ? damageDealer.GetComponent<Entity_Stats>() : null;
            float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0f;
            float mitigation = Mathf.Clamp01(entityStats.GetArmorMitigation(armorReduction));
            physicalDamageTaken = damage * (1f - mitigation);

            float resistance = Mathf.Clamp01(entityStats.GetElementalResistance(element));
            elementalDamageTaken = elementalDamage * (1f - resistance);
        }

        float finalDamage = Mathf.Max(0f, physicalDamageTaken + elementalDamageTaken);

        // Knockback before damage application
        TakeKnockback(damageDealer, finalDamage);

        ReduceHealth(finalDamage);

        OnTakingDamage?.Invoke();
        return true;
    }

    private bool AttackEvaded()
    {
        if (entityStats == null) return false;
        return UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion();
    }

    private void RegenerateHealth()
    {
        if (!canRegenerateHealth || isDead || entityStats == null) return;
        float regenAmount = entityStats.resources.healthRegen.GetValue();
        if (regenAmount <= 0f) return;
        IncreaseHealth(regenAmount);
    }

    public void IncreaseHealth(float healAmount)
    {
        if (isDead) return;

        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0f, max);

        OnHealthUpdate?.Invoke();
    }

    public void ReduceHealth(float damage)
    {
        if (isDead) return;

        entityVfx?.PlayOnDamageVfx();

        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, max);

        OnHealthUpdate?.Invoke();

        if (currentHealth <= 0f)
            Die();
    }

    // Inside Entity_Health
    public event System.Action OnDied;

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        entity?.EntityDeath();
        TryGrantEXPToPlayer();
        dropManager?.DropItems();

        OnDied?.Invoke(); // <-- add this line
    }


    private void TryGrantEXPToPlayer()
    {
        if (_lastDamageDealer == null) return;

        Player player = _lastDamageDealer.GetComponent<Player>();
        if (player != null)
        {
            float exp = GetEXPReward();
            player.GainEXP(exp);
            Debug.Log($"[Entity_Health] Granted {exp} EXP to Player.");
        }
    }

    public float GetHealthPercent()
    {
        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(currentHealth / max);
    }

    public void SetHealthToPercent(float percent)
    {
        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Clamp01(percent) * max;
        OnHealthUpdate?.Invoke();

        if (!isDead && currentHealth <= 0f)
            Die();
    }

    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Set health directly (used by save/load). Clamps and notifies. Does NOT call Die().
    /// If health becomes > 0, clears isDead flag.
    /// </summary>
    public void SetCurrentHealth(float value)
    {
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Clamp(value, 0f, max);

        if (isDead && currentHealth > 0f) isDead = false;

        OnHealthUpdate?.Invoke();
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null) return;

        var parent = healthBar.transform.parent ? healthBar.transform.parent.gameObject : null;
        if (parent != null && !parent.activeInHierarchy) return;

        healthBar.value = GetHealthPercent();
        // Absolute bar alternative:
        // if (entityStats != null) { healthBar.maxValue = entityStats.GetMaxHealth(); healthBar.value = currentHealth; }
    }

    public void EnableHealthBar(bool enable)
    {
        if (healthBar == null) return;
        var root = healthBar.transform.parent ? healthBar.transform.parent.gameObject : null;
        if (root != null) root.SetActive(enable);
    }

    private void TakeKnockback(Transform damageDealer, float finalDamage)
    {
        if (entity == null || damageDealer == null) return;

        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer);
        float duration = CalculateKnockbackDuration(finalDamage);
        entity.ReciveKnockback(knockback, duration);

        // Safety: if this is the Player, ensure we restore control after the knockback.
        var player = entity as Player;
        if (player != null)
            StartCoroutine(RestorePlayerControlAfter(duration, player));
    }

    private IEnumerator RestorePlayerControlAfter(float delay, Player p)
    {
        // small cushion
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay) + 0.05f);

        if (p == null || p.health == null) yield break;
        if (p.health.IsDead) yield break; // don't override legitimate death

        // If something left the player in a non-controllable state, bring them back.
        p.SetVelocity(0f, 0f);

        // If state machine is null or in an odd state, go idle.
        if (p.stateMachine.currentState == null || p.stateMachine.currentState == p.basicAttackState)
            p.stateMachine.ChangeState(p.idleState);
    }

    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        int xDir = transform.position.x > damageDealer.position.x ? 1 : -1;
        int yDir = transform.position.y > damageDealer.position.y ? 1 : -1;

        bool heavy = IsHeavyDamage(damage);
        if (heavy)
            return new Vector2(onHeavyDamageKnockback * xDir, onHeavyDamageKnockback * yDir);

        return new Vector2(onDamageKnockback * xDir, onDamageKnockback * yDir);
    }

    private float CalculateKnockbackDuration(float damage)
        => IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;

    private bool IsHeavyDamage(float damage)
    {
        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        if (max <= 0f) return false;

        // Fractional threshold (≤1) vs absolute (>1)
        if (heavyDamageThreshold <= 1f)
            return (damage / max) > heavyDamageThreshold;

        return damage > heavyDamageThreshold;
    }

    // -------- Revive helpers (for load/new game) --------
    public bool IsDead => isDead;

    public void ForceRevive(float? setHealth = null)
    {
        isDead = false;

        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        if (setHealth.HasValue)
            currentHealth = Mathf.Clamp(setHealth.Value, 1f, max);
        else if (currentHealth <= 0f)
            currentHealth = 1f;

        OnHealthUpdate?.Invoke();
    }

    public void ForceReviveToFull()
    {
        isDead = false;
        float max = entityStats != null ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Max(1f, max);
        OnHealthUpdate?.Invoke();
    }
}
