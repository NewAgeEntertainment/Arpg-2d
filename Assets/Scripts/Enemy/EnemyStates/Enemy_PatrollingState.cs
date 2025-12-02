using UnityEngine;

public class Enemy_PatrollingState : Enemy_GroundedState
{
    private const float reachTolerance = 0.1f;

    public Enemy_PatrollingState(Enemy enemy, StateMachine stateMachine, string animBoolName)
        : base(enemy, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // No patrol points → immediately go back to idle
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        // Clamp index
        if (enemy.currentPatrolIndex < 0 || enemy.currentPatrolIndex >= enemy.patrolPoints.Length)
            enemy.currentPatrolIndex = 0;

        enemy.target = enemy.patrolPoints[enemy.currentPatrolIndex];

        // Face towards current target
        Vector2 dir = ((Vector2)enemy.target - (Vector2)enemy.transform.position).normalized;
        enemy.currentDirection = dir;
        enemy.SetFacing(dir);
    }

    public override void Update()
    {
        base.Update();

        // If player detected, battle state takes over (your existing logic in Enemy_GroundedState)
        // Enemy_GroundedState.Update() already checks PlayerDetected() and switches to battle.

        // If we got paused (during SetPatrolPoint), stop but stay in this state
        if (enemy.isPaused)
        {
            enemy.SetZeroVelocity();
            return;
        }

        // Safety: if patrol data vanished, go idle
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        Vector2 pos = enemy.transform.position;
        Vector2 toTarget = enemy.target - pos;
        float dist = toTarget.magnitude;

        // Reached target?
        if (dist <= reachTolerance)
        {
            enemy.SetZeroVelocity();
            enemy.StartCoroutine(enemy.SetPatrolPoint());
            return;
        }

        // Move towards target
        Vector2 dir = toTarget.normalized;
        enemy.currentDirection = dir;
        enemy.SetVelocity(dir.x * enemy.moveSpeed, dir.y * enemy.moveSpeed);

        // Update anim facing
        enemy.anim.SetFloat("xInput", dir.x);
        enemy.anim.SetFloat("yInput", dir.y);
    }

    public override void Exit()
    {
        base.Exit();
        enemy.SetZeroVelocity();
    }
}
