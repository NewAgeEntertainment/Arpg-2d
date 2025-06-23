using UnityEditor;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Health : MonoBehaviour, IDamageable // Interface for entities that can take damage
{
    public event Action OnTakingDamage;

    private Slider healthBar; // Reference to the health bar UI element
    private Entity entity;
    private Entity_VFX entityVfx;
    private Entity_Stats entityStats; // Reference to the Entity_Stats component for health calculations

    [SerializeField] protected float currentHealth; // Current health points, initialized to maximum health
    [SerializeField] protected bool isDead;

    [Header("Health regen")]
    [SerializeField] private float regenInterval = 1f; // Amount of health to regenerate per second
    [SerializeField] private bool canRegenerateHealth = true; // Flag to enable or disable health regeneration

    [Header("On Damage Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f; // Duration of the knockback effect
    [SerializeField] private float onDamageKnockback;
    [Header("Heavy Damage Knockback")]
    [SerializeField] private float heavyDamageThreshold = 5f; // percentage of max hp to trigger heavy knockback
    [SerializeField] private float heavyKnockbackDuration = 5f; // Duration of the heavy knockback effect
    [SerializeField] private float onHeavyDamageKnockback; // Heavy knockback vector


    protected virtual void Awake()
    {
        entity = GetComponent<Entity>(); // Get the Entity component attached to the same GameObject
        entityVfx = GetComponent<Entity_VFX>(); // Get the Entity_VFX component attached to the same GameObject
        entityStats = GetComponent<Entity_Stats>(); // Get the Entity_Stats component attached to the same GameObject
        healthBar = GetComponentInChildren<Slider>(); // Get the Slider component for the health bar UI

        currentHealth = entityStats.GetMaxHealth(); // Initialize current health points to maximum health
        UpdateHealthBar(); // Update the health bar UI to reflect the initial health points

        InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval); // Start the health regeneration process at regular intervals

    }


    // bool Method most retun true or false
    // / Method to apply damage to the entity, including knockback and health reduction
    public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (isDead)
            return false; // If the entity is already dead, do nothing

        if (AttackEvaded()) // Check if the attack was evaded
        {
            Debug.Log("Attack Evaded!"); // Log the evasion for debugging purposes
            return false; // If evaded, do not apply damage or knockback
        }

        Entity_Stats attackerStats = damageDealer.GetComponent<Entity_Stats>(); // Get the Entity_Stats component from the damage dealer
        float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0; // Get the armor reduction value from the attacker's stats, if available

        float mitigation = entityStats.GetArmorMitigation(armorReduction); // Get the armor mitigation value from the Entity_Stats component
        float physicalDamageTaken = damage * (1 - mitigation); // Calculate the final damage after applying armor mitigation

        float resistance = entityStats.GetElementalResistance(element);
        float elementalDamageTaken = elementalDamage * (1 - resistance);
        
        TakeKnockback(damageDealer, physicalDamageTaken);
        ReduceHealth(physicalDamageTaken + elementalDamageTaken); // Call the method to reduce health points

        OnTakingDamage?.Invoke(); // Invoke the event to notify that damage has been taken
        return true; // Return true to indicate that damage was successfully applied
    }

    

    private bool AttackEvaded()
    {
        // Placeholder for attack evasion logic, currently always returns false
        // This can be expanded to include actual evasion mechanics in the future
        return UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion(); // Randomly determine if the attack is evaded based on the entity's evasion stat
    }

    private void RegenerateHealth()
    {
        if (canRegenerateHealth == false)
            return; // If health regeneration is disabled, do nothing

        float regenAmount = entityStats.resources.healthRegen.GetValue(); // Get the health regeneration amount from the Entity_Stats component
        IncreaseHealth(regenAmount); // Call the method to increase health points by the regeneration amount
    }

    public void IncreaseHealth(float healAmount)
    {
        if (isDead)
            return;

        float newHealth = currentHealth + healAmount; // Calculate the new health points after healing
        float maxHealth = entityStats.GetMaxHealth(); // Get the maximum health points from the Entity_Stats component

        // Ensure the new health does not exceed the maximum health
        currentHealth = Mathf.Min(newHealth, maxHealth); // Set the current health to the minimum of new health and maximum health
        UpdateHealthBar(); // Update the health bar UI to reflect the new health points
        
        //if (newHealth > maxHealth) other version
        //    currentHealth = maxHealth; // If the new health exceeds maximum health, set it to maximum health
        //else
        //    currentHealth = newHealth; // Otherwise, set the current health to the new health points

    }


    public void ReduceHealth(float damage)
    {

        entityVfx?.PlayOnDamageVfx(); // Play the damage visual effect
        currentHealth = currentHealth - damage; // Reduce the health points by the damage amount
        UpdateHealthBar(); // Update the health bar UI to reflect the new health points

        if (currentHealth < 0)
            Die(); // If health points drop below 0, call the Die method
    }

    private void Die()
    {
        isDead = true; // Set the entity as dead
        entity.EntityDeath(); // Call the EntityDeath method from the Entity class
    }

    public float GetHealthPercent() => currentHealth / entityStats.GetMaxHealth();

    public void SetHealthToPercent(float percent)
    {
        currentHealth = entityStats.GetMaxHealth() * Mathf.Clamp01(percent);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null) 
            return; // If the health bar is not assigned, do nothing
        
        healthBar.value = currentHealth / entityStats.GetMaxHealth(); // Update the health bar UI based on the current health points
    }

    private void TakeKnockback(Transform damageDealer, float finalDamage)
    {
        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer); // Calculate the knockback vector based on the damage dealer's position
        float duration = CalculateKnockbackDuration(finalDamage); // Calculate the knockback duration based on the damage amount

        entity?.ReciveKnockback(knockback, duration); // Apply knockback effect
    }

    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        // Calculate the knockback direction based on the damage dealer's position  
        int xDirection = transform.position.x > damageDealer.position.x ? 1 : -1;
        int yDirection = transform.position.y > damageDealer.position.y ? 1 : -1;

        // Choose the knockback vector based on heavy damage  
        Vector2 knockback = IsHeavyDamage(damage)
            ? new Vector2(onHeavyDamageKnockback, onHeavyDamageKnockback) : new Vector2(onDamageKnockback * xDirection, onDamageKnockback * yDirection);

        return knockback;
    }

    private float CalculateKnockbackDuration(float damage)
    {
        

        // Calculate the knockback duration based on the damage amount
        return IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;
    }



    private bool IsHeavyDamage(float damage) => damage / entityStats.GetMaxHealth() > heavyDamageThreshold; // Check if the damage is considered heavy based on the threshold
    // damage / MaxHp is the percentage of max hp that is being taken away.
    // If the damage is greater than the threshold, it is considered heavy damage.

}


