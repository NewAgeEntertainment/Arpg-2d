using System;
using UnityEngine;

public class Entity_Health : MonoBehaviour, IDamagable // Interface for entities that can take damage
{

    private Entity_VFX entityVfx;
    private Entity entity;

    [SerializeField] protected float MaxHp = 100f; // Maximum health points
    [SerializeField] protected bool isDead;

    [Header("On Damage Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f; // Duration of the knockback effect
    [SerializeField] private Vector2 onDamageKnockback = new Vector2(1.5f, 2.5f);
    [Header("Heavy Damage Knockback")]
    [Range(0,1)]
    [SerializeField] private float heavyDamageThreshold = 5f; // percentage of max hp to trigger heavy knockback
    [SerializeField] private float heavyKnockbackDuration = 5f; // Duration of the heavy knockback effect
    [SerializeField] private Vector2 onHeavyDamageKnockback = new Vector2(7, 7); // Heavy knockback vector

    protected virtual void Awake()
    {
        entity = GetComponent<Entity>(); // Get the Entity component attached to the same GameObject
        entityVfx = GetComponent<Entity_VFX>(); // Get the Entity_VFX component attached to the same GameObject
    }


    

    public virtual void TakeDamage(float damage, Transform damageDealer)
    {
        if (isDead) 
            return; // If the entity is already dead, do nothing

        float duration = CalculateKnockbackDuration(damage); // Calculate the knockback duration based on the damage amount
        Vector2 knockback = CalculateKnockback(damage, damageDealer); // Calculate the knockback vector based on the damage dealer's position

        entityVfx?.PlayOnDamageVfx(); // Play the damage visual effect
        entity?.ReciveKnockback(knockback, duration); // Apply knockback effect
        ReduceHp(damage); // Call the method to reduce health points
    }


    protected void ReduceHp(float damage)
    {
        MaxHp -= damage; // Reduce the health points by the damage amount
        if (MaxHp < 0)
            Die(); // If health points drop below 0, call the Die method
    }

    private void Die()
    {
        isDead = true; // Set the entity as dead
        entity.EntityDeath(); // Call the EntityDeath method from the Entity class
    }

    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        
        // Calculate the knockback direction based on the damage dealer's position  
        int xDirection = transform.position.x > damageDealer.position.x ? 1 : -1;
        int yDirection = transform.position.y > damageDealer.position.y ? 1 : -1;
        
        Vector2 knockback = IsHeavyDamage(damage) ? onDamageKnockback : onHeavyDamageKnockback; // Choose the knockback vector based on heavy damage
        return knockback;
    }

    private float CalculateKnockbackDuration(float damage)
    {
        // Calculate the knockback duration based on the damage amount
        return IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;
    }



    private bool IsHeavyDamage(float damage) => damage / MaxHp > heavyDamageThreshold; // Check if the damage is considered heavy based on the threshold
    // damage / MaxHp is the percentage of max hp that is being taken away.
    // If the damage is greater than the threshold, it is considered heavy damage.

}


