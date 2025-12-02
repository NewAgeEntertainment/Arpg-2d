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

        rb.velocity = Vector2.zero; // Stop the enemy's movement when entering idle state  

        stateTimer = enemy.idleTime;
        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);
    }

    public override void Update()
    {
        base.Update();

        // No patrol points? Stay idle forever.
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)
            return;

        // Timer done and we have patrol points → start patrolling
        if (stateTimer < 0 && !enemy.isPaused)
        {
            stateMachine.ChangeState(enemy.patrollingState);
            return;
        }
    }


    public override void Exit()
    {
        base.Exit();
    }
}
