using UnityEngine;

public class Enemy_AttackState : EnemyState
{
    public Enemy_AttackState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        SyncAttackSpeed();
        rb.velocity = Vector2.zero;

        var p = enemy.GetPlayerReference();
        if (p != null)
            enemy.SetFacing(p.position - enemy.transform.position);
    }

    public override void Exit()
    {
        base.Exit();
        enemy.lastTimeAttacked = Time.time; // Update the last time the enemy attacked
    }

    public override void Update()
    {
        base.Update();

        // Keep looking at the player while attacking
        var p = enemy.GetPlayerReference();
        if (p != null)
            enemy.SetFacing(p.position - enemy.transform.position);

        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }
}
