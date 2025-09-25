using UnityEngine;

public class Enemy_StunnedState : EnemyState
{
    private Enemy_VFX vfx;
    public Enemy_StunnedState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
        vfx = enemy.GetComponent<Enemy_VFX>();
    }

    public override void Enter()
    {
        base.Enter();

        // If configured to ignore stun while attacking, exit immediately
        if (enemy.ignoreStunDuringAttack && enemy.IsAttacking)
        {
            stateMachine.ChangeState(enemy.battleState);
            return;
        }

        vfx?.EnableAttackAlert(false);
        enemy.EnableCounterWindow(false);
        stateTimer = enemy.stunnedDuration;

        // apply stunned velocity (your original)
        rb.velocity = new Vector2(
            enemy.stunnedVelocity.x * -enemy.currentDir.x,
            enemy.stunnedVelocity.y - enemy.currentDir.y
        );
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.idleState);
    }
}
