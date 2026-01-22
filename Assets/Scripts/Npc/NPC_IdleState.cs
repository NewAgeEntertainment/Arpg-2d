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

        // ✅ Keep sprite facing the last direction we were moving/looking
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

        // Always stop in idle
        npc.SetZeroVelocity();

        // ✅ If interacting (Dialogue), just stay idle and keep facing interactor/lastFacing
        if (npc.IsInteracting)
        {
            // If you want them to continuously face the interactor while talking:
            // npc.FaceCurrentInteractor();
            return;
        }

        // ✅ Ensure our idle keeps the last facing (in case other systems changed it)
        npc.ApplyLastFacing();

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
                npc.AdvancePatrolIndexAndTarget();
                stateMachine.ChangeState(npc.patrolState);
                return;
            }
        }

        // ❌ DO NOT set xInput/yInput to 0 here.
        // That forces a default facing and overrides start-facing / lastFacing.
        //
        // If your animator needs an "idle" indicator, use a separate param like:
        // anim.SetFloat("speed", 0f);
        // or anim.SetBool("isMoving", false);
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
