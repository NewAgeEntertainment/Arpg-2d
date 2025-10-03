// Entity_Health.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Health : MonoBehaviour, IDamageable
{
    // ---------------- Events ----------------
    public event Action OnTakingDamage;
    public event Action OnHealthUpdate;

    /// <summary>Raised once when this entity dies.</summary>
    public event Action OnDied;

    /// <summary>Raised whenever this entity transitions from dead -> alive.</summary>
    public event Action OnRevived;

    /// <summary>Optional: same as OnDied, but includes the last damage dealer Transform.</summary>
    public event Action<Transform> OnDiedWithKiller;

    public float MaxHealth => (entityStats != null) ? entityStats.GetMaxHealth() : Mathf.Max(1f, currentHealth);
    public float GetMaxHealth() => MaxHealth;

    // --------------- References ---------------
    private Slider healthBar;
    private Entity entity;
    private Entity_VFX entityVfx;
    private Entity_Stats entityStats;
    private Entity_DropManager dropManager;

    // --------------- EXP Reward ---------------
    [Header("EXP Reward")]
    [SerializeField] private float expReward = 25f;
    private float GetEXPReward() => expReward;

    // --------------- Runtime State ---------------
    private Transform _lastDamageDealer;
    [SerializeField] protected float currentHealth = 0f;
    [SerializeField] protected bool isDead = false;

    // --------------- Health Regen ---------------
    [Header("Health regen")]
    [SerializeField] private float regenInterval = 1f;
    [SerializeField] private bool canRegenerateHealth = true;

    // --------------- Knockback (normal) ---------------
    [Header("On Damage Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float onDamageKnockback = 4f;

    // --------------- Knockback (heavy) ---------------
    [Header("Heavy Damage Knockback")]
    [Tooltip("If ≤ 1, treated as a FRACTION of max HP (e.g., 0.2 = 20%). If > 1, treated as absolute damage.")]
    [SerializeField] private float heavyDamageThreshold = 0.2f;
    [SerializeField] private float heavyKnockbackDuration = 0.35f;
    [SerializeField] private float onHeavyDamageKnockback = 7f;

    // --------------- Unity Lifecycle ---------------
    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        entityVfx = GetComponent<Entity_VFX>();
        entityStats = GetComponent<Entity_Stats>();
        dropManager = GetComponent<Entity_DropManager>();
        healthBar = GetComponentInChildren<Slider>();

        // Initialize health to max if unset.
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        if (currentHealth <= 0f) currentHealth = max;

        OnHealthUpdate += UpdateHealthBar;
        UpdateHealthBar();

        if (regenInterval > 0f)
            InvokeRepeating(nameof(RegenerateHealth), 0f, regenInterval);
    }

    // --------------- IDamageable ---------------
    public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (isDead) return false;
        if (AttackEvaded()) return false;

        _lastDamageDealer = damageDealer;

        // --- Damage calculations (mitigation & resistances) ---
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

        // Knockback (before applying damage so reactions feel immediate).
        TakeKnockback(damageDealer, finalDamage);

        // Apply damage.
        ReduceHealth(finalDamage);

        OnTakingDamage?.Invoke();
        return true;
    }

    // --------------- Public API ---------------
    public void IncreaseHealth(float healAmount)
    {
        if (isDead) return;

        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0f, max);

        OnHealthUpdate?.Invoke();
    }

    public void ReduceHealth(float damage)
    {
        if (isDead) return;

        entityVfx?.PlayOnDamageVfx();

        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, max);

        OnHealthUpdate?.Invoke();

        if (currentHealth <= 0f)
            Die();
    }

    public float GetHealthPercent()
    {
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(currentHealth / max);
    }

    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Sets health (used by save/load). Clamps and notifies. Does NOT call Die().
    /// If value goes above 0 while dead, raises OnRevived.
    /// </summary>
    public void SetCurrentHealth(float value)
    {
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;

        bool wasDead = isDead;
        currentHealth = Mathf.Clamp(value, 0f, max);

        if (wasDead && currentHealth > 0f)
        {
            isDead = false;
            OnHealthUpdate?.Invoke();
            OnRevived?.Invoke();
            return;
        }

        OnHealthUpdate?.Invoke();

        if (!isDead && currentHealth <= 0f)
            Die();
    }

    /// <summary>Convenience to set health as a fraction of max.</summary>
    public void SetHealthToPercent(float percent)
    {
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        SetCurrentHealth(Mathf.Clamp01(percent) * max);
    }

    public void EnableHealthBar(bool enable)
    {
        if (healthBar == null) return;
        var root = healthBar.transform.parent ? healthBar.transform.parent.gameObject : null;
        if (root != null) root.SetActive(enable);
    }

    // -------- Revive helpers (for load/new game, respawns, etc.) --------
    public bool IsDead => isDead;

    public void ForceRevive(float? setHealth = null)
    {
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        isDead = false;

        if (setHealth.HasValue)
            currentHealth = Mathf.Clamp(setHealth.Value, 1f, max);
        else if (currentHealth <= 0f)
            currentHealth = Mathf.Max(1f, max * 0.1f); // bring back with at least some HP

        OnHealthUpdate?.Invoke();
        OnRevived?.Invoke();
    }

    public void ForceReviveToFull()
    {
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        isDead = false;
        currentHealth = Mathf.Max(1f, max);
        OnHealthUpdate?.Invoke();
        OnRevived?.Invoke();
    }

    // --------------- Internals ---------------
    private void RegenerateHealth()
    {
        if (!canRegenerateHealth || isDead || entityStats == null) return;

        float regenAmount = entityStats.resources.healthRegen.GetValue();
        if (regenAmount <= 0f) return;

        IncreaseHealth(regenAmount);
    }

    private bool AttackEvaded()
    {
        if (entityStats == null) return false;
        return UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion();
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null) return;

        var parent = healthBar.transform.parent ? healthBar.transform.parent.gameObject : null;
        if (parent != null && !parent.activeInHierarchy) return;

        healthBar.value = GetHealthPercent();

        // Alternative absolute bar:
        // if (entityStats != null) { healthBar.maxValue = entityStats.GetMaxHealth(); healthBar.value = currentHealth; }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Core death logic.
        entity?.EntityDeath();
        TryGrantEXPToPlayer();
        dropManager?.DropItems();

        // Notify listeners (Quest Machine bridge can subscribe to these).
        OnDied?.Invoke();
        OnDiedWithKiller?.Invoke(_lastDamageDealer);
    }

    private void TryGrantEXPToPlayer()
    {
        if (_lastDamageDealer == null) return;

        var player = _lastDamageDealer.GetComponent<Player>();
        if (player != null)
        {
            float exp = GetEXPReward();
            player.GainEXP(exp);
            Debug.Log($"[Entity_Health] Granted {exp} EXP to Player.");
        }
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
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay) + 0.05f);

        if (p == null || p.health == null) yield break;
        if (p.health.IsDead) yield break;

        p.SetVelocity(0f, 0f);

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
        float max = (entityStats != null) ? entityStats.GetMaxHealth() : 1f;
        if (max <= 0f) return false;

        // Fractional threshold (≤1) vs absolute (>1)
        if (heavyDamageThreshold <= 1f)
            return (damage / max) > heavyDamageThreshold;

        return damage > heavyDamageThreshold;
    }
}
