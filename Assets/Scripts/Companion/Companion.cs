using System;
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

    // --- Party (needed by PartyManager & Companion_Rabbie) ---
    [Header("Party")]
    [SerializeField] private bool startInParty = false; // Inspector toggle
    public bool StartInParty => startInParty;
    public bool InParty { get; private set; }
    public Action<bool> OnPartyFlagChanged;
    private bool _queueFollow; // if SetInParty happens before Initialize(...)

    // --- States ---
    [HideInInspector] public Companion_FollowState followState;
    [HideInInspector] public Companion_ChaseState chaseState;
    [HideInInspector] public Companion_AttackState attackState;
    [HideInInspector] public Companion_ReturnState returnState;
    [HideInInspector] public Companion_IdleState idleState;

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();   // keep your original pattern
        combat = GetComponent<CompanionCombat>();
    }

    protected override void Start()
    {
        if (playerTarget == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        // Create states if not already provided by a subclass
        if (followState == null) followState = new Companion_FollowState(this, stateMachine);
        if (chaseState == null) chaseState = new Companion_ChaseState(this, stateMachine);
        if (attackState == null) attackState = new Companion_AttackState(this, stateMachine);
        if (returnState == null) returnState = new Companion_ReturnState(this, stateMachine);
        if (idleState == null) idleState = new Companion_IdleState(this, stateMachine);

        // Initialize once
        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);

        // If recruited early or Start In Party is checked, ensure we end in Follow
        AfterStateMachineInitialized();
    }

    protected override void Update()
    {
        stateMachine.UpdateActiveState();
    }

    // ---------------- Movement ----------------
    // Companion.cs — replace these

    public void MoveTo(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f)
        {
            SetZeroVelocity();             // <- Entity API (applied in FixedUpdate)
            return;
        }

        dir.Normalize();
        SetVelocity(dir.x * moveSpeed, dir.y * moveSpeed);  // <- Entity API

        // keep animator aligned with motion
        anim.SetFloat("xInput", dir.x);
        anim.SetFloat("yInput", dir.y);
    }

    public void StopMovement()
    {
        SetZeroVelocity();                 // <- Entity API
    }

    public float DistanceToPlayer() =>
    playerTarget ? Vector2.Distance(transform.position, playerTarget.position) : float.PositiveInfinity;

    // use start distance for waking up from Idle
    public bool ShouldStartFollowing() =>
        DistanceToPlayer() > followStartDistance;


    public bool IsTooFarFromPlayer()
    {
        if (playerTarget == null) return false;
        return Vector2.Distance(transform.position, playerTarget.position) > maxFollowDistance;
    }

    public bool IsCloseEnoughToPlayer()
    {
        if (playerTarget == null) return false;
        return Vector2.Distance(transform.position, playerTarget.position) <= followStopDistance;
    }

    // ---------------- Combat helpers ----------------
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
        if (enemy == null) return false;
        return Vector2.Distance(transform.position, enemy.position) <= attackRange;
    }

    public bool IsEnemyInChaseRadius(Transform enemy)
    {
        if (enemy == null) return false;
        return Vector2.Distance(transform.position, enemy.position) <= chaseRadius;
    }

    public void FaceTarget(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        // Cardinal rounding
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

    // ---------------- Party API ----------------
    /// <summary>Called by CompanionPartyManager when recruiting/dismissing.</summary>
    public void SetInParty(bool value)
    {
        if (InParty == value) return;
        InParty = value;
        OnPartyFlagChanged?.Invoke(value);

        if (value)
        {
            if (playerTarget == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTarget = p.transform;
            }

            if (followState != null && stateMachine?.currentState != null)
                stateMachine.ChangeState(followState);
            else
                _queueFollow = true;
        }
        else
        {
            if (idleState != null && stateMachine?.currentState != null)
                stateMachine.ChangeState(idleState);
            StopMovement();
        }
    }

    /// <summary>Call once immediately after Initialize(idleState).</summary>
    public void AfterStateMachineInitialized()
    {
        if ((InParty || StartInParty || _queueFollow) &&
            followState != null && stateMachine?.currentState != null)
        {
            _queueFollow = false;
            if (!InParty && StartInParty) InParty = true; // keep event noise low
            stateMachine.ChangeState(followState);
        }
    }

    // ---------------- Gizmos ----------------
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
