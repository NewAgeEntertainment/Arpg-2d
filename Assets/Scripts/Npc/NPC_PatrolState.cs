using UnityEngine;

public class NPC_PatrolState : EntityState
{
    private const float reachTolerance = 0.1f;
    private readonly NPC npc;

    public NPC_PatrolState(NPC npc, StateMachine sm, string animBool)
        : base(sm, animBool)
    {
        this.npc = npc;
        this.anim = npc.anim;
        this.rb = npc.rb;
    }

    public override void Enter()
    {
        base.Enter();

        if (npc.patrolPoints == null || npc.patrolPoints.Length == 0)
        {
            npc.SetZeroVelocity();
            stateMachine.ChangeState(npc.idleState);
            return;
        }

        if (npc.currentPatrolIndex < 0 || npc.currentPatrolIndex >= npc.patrolPoints.Length)
            npc.currentPatrolIndex = 0;

        npc.target = npc.patrolPoints[npc.currentPatrolIndex];

        Vector2 dir = (npc.target - (Vector2)npc.transform.position).normalized;
        npc.currentDir = dir;
        npc.UpdateFacing(dir);
    }

    public override void Update()
    {
        base.Update();

        if (npc.IsInteracting)
        {
            npc.SetZeroVelocity();
            stateMachine.ChangeState(npc.idleState);
            return;
        }


        // follow has priority
        if (npc.followCommanded && npc.followTarget != null)
        {
            float d = Vector2.Distance(npc.transform.position, npc.followTarget.position);
            if (d > npc.followStopDistance)
            {
                npc.SetZeroVelocity();
                stateMachine.ChangeState(npc.followState);
                return;
            }
        }

        if (npc.patrolPoints == null || npc.patrolPoints.Length == 0)
        {
            npc.SetZeroVelocity();
            stateMachine.ChangeState(npc.idleState);
            return;
        }

        Vector2 pos = npc.transform.position;
        Vector2 toTarget = npc.target - pos;
        float dist = toTarget.magnitude;

        // ✅ reached waypoint -> go idle (pause happens in idle)
        if (dist <= reachTolerance)
        {
            npc.SetZeroVelocity();
            stateMachine.ChangeState(npc.idleState);
            return;
        }

        Vector2 dir = toTarget.normalized;
        npc.currentDir = dir;
        npc.SetVelocity(dir.x * npc.moveSpeed, dir.y * npc.moveSpeed);

        // ✅ keep lastFacing updated continuously
        npc.UpdateFacing(dir);

        //if (npc.anim)
        //{
        //    //npc.anim.SetFloat("xInput", dir.x);
        //    //npc.anim.SetFloat("yInput", dir.y);
        //}
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
