using UnityEngine;

public class Enemy_AttackState : EnemyState
{
    public Enemy_AttackState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        SyncAttackSpeed(); // Sync the attack speed with the enemy's stats
        rb.velocity = Vector2.zero; // Stop the enemy's movement when entering attack state
    }

    public override void Exit()
    {
        base.Exit();
        enemy.lastTimeAttacked = Time.time; // Update the last time the enemy attacked
    }

    public override void Update()
    {
        base.Update();

        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }
}
