using UnityEngine;

public class Enemy_GroundedState : EnemyState
{
    public Enemy_GroundedState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);
    }

    public override void Update()
    {
        base.Update();



        if (enemy.PlayerDetected())
        {
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}
