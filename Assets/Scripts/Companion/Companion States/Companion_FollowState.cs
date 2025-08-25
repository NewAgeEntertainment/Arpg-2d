using UnityEngine;

// Follow Player
public class Companion_FollowState : CompanionState
{
    public Companion_FollowState(Companion c, StateMachine sm)
        : base(c, sm, "move") { }

    public override void Update()
    {
        base.Update();

        // If dismissed or there's no target, stop and idle.
        if (!companion.InParty || companion.playerTarget == null)
        {
            companion.StopMovement();
            stateMachine.ChangeState(companion.idleState);
            return;
        }

        // Combat takes priority.
        if (companion.HasEnemyInChaseRadius())
        {
            stateMachine.ChangeState(companion.chaseState);
            return;
        }

        // Reached player? idle.
        float sqrDist = (companion.playerTarget.position - companion.transform.position).sqrMagnitude;
        float stopDist = companion.followStopDistance;
        if (sqrDist <= stopDist * stopDist)
        {
            companion.StopMovement();
            stateMachine.ChangeState(companion.idleState);
            return;
        }

        // Move toward the player and face them.
        Vector2 targetPos = companion.playerTarget.position;
        companion.MoveTo(targetPos);
        companion.FaceTarget(targetPos);
    }
}
