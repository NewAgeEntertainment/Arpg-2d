using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Companion : Entity
{
    [HideInInspector] public CompanionCombat combat;

    [Header("Follow Settings")]
    public Transform playerTarget;
    public float followStartDistance = 3f;
    public float followStopDistance = 1.2f;
    public float maxFollowDistance = 10f;
    public float moveSpeed = 3.5f;

    [Header("Battle Settings")]
    public float chaseRadius = 6f;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    [Header("Animation Settings")]
    public float battleMoveSpeed = 4.5f;
    public float moveAnimSpeedMultiplier = 1f;

    [HideInInspector] public Companion_FollowState followState;
    [HideInInspector] public Companion_ChaseState chaseState;
    [HideInInspector] public Companion_AttackState attackState;
    [HideInInspector] public Companion_ReturnState returnState;
    [HideInInspector] public Companion_IdleState idleState;

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();
        combat = GetComponent<CompanionCombat>();
    }

    protected override void Start()
    {
        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player").transform;

        followState = new Companion_FollowState(this, stateMachine);
        chaseState = new Companion_ChaseState(this, stateMachine);
        attackState = new Companion_AttackState(this, stateMachine);
        returnState = new Companion_ReturnState(this, stateMachine);
        idleState = new Companion_IdleState(this, stateMachine);

        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        stateMachine.UpdateActiveState();
    }

    public void MoveTo(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        rb.velocity = dir * moveSpeed;
        UpdateAnimatorDirection(dir);
    }

    public void StopMovement()
    {
        rb.velocity = Vector2.zero;
    }

    public bool IsTooFarFromPlayer()
    {
        return Vector2.Distance(transform.position, playerTarget.position) > maxFollowDistance;
    }

    public bool IsCloseEnoughToPlayer()
    {
        return Vector2.Distance(transform.position, playerTarget.position) <= followStopDistance;
    }

    public bool HasEnemyInChaseRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, chaseRadius, enemyLayer);
        return hits.Length > 0;
    }

    public Transform GetNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, chaseRadius, enemyLayer);
        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider2D hit in hits)
        {
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = hit.transform;
            }
        }

        return closest;
    }

    public bool IsEnemyInAttackRange(Transform enemy)
    {
        return Vector2.Distance(transform.position, enemy.position) <= attackRange;
    }

    public bool IsEnemyInChaseRadius(Transform enemy)
    {
        return Vector2.Distance(transform.position, enemy.position) <= chaseRadius;
    }

    public void FaceTarget(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        // Use cardinal direction rounding
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            anim.SetFloat("xInput", direction.x > 0 ? 1 : -1);
            anim.SetFloat("yInput", 0);
        }
        else
        {
            anim.SetFloat("xInput", 0);
            anim.SetFloat("yInput", direction.y > 0 ? 1 : -1);
        }
    }



    private void UpdateAnimatorDirection(Vector2 dir)
    {
        anim.SetFloat("xInput", dir.x);
        anim.SetFloat("yInput", dir.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followStartDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followStopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
