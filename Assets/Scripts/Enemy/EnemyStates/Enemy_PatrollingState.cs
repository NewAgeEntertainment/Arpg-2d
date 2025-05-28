using UnityEngine;

public class Enemy_PatrollingState : Enemy_GroundedState
{
    public Enemy_PatrollingState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        enemy.target = enemy.patrolPoints[0]; // Initialize the target to the first patrol point

        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);
        Debug.Log("Patrolling State Entered");
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
            return;
        }

        Vector2 movedirection = ((Vector3)enemy.target - enemy.transform.position).normalized; // Calculate the direction to the target point  
        rb.linearVelocity = movedirection * enemy.moveSpeed; // Set the enemy's velocity towards the target  

        if (Vector2.Distance(enemy.transform.position, enemy.target) < .1f) // check if the enemy has reached the target point  
        {
            enemy.StartCoroutine(enemy.SetPatrolPoint()); // Move to the next patrol point  
        }

    }
}