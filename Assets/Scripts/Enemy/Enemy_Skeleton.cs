using UnityEngine;

public class Enemy_Skeleton : Enemy
{

    protected override void Awake()
    {
        base.Awake();

        idleState = new Enemy_IdleState(this, stateMachine, "idle");
        moveState = new Enemy_MoveState(this, stateMachine, "move");
        attackState = new Enemy_AttackState(this, stateMachine, "attack");
       // battleState = new Enemy_BattleState(this, stateMachine, "battle");
        //patrollingState = new Enemy_PatrollingState(this, stateMachine, "move");
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }
}
