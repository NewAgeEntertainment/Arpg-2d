using UnityEngine;

public class Enemy_PatrollingState : Enemy_GroundedState
{
    public Enemy_PatrollingState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // If entering from Battle state, set animation  
        if (enemy.previousState == enemy.battleState)
        {
            enemy.anim.SetFloat("xInput", enemy.currentDir.x);
            enemy.anim.SetFloat("xInput", enemy.currentDir.y);
        }

        // If entering from Idle state, move to the next patrol point  
        if (enemy.previousState == enemy.idleState)
        {
            enemy.StartCoroutine(enemy.SetPatrolPoint());
        }

        enemy.target = enemy.patrolPoints[enemy.currentPatrolIndex]; // Set the target to the current patrol point  
    }

    public override void Exit()
    {
        base.Exit();
        
    }

    public override void Update()
    {
        base.Update();
       
        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);

        if (enemy.PlayerDetected()) // 
        {
            stateMachine.ChangeState(enemy.battleState); // Change to move state if not within attack distance
        }

        if (enemy.isPaused)
        {
            rb.linearVelocity = Vector2.zero; // Stop the enemy's movement when paused
            
            stateMachine.ChangeState(enemy.idleState); // Change to idle state if paused

            return;
        }

        Vector2 movedirection = ((Vector3)enemy.target - enemy.transform.position).normalized; // Calculate the direction to the target point  
        rb.linearVelocity = movedirection * enemy.moveSpeed; // Set the enemy's velocity towards the target  

        if (Vector2.Distance(enemy.transform.position, enemy.target) < .1f) // check if the enemy has reached the target point  
        {
            enemy.StartCoroutine(enemy.SetPatrolPoint()); // Move to the next patrol point  
        }


        // Update the enemy's current direction based on the movement direction

    }
}