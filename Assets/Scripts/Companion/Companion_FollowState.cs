using UnityEngine;

// Follow Player
public class Companion_FollowState : CompanionState
{
    private const float EnemyCheckInterval = 0.2f;
    private float _nextEnemyCheckTime;

    public Companion_FollowState(Companion companion, StateMachine stateMachine)
        : base(companion, stateMachine, "move") { }

    public override void Enter()
    {
        base.Enter();
        _nextEnemyCheckTime = 0f;
        companion.StopMovement();
    }

    public override void Exit()
    {
        companion.StopMovement();
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        // Hard gates / safety
        if (!companion.InParty || companion.playerTarget == null)
        {
            companion.StopMovement();
            stateMachine.ChangeState(companion.idleState);
            return;
        }

        if (companion.IsTooFarFromPlayer())
        {
            stateMachine.ChangeState(companion.returnState);
            return;
        }

        if (Time.time >= _nextEnemyCheckTime)
        {
            _nextEnemyCheckTime = Time.time + EnemyCheckInterval;
            if (companion.HasEnemyInChaseRadius())
            {
                stateMachine.ChangeState(companion.chaseState);
                return;
            }
        }

        if (companion.IsCloseEnoughToPlayer())
        {
            companion.StopMovement();
            stateMachine.ChangeState(companion.idleState);
            return;
        }

        Vector2 targetPos = companion.playerTarget.position;
        companion.MoveTo(targetPos);
        companion.FaceTarget(targetPos);
    }
}
