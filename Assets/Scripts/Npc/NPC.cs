using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NPC : Entity
{
    // ───── Movement
    [Header("Movement")]
    public float moveSpeed = 3f;

    // ───── Animator params (driven by states)
    // xinput / yinput are set in states; nothing to configure here.

    // ───── Patrol (enemy-like)
    [Header("Patrol")]
    public List<Transform> patrolPoints = new List<Transform>();
    public bool loopPatrol = true;
    public float waypointTolerance = 0.10f;
    public float waitAtWaypoint = 0.25f;
    public bool autoStartPatrol = false;

    // ───── Follow (command-only via Dialogue)
    [Header("Follow (commanded only)")]
    public bool followCommanded = false;        // <— the missing field
    public Transform followTarget = null;       // usually PlayerLocator.Current
    public float followStopDistance = 1.25f;

    // ───── States
    [HideInInspector] public NPC_IdleState idleState;
    [HideInInspector] public NPC_PatrolState patrolState;
    [HideInInspector] public NPC_FollowState followState;
    [HideInInspector] public Vector2 lastFacing = Vector2.down;

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();

        // Create states
        idleState = new NPC_IdleState(this, stateMachine, "idle");
        patrolState = new NPC_PatrolState(this, stateMachine, "move");   // uses x/y
        followState = new NPC_FollowState(this, stateMachine, "move");   // uses x/y
    }

    protected override void Start()
    {
        // Start in Idle; state will hand off to patrol if autoStartPatrol is true
        stateMachine.Initialize(idleState);
    }

    public void UpdateFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            lastFacing = dir;
            if (anim)
            {
                anim.SetFloat("xInput", dir.x);
                anim.SetFloat("yInput", dir.y);
            }
        }
    }

    public void ApplyLastFacing()
    {
        if (anim)
        {
            anim.SetFloat("xInput", lastFacing.x);
            anim.SetFloat("yInput", lastFacing.y);
        }
    }

    // Helpers for animation & movement
    public void SetIdleAnim()
    {
        if (!anim) return;
        anim.SetFloat("xinput", 0f);
        anim.SetFloat("yinput", 0f);
    }

    // Public API used by dialogue bridge
    public void StartFollowing(Transform target)
    {
        followCommanded = true;
        followTarget = target;
        if (stateMachine.currentState != followState)
            stateMachine.ChangeState(followState);
    }

    public void StartFollow(Transform target) => StartFollowing(target);
    public void StopFollow() => StopFollowing();

    public void StopFollowing()
    {
        followCommanded = false;
        followTarget = null;

        if (autoStartPatrol && patrolPoints != null && patrolPoints.Count > 0)
            stateMachine.ChangeState(patrolState);
        else
            stateMachine.ChangeState(idleState);
    }
}
