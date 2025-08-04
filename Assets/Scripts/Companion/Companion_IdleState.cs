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

        if (companion.HasEnemyInChaseRadius())
        {
            stateMachine.ChangeState(companion.chaseState);
        }
        else if (!companion.IsCloseEnoughToPlayer())
        {
            stateMachine.ChangeState(companion.followState);
        }
    }
}

