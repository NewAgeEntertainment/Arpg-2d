using UnityEngine;

public class Enemy_MoveState : Enemy_GroundedState
{
    public Enemy_MoveState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log("Move State Entered");
        //if (enemy.groundDetected == false || enemy.wallDetected)
        //    enemy.Flip();
    }


    public override void Update()
    {

        base.Update();
        //if (enemy.isKnocked)
        //    return;


        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);

        //enemy.SetVelocity(enemy.moveSpeed * enemy.currentDir.x, enemy.moveSpeed * enemy.currentDir.y);

        //if (xInput == 0 || yInput == 0)
        //    stateMachine.ChangeState(enemy.idleState);
    }
}
