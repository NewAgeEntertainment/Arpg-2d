using UnityEngine;

public class Player_ThrustState : PlayerState
{
    private Vector2 thrustDir;

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // Prefer the latest input; if none, use last facing direction
        thrustDir = moveInput.sqrMagnitude > 0.0001f ? moveInput : player.lastMoveDirection;
        if (thrustDir.sqrMagnitude < 0.0001f) thrustDir = Vector2.down;
        thrustDir = thrustDir.normalized;

        // Start cooldown/effects
        if (skillManager?.thrust != null)
        {
            skillManager.thrust.SetSkillOnCooldown();
            skillManager.thrust.OnStartEffect();
        }

        stateTimer = player.ThrustDuration;
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(player.ThrustSpeed * thrustDir.x, player.ThrustSpeed * thrustDir.y);

        if (stateTimer < 0f)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (skillManager?.thrust != null)
            skillManager.thrust.OnEndEffect();

        player.SetVelocity(0f, 0f);
    }

    private void CancelThrustIfNeeded() { /* hook for cancels */ }
}
