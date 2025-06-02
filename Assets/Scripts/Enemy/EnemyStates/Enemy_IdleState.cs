using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_IdleState : Enemy_GroundedState
{
    public Enemy_IdleState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        

        rb.linearVelocity = Vector2.zero; // Stop the enemy's movement when entering idle state  

        stateTimer = enemy.idleTime;
        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0 && !enemy.isPaused) // Corrected the condition
        {
            stateMachine.ChangeState(enemy.patrollingState); // Change to patrolling state after idle time
            return;
        }

        
    }

    public override void Exit()
    {
        base.Exit();
        
    }

    
}
