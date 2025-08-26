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

        if (target == null || !companion.IsEnemyInChaseRadius(target))
        {
            stateMachine.ChangeState(companion.followState);
            return;
        }

        if (companion.IsEnemyInAttackRange(target))
        {
            stateMachine.ChangeState(companion.attackState);
            return;
        }

        companion.MoveTo(target.position);
    }
}