using UnityEngine;

public class Companion_Rabbie : Companion
{
    protected override void Awake()
    {
        base.Awake();
        idleState = new Companion_IdleState(this, stateMachine);
        followState = new Companion_FollowState(this, stateMachine);
        chaseState = new Companion_ChaseState(this, stateMachine);
        attackState = new Companion_AttackState(this, stateMachine);
        returnState = new Companion_ReturnState(this, stateMachine);

        // ensure deadState exists with current settings
        if (deadState == null)
            deadState = new Companion_DeadState(this, stateMachine, "dead", 5f, 0.3f);
    }

    protected override void Start()
    {
        base.Start();
        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);  // only if nothing set yet
    }
}
