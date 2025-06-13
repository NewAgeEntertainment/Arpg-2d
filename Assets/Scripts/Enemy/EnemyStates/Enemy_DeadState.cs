using UnityEngine;

public class Enemy_DeadState : EnemyState
{
    public Enemy_DeadState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }


    public override void Enter()
    {
        base.Enter();

        anim.Play("dead");
        rb.velocity = Vector2.zero; // Stop the enemy's movement

        Debug.Log("Entered dead State");
    }
}
