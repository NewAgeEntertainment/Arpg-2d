using UnityEngine;

public class NPC_IdleState : EntityState
{
    private readonly NPC npc;

    public NPC_IdleState(NPC npc, StateMachine sm, string animBool)
        : base(sm, animBool)
    {
        this.npc = npc;
        this.anim = npc.anim;
        this.rb = npc.rb;
    }

    // in Idle.Enter()
    public override void Enter()
    {
        base.Enter();
        npc.SetZeroVelocity();
        npc.ApplyLastFacing();   // keep facing the last direction from follow/patrol
    }



    public override void Update()
    {
        base.Update();

        // If follow was commanded and we have a target, go follow unless already close enough
        if (npc.followCommanded && npc.followTarget != null)
        {
            float dist = Vector2.Distance(npc.transform.position, npc.followTarget.position);
            if (dist > npc.followStopDistance)
            {
                stateMachine.ChangeState(npc.followState);
                return;
            }
        }
        else
        {
            // Otherwise, optionally go patrol if configured
            if (npc.patrolPoints != null && npc.patrolPoints.Count > 0 && npc.autoStartPatrol)
            {
                stateMachine.ChangeState(npc.patrolState);
                return;
            }
        }

        if (npc.anim)
        {
            npc.anim.SetFloat("xinput", 0f);
            npc.anim.SetFloat("yinput", 0f);
        }
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
