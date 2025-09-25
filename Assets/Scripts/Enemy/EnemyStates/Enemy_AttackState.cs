using UnityEngine;

public class Enemy_AttackState : EnemyState
{
    private Vector2 lungeDir;
    private float lungeTimer;

    public Enemy_AttackState(Enemy enemy, StateMachine stateMachine, string animBoolName)
        : base(enemy, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        SyncAttackSpeed();

        // mark attacking (enables super-armor checks)
        enemy.IsAttacking = true;

        // cancel any ongoing knockback immediately so attack truly overrides it
        enemy.CancelKnockbackImmediate();

        // hard-stop before lunge (you use persistent desired-velocity)
        enemy.SetVelocity(0f, 0f);

        // face the player & pick lunge dir
        var p = enemy.GetPlayerReference();
        if (p != null)
        {
            lungeDir = ((Vector2)(p.position - enemy.transform.position)).normalized;
            enemy.SetFacing(lungeDir);
        }
        else
        {
            lungeDir = enemy.LastDir.sqrMagnitude > 0.0001f ? enemy.LastDir.normalized : Vector2.down;
            enemy.SetFacing(lungeDir);
        }

        // start lunge window (constant, no curve)
        lungeTimer = Mathf.Max(0f, enemy.attackLungeDuration);
    }

    public override void Update()
    {
        base.Update();

        // keep looking at the player while attacking
        var p = enemy.GetPlayerReference();
        if (p != null)
            enemy.SetFacing(p.position - enemy.transform.position);

        // lunge for the configured duration (constant push)
        if (lungeTimer > 0f)
        {
            Vector2 v = lungeDir * enemy.attackLungeSpeed;
            enemy.SetVelocity(v.x, v.y);

            lungeTimer -= Time.deltaTime;
            if (lungeTimer <= 0f)
                enemy.SetVelocity(0f, 0f); // lock in place after the lunge
        }
        else
        {
            // movement locked during the rest of the attack
            enemy.SetVelocity(0f, 0f);
        }

        // end when animation trigger fires
        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }

    public override void Exit()
    {
        base.Exit();

        enemy.SetVelocity(0f, 0f);
        enemy.lastTimeAttacked = Time.time;

        // clear super-armor flag
        enemy.IsAttacking = false;
    }
}
