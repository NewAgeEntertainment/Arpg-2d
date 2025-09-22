using UnityEngine;

public class Player_DashState : PlayerState
{
    private Vector2 dashDir;

    [SerializeField] private float stickDeadzone = 0.25f; // input must exceed this to override facing

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 1) Resolve dash direction: input if strong enough, else facing (currentDir/lastMoveDirection), else down.
        dashDir = ResolveDashDirection();

        // 2) Can we dash? Spend resources & start cooldown (if you use a skill system)
        if (!skillManager.dash.CanUseSkillCheck(out var why))
        {
            // Optional: play denied SFX
            stateMachine.ChangeState(player.idleState);
            return;
        }
        if (!skillManager.dash.CommitUse())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 3) VFX/SFX hooks
        skillManager.dash.OnStartEffect();
        player.vfx?.DoImageEchoEffect(player.dashDuration);

        // 4) run for the configured duration
        stateTimer = player.dashDuration;

        // Optional: stamp animator dir (rounded 8-way/4-way as your controller expects)
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

        skillManager.dash.OnEndEffect();
        player.SetVelocity(0f, 0f);
    }

    private Vector2 ResolveDashDirection()
    {
        // Prefer live input if it beats the deadzone
        Vector2 input = player.moveInput;
        if (input.sqrMagnitude >= (stickDeadzone * stickDeadzone))
            return input.normalized;

        // Otherwise, use facing. Prefer currentDir (kept by Idle/Move), else lastMoveDirection, else down.
        Vector2 facing = player.currentDir.sqrMagnitude > 0.0001f
            ? player.currentDir
            : (player.lastMoveDirection.sqrMagnitude > 0.0001f ? player.lastMoveDirection : Vector2.down);

        return facing.normalized;
    }
}
