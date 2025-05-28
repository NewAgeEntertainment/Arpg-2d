using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Health : MonoBehaviour, IDamagable // Interface for entities that can take damage
{
    private Slider healthBar; // Reference to the health bar UI element
    private Entity_VFX entityVfx;
    private Entity entity;
    private Entity_Stats stats; // Reference to the Entity_Stats component for health calculations

    [SerializeField] protected float currentHp; // Current health points, initialized to maximum health
    [SerializeField] protected bool isDead;

    [Header("On Damage Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f; // Duration of the knockback effect
    [SerializeField] private float onDamageKnockback;
    [Header("Heavy Damage Knockback")]
    [SerializeField] private float heavyDamageThreshold = 5f; // percentage of max hp to trigger heavy knockback
    [SerializeField] private float heavyKnockbackDuration = 5f; // Duration of the heavy knockback effect
    [SerializeField] private Vector2 onHeavyDamageKnockback = new Vector2(7, 7); // Heavy knockback vector


    protected virtual void Awake()
    {
        entity = GetComponent<Entity>(); // Get the Entity component attached to the same GameObject
        entityVfx = GetComponent<Entity_VFX>(); // Get the Entity_VFX component attached to the same GameObject
        stats = GetComponent<Entity_Stats>(); // Get the Entity_Stats component attached to the same GameObject
        healthBar = GetComponentInChildren<Slider>(); // Get the Slider component for the health bar UI

        currentHp = stats.GetMaxHealth(); // Initialize current health points to maximum health
        updateHealthBar(); // Update the health bar UI to reflect the initial health points

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

        float mitigation = stats.GetArmorMitigation(armorReduction); // Get the armor mitigation value from the Entity_Stats component
        float finalDamage = damage * (1 - mitigation); // Calculate the final damage after applying armor mitigation

        float resistance = stats.GetElementalResistance(element);
        float elementalDamageTaken = elementalDamage * (1 - resistance);

        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer); // Calculate the knockback vector based on the damage dealer's position
        float duration = CalculateKnockbackDuration(finalDamage); // Calculate the knockback duration based on the damage amount

        entityVfx?.PlayOnDamageVfx(); // Play the damage visual effect
        entity?.ReciveKnockback(knockback, duration); // Apply knockback effect
        ReduceHp(finalDamage); // Call the method to reduce health points
        Debug.Log("Elemental damage Taken: " + elementalDamage + " - element:" + element);

        return true; // Return true to indicate that damage was successfully applied
    }

    private bool AttackEvaded()
    {
        // Placeholder for attack evasion logic, currently always returns false
        // This can be expanded to include actual evasion mechanics in the future
        return Random.Range(0, 100) < stats.GetEvasion(); // Randomly determine if the attack is evaded based on the entity's evasion stat
    }


    protected void ReduceHp(float damage)
    {

        currentHp -= damage; // Reduce the health points by the damage amount
        updateHealthBar(); // Update the health bar UI to reflect the new health points

        if (currentHp < 0)
            Die(); // If health points drop below 0, call the Die method
    }

    private void Die()
    {
        isDead = true; // Set the entity as dead
        entity.EntityDeath(); // Call the EntityDeath method from the Entity class
    }

    private void updateHealthBar()
    {
        if (healthBar == null) 
            return; // If the health bar is not assigned, do nothing
        
        healthBar.value = currentHp / stats.GetMaxHealth(); // Update the health bar UI based on the current health points
    }

    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        // Calculate the knockback direction based on the damage dealer's position  
        int xDirection = transform.position.x > damageDealer.position.x ? 1 : -1;
        int yDirection = transform.position.y > damageDealer.position.y ? 1 : -1;

        // Choose the knockback vector based on heavy damage  
        Vector2 knockback = IsHeavyDamage(damage) ? onHeavyDamageKnockback : new Vector2(onDamageKnockback * xDirection, onDamageKnockback * yDirection);
        return knockback;
    }

    private float CalculateKnockbackDuration(float damage)
    {
        // Calculate the knockback duration based on the damage amount
        return IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;
    }



    private bool IsHeavyDamage(float damage) => damage / stats.GetMaxHealth() > heavyDamageThreshold; // Check if the damage is considered heavy based on the threshold
    // damage / MaxHp is the percentage of max hp that is being taken away.
    // If the damage is greater than the threshold, it is considered heavy damage.

}


