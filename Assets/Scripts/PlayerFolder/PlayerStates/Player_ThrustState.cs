using UnityEngine;

public class Player_ThrustState : PlayerState
{
    private Vector2 thrustDir;

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 1) Resolve thrust direction: current input if present, else last facing, else down
        thrustDir = moveInput.sqrMagnitude > 0.0001f ? moveInput : player.lastMoveDirection;
        if (thrustDir.sqrMagnitude < 0.0001f) thrustDir = Vector2.down;
        thrustDir = thrustDir.normalized;

        // 2) Skill gate & commit (spend mana, start UI cooldown)
        if (skillManager == null || skillManager.thrust == null || !skillManager.thrust.CanUseSkillCheck(out var why))
        {
            Debug.LogWarning($"[Thrust] blocked in Enter:");
            stateMachine.ChangeState(player.idleState);
            return;
        }
        if (!skillManager.thrust.CommitUse())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 3) (Optional) brief i-frames during thrust
        // (player.health as Player_Health)?.GrantInvulnerabilityFor("Thrust", player.ThrustDuration + 0.05f);

        // 4) VFX / SFX hooks
        skillManager.thrust.OnStartEffect();

        // 5) Duration
        stateTimer = player.ThrustDuration;

        // Stamp animator facing (if your anim needs it)
        anim.SetFloat("xInput", Mathf.Round(thrustDir.x));
        anim.SetFloat("yInput", Mathf.Round(thrustDir.y));
    }

    public override void Update()
    {
        base.Update();

        // Constant push while thrusting
        player.SetVelocity(player.ThrustSpeed * thrustDir.x, player.ThrustSpeed * thrustDir.y);

        if (stateTimer < 0f)
            stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();

        // Clear i-frames if you enabled them
        // (player.health as Player_Health)?.RemoveInvulnerability("Thrust");

        skillManager?.thrust?.OnEndEffect();
        player.SetVelocity(0f, 0f);
    }

    private void CancelThrustIfNeeded() { /* hook for cancels */ }
}
