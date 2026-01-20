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
        npc.ApplyLastFacing();

        idleTimer = 0f;

        waitingForPatrolContinue =
            npc.autoStartPatrol &&
            npc.patrolPoints != null &&
            npc.patrolPoints.Length > 0 &&
            npc.followCommanded == false;
    }

    public override void Update()
    {
        base.Update();

        // ✅ If interacting, stay idle + face the player
        // If we're in a Dialogue System interaction, stay idle and keep facing player
        if (npc.IsInteracting)
        {
            npc.SetZeroVelocity();
            if (npc.faceInteractorWhileTalking)
                npc.FaceCurrentInteractor();
            return;
        }



        // Follow takes priority over everything
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

        // Patrol pause logic
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

        // ✅ Keep facing direction while idling
        npc.ApplyLastFacing();
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
