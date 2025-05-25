using UnityEngine;

public class Enemy_Rabbie : Enemy , ICounterable
{
    public bool CanBeCountered { get => canBeStunned; }

    protected override void Awake()
    {
        base.Awake();

        idleState = new Enemy_IdleState(this, stateMachine, "idle");
        moveState = new Enemy_MoveState(this, stateMachine, "move");
        attackState = new Enemy_AttackState(this, stateMachine, "attack");
        battleState = new Enemy_BattleState(this, stateMachine, "move");
        patrollingState = new Enemy_PatrollingState(this, stateMachine, "move");
        deadState = new Enemy_DeadState(this, stateMachine, "dead");
        stunnedState = new Enemy_StunnedState(this, stateMachine, "Stunned");
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }

    

    // stunned logic that is connected to the (Enemy_StunnedState) code
    // This method is called when the enemy is countered.
    public void HandleCounter()
    {
        // Stunning logic 
        if (CanBeCountered == false)
            return;

        stateMachine.ChangeState(stunnedState); // Change to stunned state

    }
}
