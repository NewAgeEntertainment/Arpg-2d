using UnityEngine;

public class SkillObject_SwordSpin : SkillObject_Sword
{
    private int maxDistance;
    private float attacksPerSecond;
    private float attackTimer;

    // Fix: Adjusted access modifier to match the base class method
    private void SetupSword(Skill_Sword swordManager)
    {

        anim?.SetTrigger("spin");

        maxDistance = swordManager.maxDistance;
        attacksPerSecond = swordManager.attacksPerSecond;

        Invoke(nameof(GetSwordBackToPlayer), swordManager.maxSpinDuration);
    }

    protected override void Update()
    {
        HandleAttack();
        HandleStopping();
        HandleComeback();
    }

    private void HandleStopping()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > maxDistance && rb.simulated == true)
            rb.simulated = false;
    }

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer < 0)
        {
            DamageEnemiesInRadius(transform, 1);
            attackTimer = 1 / attacksPerSecond;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        rb.simulated = false;
    }
}

