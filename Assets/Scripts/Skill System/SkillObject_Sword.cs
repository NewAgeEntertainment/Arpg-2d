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
        DamageEnemiesInRadius(transform, 1);
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
            DamageEnemiesInRadius(transform, 1);
            attackTimer = 1f / attacksPerSecond;
        }
    }
}