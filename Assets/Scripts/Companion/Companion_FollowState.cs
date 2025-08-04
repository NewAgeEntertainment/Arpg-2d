using UnityEngine;

// Follow Player
public class Companion_FollowState : CompanionState
{
    public Companion_FollowState(Companion c, StateMachine sm)
        : base(c, sm, "move") { }

    public override void Update()
    {
        base.Update();

        if (companion.HasEnemyInChaseRadius())
        {
            stateMachine.ChangeState(companion.chaseState);
            return;
        }

        if (companion.IsCloseEnoughToPlayer())
        {
            companion.StopMovement();
            stateMachine.ChangeState(companion.idleState);
            return;
        }

        Vector2 direction = (companion.playerTarget.position - companion.transform.position).normalized;
        companion.MoveTo(companion.playerTarget.position);
        companion.FaceTarget(companion.playerTarget.position);
    }
}

