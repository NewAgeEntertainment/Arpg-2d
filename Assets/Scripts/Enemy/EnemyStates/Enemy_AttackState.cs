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

        // Hard-stop any residual motion (important with your persistent desired-velocity system)
        enemy.SetVelocity(0f, 0f);

        // Face the player & pick lunge direction
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

        // Start the lunge timer
        lungeTimer = Mathf.Max(0f, enemy.attackLungeDuration);
    }

    public override void Update()
    {
        base.Update();

        // Keep looking at the player while attacking
        var p = enemy.GetPlayerReference();
        if (p != null)
            enemy.SetFacing(p.position - enemy.transform.position);

        // --- Adjustable lunge window ---
        if (lungeTimer > 0f)
        {
            float duration = Mathf.Max(0.0001f, enemy.attackLungeDuration);
            float t = 1f - (lungeTimer / duration);            // 0 -> 1 over the lunge
            float mult = 1f;

            Vector2 v = lungeDir * enemy.attackLungeSpeed * mult;
            enemy.SetVelocity(v.x, v.y);

            lungeTimer -= Time.deltaTime;
            if (lungeTimer <= 0f)
            {
                // After the lunge window, keep the enemy locked in place until the attack ends
                enemy.SetVelocity(0f, 0f);
            }
        }
        else
        {
            // Movement locked during the rest of the attack animation
            enemy.SetVelocity(0f, 0f);
        }

        // Animator will call AnimationTrigger() -> sets triggerCalled
        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }

    public override void Exit()
    {
        base.Exit();
        // Safety: stop on exit and stamp last attack time
        enemy.SetVelocity(0f, 0f);
        enemy.lastTimeAttacked = Time.time;
    }
}
