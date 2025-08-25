using UnityEngine;

// Idle State
public class Companion_IdleState : CompanionState
{
    public Companion_IdleState(Companion c, StateMachine sm)
        : base(c, sm, "idle") { }

    public override void Enter()
    {
        base.Enter();
        companion.StopMovement();
        anim.SetFloat("xInput", 0);
        anim.SetFloat("yInput", 0);
    }

    public override void Update()
    {
        base.Update();

        // If not in the party (dismissed), do nothing.
        if (!companion.InParty) return;

        // Safety: no player yet -> stay idle.
        if (companion.playerTarget == null) return;

        // Prefer combat if an enemy is around.
        if (companion.HasEnemyInChaseRadius())
        {
            stateMachine.ChangeState(companion.chaseState);
            return;
        }

        // Start following if we drift beyond the start distance.
        float sqrDist = (companion.playerTarget.position - companion.transform.position).sqrMagnitude;
        float startDist = companion.followStartDistance;
        if (sqrDist > startDist * startDist)
        {
            stateMachine.ChangeState(companion.followState);
        }
    }
}
