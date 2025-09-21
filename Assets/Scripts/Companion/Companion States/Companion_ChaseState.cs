using UnityEngine;

// Chase Enemy
public class Companion_ChaseState : CompanionState
{
    private Transform target;

    public Companion_ChaseState(Companion c, StateMachine sm)
        : base(c, sm, "move") { }

    public override void Enter()
    {
        base.Enter();
        target = companion.GetNearestEnemy();
    }

    public override void Update()
    {
        base.Update();

        if (companion.IsTooFarFromPlayer())
        {
            stateMachine.ChangeState(companion.returnState);
            return;
        }

        // Reacquire if lost, else fall back to following
        if (target == null || !companion.IsEnemyInChaseRadius(target))
        {
            target = companion.GetNearestEnemy();
            if (target == null)
            {
                stateMachine.ChangeState(companion.followState);
                return;
            }
        }

        if (companion.IsEnemyInAttackRange(target))
        {
            stateMachine.ChangeState(companion.attackState);
            return;
        }

        // Move toward the target
        companion.MoveTo(target.position);
        companion.FaceTarget(target.position);
    }
}
