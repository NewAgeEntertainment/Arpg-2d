using UnityEngine;

public class Player_DashState : PlayerState
{
    private Vector2 dashDir;

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 1) pick a direction (like Thrust): current input, else last facing, else down
        dashDir = moveInput.sqrMagnitude > 0.0001f ? moveInput : player.lastMoveDirection;
        if (dashDir.sqrMagnitude < 0.0001f) dashDir = Vector2.down;
        dashDir = dashDir.normalized;

        // 2) spend mana & start cooldown *only if* dash can be used
        if (!skillManager.dash.CanUseSkillCheck(out var why))
        {
            Debug.LogWarning($"[Dash] blocked in Enter: {why}");
            stateMachine.ChangeState(player.idleState);
            return;
        }
        if (!skillManager.dash.CommitUse())
        {
            // Mana spend failed or something else blocked last second
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 3) effects (same timing as before)
        skillManager.dash.OnStartEffect();
        player.vfx?.DoImageEchoEffect(player.dashDuration);

        // 4) run for the configured duration
        stateTimer = player.dashDuration;
    }

    public override void Update()
    {
        base.Update();

        // Constant push like Thrust: velocity = speed * fixed direction
        player.SetVelocity(player.dashSpeed * dashDir.x, player.dashSpeed * dashDir.y);

        if (stateTimer < 0f)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();

        skillManager.dash.OnEndEffect();
        player.SetVelocity(0f, 0f);
    }

    private void CancelDashIfNeeded() { /* hook if you add cancels */ }
}
