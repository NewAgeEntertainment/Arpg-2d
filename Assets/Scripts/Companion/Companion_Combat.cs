using UnityEngine;

public class CompanionCombat : Entity_Combat
{
    private Companion companion;
    private float lastAttackTime;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask targetMask;

    private void Start()
    {
        companion = GetComponent<Companion>();

        // Ensure Entity_Stats is set in base
        stats = GetComponent<Entity_Stats>();
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>();
        mana = GetComponent<Entity_Mana>();
    }

    public void AttemptAttack(Transform target)
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        if (target == null)
            return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange)
            return;

        lastAttackTime = Time.time;

        // Play the attack animation trigger (handled via animation event)
        PerformAttack();
    }

    public override Collider2D[] GetDetectedCollider()
    {
        return Physics2D.OverlapCircleAll(attackOrigin.position, attackRange, targetMask);
    }

    public override void PerformAttack()
    {
        var hits = GetDetectedCollider();
        Debug.Log($"[CompanionCombat] Hits: {hits.Length}");

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null)
                continue;

            AttackData attackData = new AttackData(stats, basicAttackScale);
            Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

            float physicalDamage = attackData.physicalDamage;
            float elementalDamage = attackData.elementalDamage;
            ElementType element = attackData.element;

            bool targetGotHit = damageable.TakeDamage(physicalDamage, elementalDamage, element, transform);

            if (element != ElementType.None)
                statusHandler?.ApplyStatusEffect(element, attackData.effectData);

            if (targetGotHit)
            {
                
                vfx?.CreateOnHitVFX(target.transform, attackData.isCrit, element);

                // Optional: mana restore if companion ever uses mana
                if (_entity is Player player)
                    player.mana.RestoreManaOnHitWithScaling(player.Level);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
        }
    }
}
