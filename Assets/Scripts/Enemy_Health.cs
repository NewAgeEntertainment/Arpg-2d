using UnityEngine;

public class Enemy_Health : Entity_Health
{

    private Enemy enemy => GetComponent<Enemy>();

    [Header("On Damage Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f; // Duration of the knockback effect
    [SerializeField] private Vector2 onDamageKnockback = new Vector2(1.5f, 2.5f);

    public override void TakeDamage(float damage, Transform damageDealer)
    {
        base.TakeDamage(damage, damageDealer); // Call the base class method to handle damage
        
        if (isDead)
            return; // If the entity is already dead, do nothing

        if (damageDealer.CompareTag("Player"));
            //enemy.TryEnterBattleState(damageDealer); // If the damage dealer is the player, enter battle state

    }
}

