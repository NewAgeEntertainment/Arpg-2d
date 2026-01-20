using UnityEngine;

public class NPC_FollowState : EntityState
{
    private readonly NPC npc;
    private const float RepathCheckInterval = 0.15f;
    private float nextCheckAt = 0f;

    public NPC_FollowState(NPC npc, StateMachine sm, string animBool)
        : base(sm, animBool)
    {
        this.npc = npc;
        this.anim = npc.anim;
        this.rb = npc.rb;
    }

    public override void Enter()
    {
        base.Enter();
        nextCheckAt = 0f;
        npc.SetZeroVelocity();
    }

    public override void Update()
    {
        base.Update();

        if (npc.isInteracting)
        {
            npc.SetZeroVelocity();
            stateMachine.ChangeState(npc.idleState);
            return;
        }


        // stop following if not commanded or no target
        if (!npc.followCommanded || npc.followTarget == null)
        {
            if (npc.patrolPoints != null && npc.patrolPoints.Length > 0 && npc.autoStartPatrol)
                stateMachine.ChangeState(npc.patrolState);
            else
                stateMachine.ChangeState(npc.idleState);
            return;
        }

        float dist = Vector2.Distance(npc.transform.position, npc.followTarget.position);

        // close enough → idle
        if (dist <= npc.followStopDistance)
        {
            npc.SetZeroVelocity();
            stateMachine.ChangeState(npc.idleState);
            return;
        }

        // move toward player (repath throttle is optional)
        if (Time.time >= nextCheckAt)
        {
            nextCheckAt = Time.time + RepathCheckInterval;

            Vector2 from = npc.transform.position;
            Vector2 to = npc.followTarget.position;
            Vector2 dir = (to - from).normalized;

            npc.SetVelocity(dir.x * npc.moveSpeed, dir.y * npc.moveSpeed);
            npc.UpdateFacing(dir);                  // <- sets xInput/yInput and remembers lastFacing
        }
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
