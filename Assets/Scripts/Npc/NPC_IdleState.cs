using UnityEngine;

public class NPC_IdleState : EntityState
{
    private readonly NPC npc;

    private float idleTimer;
    private bool waitingForPatrolContinue;

    public NPC_IdleState(NPC npc, StateMachine sm, string animBool)
        : base(sm, animBool)
    {
        this.npc = npc;
        this.anim = npc.anim;
        this.rb = npc.rb;
    }

    public override void Enter()
    {
        base.Enter();

        npc.SetZeroVelocity();

        // ✅ Idle should face the last direction we were moving/looking
        npc.ApplyLastFacing();

        idleTimer = 0f;

        // ✅ If patrolling, this idle acts like a "pause at waypoint"
        waitingForPatrolContinue =
            npc.autoStartPatrol &&
            npc.patrolPoints != null &&
            npc.patrolPoints.Length > 0 &&
            !npc.followCommanded;
    }

    public override void Update()
    {
        base.Update();

        // If interacting (Dialogue), just stay idle and keep last facing
        if (npc.IsInteracting)
        {
            npc.SetZeroVelocity();
            return;
        }

        // Follow takes priority
        if (npc.followCommanded && npc.followTarget != null)
        {
            float dist = Vector2.Distance(npc.transform.position, npc.followTarget.position);
            if (dist > npc.followStopDistance)
            {
                stateMachine.ChangeState(npc.followState);
                return;
            }

            waitingForPatrolContinue = false;
        }

        // Patrol pause → resume after wait
        if (waitingForPatrolContinue)
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= npc.waitAtWaypoint)
            {
                // advance patrol index + set next target
                npc.AdvancePatrolIndexAndTarget();
                stateMachine.ChangeState(npc.patrolState);
                return;
            }
        }

        // If your animator needs "idle params", set them here.
        // NOTE: If xInput/yInput = 0 forces down-facing in your animator,
        // remove these two lines and rely on ApplyLastFacing() instead.
        if (npc.anim)
        {
            npc.anim.SetFloat("xInput", 0f);
            npc.anim.SetFloat("yInput", 0f);
        }
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
