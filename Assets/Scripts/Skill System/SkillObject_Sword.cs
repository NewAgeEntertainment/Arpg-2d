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
            launchDirection = Vector2.right;

        transform.right = launchDirection;
    }

    public void MoveTowardsClosestTarget(float speed)
    {
        target = FindClosestTarget();
        this.speed = speed;

        isLaunchedForward = false;
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
            attackTimer = 1 / attacksPerSecond;
        }
    }
}