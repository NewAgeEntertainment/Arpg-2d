using UnityEngine;

public class SkillObject_Sword : SkillObject_Base
{
    private Transform target;
    private float speed;

    public Transform playerTransform { get; private set; }

    private Skill_Sword swordManager;

    private int maxDistance;
    private float attacksPerSecond;
    private float attackTimer;

    private bool isLaunchedForward;
    private Vector2 launchDirection;

    [Header("Sword Hit")]
    [SerializeField] private float hitRadius = 1f;
    [SerializeField] private bool debugHits = true;

    protected virtual void Update()
    {
        if (playerTransform == null)
            return;

        if (target != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

            Vector2 directionToEnemy = target.position - transform.position;

            if (directionToEnemy.sqrMagnitude > 0.01f)
                transform.right = directionToEnemy.normalized;
        }
        else if (isLaunchedForward)
        {
            transform.position += (Vector3)(launchDirection * speed * Time.deltaTime);
        }

        HandleAttack();
        HandleStopping();
    }

    public void SetupSword(Skill_Sword swordManager)
    {
        this.swordManager = swordManager;

        playerTransform = swordManager.player.transform;
        playerStats = swordManager.player.stats;
        damageScaleData = swordManager.damageScaleData;

        anim?.SetTrigger("spin");

        maxDistance = swordManager.maxDistance;
        attacksPerSecond = swordManager.attacksPerSecond;
    }

    public void LaunchForward(Vector2 direction, float launchSpeed)
    {
        target = null;

        isLaunchedForward = true;
        launchDirection = direction.normalized;
        speed = launchSpeed;

        if (launchDirection.sqrMagnitude < 0.01f)
            launchDirection = Vector2.down;

        transform.right = launchDirection;
    }

    public void LaunchToClosestEnemy(float launchSpeed)
    {
        target = FindClosestTarget();
        speed = launchSpeed;

        isLaunchedForward = false;

        if (target == null)
        {
            Debug.Log("[Sword] No enemy found. Launching forward instead.");
            LaunchForward(GetPlayerFacingDirection(), launchSpeed);
            return;
        }

        Debug.Log("[Sword] Launching toward enemy: " + target.name);

        Vector2 directionToEnemy = target.position - transform.position;

        if (directionToEnemy.sqrMagnitude > 0.01f)
            transform.right = directionToEnemy.normalized;
    }

    public void MoveTowardsClosestTarget(float speed)
    {
        LaunchToClosestEnemy(speed);
    }

    private Vector2 GetPlayerFacingDirection()
    {
        if (playerTransform == null)
            return Vector2.down;

        Player player = playerTransform.GetComponent<Player>();

        if (player == null || player.lastMoveDirection.sqrMagnitude < 0.01f)
            return Vector2.down;

        return player.lastMoveDirection.normalized;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (debugHits)
        {
            Debug.Log(
                $"[Sword] Trigger hit: {collision.name}, " +
                $"Layer={LayerMask.LayerToName(collision.gameObject.layer)}, " +
                $"IsTrigger={collision.isTrigger}"
            );
        }

        if (!IsInEnemyLayer(collision))
        {
            if (debugHits)
                Debug.Log($"[Sword] Ignored: {collision.name} is not in whatIsEnemy mask.");

            return;
        }

        DamageCollider(collision);
    }

    private void HandleStopping()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer < 0)
        {
            DamageEnemiesInSwordRadius();
            attackTimer = 1f / attacksPerSecond;
        }
    }

    private void DamageEnemiesInSwordRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            hitRadius,
            whatIsEnemy
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            DamageCollider(hit);
        }
    }

    private void DamageCollider(Collider2D collision)
    {
        IDamageable damageable =
            collision.GetComponent<IDamageable>() ??
            collision.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            if (debugHits)
                Debug.LogWarning($"[Sword] Hit {collision.name}, but no IDamageable found on it or parent.");

            return;
        }

        if (playerStats == null || damageScaleData == null)
        {
            Debug.LogWarning("[Sword] Missing playerStats or damageScaleData.");
            return;
        }

        AttackData attackData = playerStats.GetAttackData(damageScaleData);

        damageable.TakeDamage(
            attackData.physicalDamage,
            attackData.elementalDamage,
            attackData.element,
            transform
        );

        Entity_StatusHandler statusHandler =
            collision.GetComponent<Entity_StatusHandler>() ??
            collision.GetComponentInParent<Entity_StatusHandler>();

        if (attackData.element != ElementType.None)
            statusHandler?.ApplyStatusEffect(attackData.element, attackData.effectData);

        usedElement = attackData.element;

        if (debugHits)
            Debug.Log($"[Sword] Damaged target through {collision.name}.");
    }

    private bool IsInEnemyLayer(Collider2D collision)
    {
        return (whatIsEnemy.value & (1 << collision.gameObject.layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}