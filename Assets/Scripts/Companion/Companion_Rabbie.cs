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
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }
}
