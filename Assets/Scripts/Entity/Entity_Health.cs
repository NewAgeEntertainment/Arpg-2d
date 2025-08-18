// Assets/Scripts/Entity/Entity_Health.cs
using System;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Health : MonoBehaviour, IDamageable
{
    public event Action OnTakingDamage;
    public event Action OnHealthUpdate;

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
    [SerializeField] private float onDamageKnockback;

    [Header("Heavy Damage Knockback")]
    [SerializeField] private float heavyDamageThreshold = 5f;
    [SerializeField] private float heavyKnockbackDuration = 5f;
    [SerializeField] private float onHeavyDamageKnockback;

    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        entityVfx = GetComponent<Entity_VFX>();
        entityStats = GetComponent<Entity_Stats>();
        healthBar = GetComponentInChildren<Slider>();
        dropManager = GetComponent<Entity_DropManager>();

        if (currentHealth <= 0) currentHealth = entityStats.GetMaxHealth();

        OnHealthUpdate += UpdateHealthBar;
        UpdateHealthBar();

        InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval);
    }

    public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (isDead) return false;
        if (AttackEvaded()) return false;

        _lastDamageDealer = damageDealer;

        Entity_Stats attackerStats = damageDealer.GetComponent<Entity_Stats>();
        float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0;
        float mitigation = entityStats.GetArmorMitigation(armorReduction);
        float physicalDamageTaken = damage * (1 - mitigation);

        float resistance = entityStats.GetElementalResistance(element);
        float elementalDamageTaken = elementalDamage * (1 - resistance);

        TakeKnockback(damageDealer, physicalDamageTaken);
        ReduceHealth(physicalDamageTaken + elementalDamageTaken);

        OnTakingDamage?.Invoke();
        return true;
    }

    private bool AttackEvaded()
        => UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion();

    private void RegenerateHealth()
    {
        if (!canRegenerateHealth) return;
        float regenAmount = entityStats.resources.healthRegen.GetValue();
        IncreaseHealth(regenAmount);
    }

    public void IncreaseHealth(float healAmount)
    {
        if (isDead) return;

        float newHealth = currentHealth + healAmount;
        float maxHealth = entityStats.GetMaxHealth();
        currentHealth = Mathf.Min(newHealth, maxHealth);
        OnHealthUpdate?.Invoke();
    }

    public void ReduceHealth(float damage)
    {
        entityVfx?.PlayOnDamageVfx();
        currentHealth = currentHealth - damage;
        OnHealthUpdate?.Invoke();

        if (currentHealth < 0) Die();
    }

    private void Die()
    {
        isDead = true;
        entity.EntityDeath();

        TryGrantEXPToPlayer();
        dropManager?.DropItems();
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

    public float GetHealthPercent() => currentHealth / entityStats.GetMaxHealth();

    public void SetHealthToPercent(float percent)
    {
        currentHealth = entityStats.GetMaxHealth() * Mathf.Clamp01(percent);
        OnHealthUpdate?.Invoke();
    }

    public float GetCurrentHealth() => currentHealth;

    private void UpdateHealthBar()
    {
        if (healthBar == null || !healthBar.transform.parent.gameObject.activeSelf) return;
        healthBar.value = currentHealth / entityStats.GetMaxHealth();
    }

    public void EnableHealthBar(bool enable) => healthBar?.transform.parent.gameObject.SetActive(enable);

    private void TakeKnockback(Transform damageDealer, float finalDamage)
    {
        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer);
        float duration = CalculateKnockbackDuration(finalDamage);
        entity?.ReciveKnockback(knockback, duration);
    }

    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        int xDirection = transform.position.x > damageDealer.position.x ? 1 : -1;
        int yDirection = transform.position.y > damageDealer.position.y ? 1 : -1;

        Vector2 knockback = IsHeavyDamage(damage)
            ? new Vector2(onHeavyDamageKnockback, onHeavyDamageKnockback)
            : new Vector2(onDamageKnockback * xDirection, onDamageKnockback * yDirection);

        return knockback;
    }

    public void SetCurrentHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, entityStats.GetMaxHealth());
        OnHealthUpdate?.Invoke();
    }

    private float CalculateKnockbackDuration(float damage)
        => IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;

    private bool IsHeavyDamage(float damage)
        => damage / entityStats.GetMaxHealth() > heavyDamageThreshold;
}
