using UnityEngine;

public class NPC_PatrolState : EntityState
{
    private readonly NPC npc;
    private int index = 0;
    private float waitTimer = 0f;

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
        waitTimer = 0f;

        if (npc.patrolPoints == null || npc.patrolPoints.Count == 0)
        {
            stateMachine.ChangeState(npc.idleState);
            return;
        }

        if (index < 0 || index >= npc.patrolPoints.Count) index = 0;
    }

    public override void Update()
    {
        base.Update();

        // follow command takes priority
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

        // no points → idle
        if (npc.patrolPoints == null || npc.patrolPoints.Count == 0)
        {
            stateMachine.ChangeState(npc.idleState);
            return;
        }

        if (index < 0 || index >= npc.patrolPoints.Count) index = 0;
        Transform point = npc.patrolPoints[index];
        if (!point) { NextIndex(); return; }

        Vector2 from = npc.transform.position;
        Vector2 to = point.position;
        float dist = Vector2.Distance(from, to);

        if (dist <= npc.waypointTolerance)
        {
            npc.SetZeroVelocity();
            waitTimer += Time.deltaTime;
            if (waitTimer >= npc.waitAtWaypoint)
            {
                waitTimer = 0f;
                NextIndex();
            }
            return;
        }

        // move toward waypoint
        Vector2 dir = (to - from).normalized;
        npc.SetVelocity(dir.x * npc.moveSpeed, dir.y * npc.moveSpeed);
        npc.UpdateFacing(dir);                      // <- sets xInput/yInput and remembers lastFacing
    }

    private void NextIndex()
    {
        index++;
        if (index >= npc.patrolPoints.Count)
            index = npc.loopPatrol ? 0 : npc.patrolPoints.Count - 1;
    }

    public override void Exit()
    {
        npc.SetZeroVelocity();
        base.Exit();
    }
}
