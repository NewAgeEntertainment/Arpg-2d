using UnityEngine;

public class Enemy_StunnedState : EnemyState
{
    private Enemy_VFX vfx;

    public Enemy_StunnedState(Enemy enemy, StateMachine stateMachine, string animBoolName)
        : base(enemy, stateMachine, animBoolName)
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

        // Stop any chase movement first
        enemy.SetZeroVelocity();
        if (rb != null) rb.velocity = Vector2.zero; // optional extra safety

        vfx?.EnableAttackAlert(false);
        enemy.EnableCounterWindow(false);

        stateTimer = enemy.stunnedDuration;

        // ---- Apply knockback using Entity's KB system ----
        // Knock opposite of currentDir (fallback: straight up)
        Vector2 dir =
            enemy.currentDir.sqrMagnitude > 0.0001f
                ? -enemy.currentDir.normalized
                : Vector2.up;

        Vector2 kb = new Vector2(
            dir.x * enemy.stunnedVelocity.x,
            dir.y * enemy.stunnedVelocity.y
        );

        // Let the built-in knockback coroutine move the Rigidbody and
        // temporarily ignore normal movement in FixedUpdate.
        enemy.ReciveKnockback(kb, enemy.stunnedDuration * 0.3f);
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0f)
        {
            // End of stun: kill any remaining knockback and stop movement
            enemy.CancelKnockbackImmediate();
            enemy.SetZeroVelocity();
            if (rb != null) rb.velocity = Vector2.zero;

            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
