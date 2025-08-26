using UnityEngine;

// Idle State
public class Companion_IdleState : CompanionState
{
    public Companion_IdleState(Companion c, StateMachine sm)
        : base(c, sm, "idle") { }

    public override void Enter()
    {
        base.Enter();
        anim.SetFloat("xInput", 0);
        anim.SetFloat("yInput", 0);
        companion.StopMovement();
    }

    public override void Update()
    {
        base.Update();

        // --- DO NOTHING UNTIL RECRUITED ---
        if (!companion.InParty) return;
        if (companion.playerTarget == null) return;

        if (companion.HasEnemyInChaseRadius())
        {
            stateMachine.ChangeState(companion.chaseState);
            return;
        }

        if (!companion.IsCloseEnoughToPlayer())
        {
            stateMachine.ChangeState(companion.followState);
            return;
        }
    }


}
