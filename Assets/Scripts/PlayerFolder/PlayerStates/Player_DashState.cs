// Player_DashState.cs
using UnityEngine;

public class Player_DashState : PlayerState
{
    private Vector2 dashDir;

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 1) Resolve dash direction: input if present, else facing, else down
        dashDir = moveInput.sqrMagnitude > 0.0001f ? moveInput : player.lastMoveDirection;
        if (dashDir.sqrMagnitude < 0.0001f) dashDir = Vector2.down;
        dashDir = dashDir.normalized;

        // 2) Skill gate (if using a skill system)
        if (!skillManager.dash.CanUseSkillCheck(out var why))
        {
            Debug.LogWarning($"[Dash] blocked in Enter: {why}");
            stateMachine.ChangeState(player.idleState);
            return;
        }
        if (!skillManager.dash.CommitUse())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 3) Grant i-frames for dash (+ tiny buffer)
        (player.health as Player_Health)?.GrantInvulnerabilityFor("Dash", player.dashDuration + 0.05f);

        // 4) Effects / VFX
        skillManager.dash.OnStartEffect();
        player.vfx?.DoImageEchoEffect(player.dashDuration);

        // 5) run for duration
        stateTimer = player.dashDuration;

        // (Optional) stamp animator dir
        anim.SetFloat("xInput", Mathf.Round(dashDir.x));
        anim.SetFloat("yInput", Mathf.Round(dashDir.y));
    }

    public override void Update()
    {
        base.Update();

        // constant velocity during dash
        player.SetVelocity(player.dashSpeed * dashDir.x, player.dashSpeed * dashDir.y);

        if (stateTimer < 0f)
            stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();

        // Ensure i-frames are cleared (safe even if timer already removed them)
        (player.health as Player_Health)?.RemoveInvulnerability("Dash");

        skillManager.dash.OnEndEffect();
        player.SetVelocity(0f, 0f);
    }
}
