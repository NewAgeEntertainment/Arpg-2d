using Rewired;
using UnityEngine;

public class Player_DashState : PlayerState
{
    private Vector2 dashDir;

    // AudioDatabaseSO audioName for the dash sound
    private const string DashSfxName = "PlayerDash";

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    private bool IsPlayerDead()
    {
        var hp = player.health as Entity_Health;
        return hp != null && hp.IsDead;
    }

    public override void Enter()
    {
        if (IsPlayerDead())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        base.Enter();

        // -------------------------------------------------------
        // DASH DIRECTION
        // -------------------------------------------------------
        // Use current movement input first.
        // This allows: attack -> hold direction -> dash immediately that way.
        Vector2 rawInput = Vector2.zero;

        if (rPlayer != null && ReInput.isReady)
        {
            float x = rPlayer.GetAxisRaw("Horizontal");
            float y = rPlayer.GetAxisRaw("Vertical");
            rawInput = new Vector2(x, y);
        }

        Vector2 face = player.lastMoveDirection.sqrMagnitude > 0.0001f
            ? player.lastMoveDirection.normalized
            : Vector2.down;

        if (rawInput.sqrMagnitude > 0.01f)
            dashDir = rawInput.normalized;
        else
            dashDir = face;

        if (dashDir.sqrMagnitude < 0.0001f)
            dashDir = Vector2.down;

        player.lastMoveDirection = dashDir;
        player.currentDir = dashDir;

        // -------------------------------------------------------
        // DASH SKILL CHECK
        // -------------------------------------------------------
        if (skillManager == null || skillManager.dash == null)
        {
            Debug.LogWarning("[Dash] No dash skill found on Player_SkillManager.");
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (!skillManager.dash.CanUseSkillCheck(out var reason))
        {
            Debug.LogWarning($"[Dash] blocked: {reason}");
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (!skillManager.dash.CommitUse())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // -------------------------------------------------------
        // DASH START
        // -------------------------------------------------------
        float duration = player.dashDuration;

        (player.health as Player_Health)?.GrantInvulnerabilityFor("Dash", duration + 0.05f);

        skillManager.dash.OnStartEffect();

        PlayDashSfx();

        stateTimer = duration;

        UpdateDashAnimFacing();
    }

    public override void Update()
    {
        if (IsPlayerDead())
        {
            (player.health as Player_Health)?.RemoveInvulnerability("Dash");

            if (skillManager != null && skillManager.dash != null)
                skillManager.dash.OnEndEffect();

            player.SetVelocity(0f, 0f);
            stateMachine.ChangeState(player.idleState);
            return;
        }

        stateTimer -= Time.deltaTime;

        player.SetVelocity(
            player.dashSpeed * dashDir.x,
            player.dashSpeed * dashDir.y
        );

        UpdateDashAnimFacing();

        if (stateTimer <= 0f)
            stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();

        (player.health as Player_Health)?.RemoveInvulnerability("Dash");

        if (skillManager != null && skillManager.dash != null)
            skillManager.dash.OnEndEffect();

        player.SetVelocity(0f, 0f);
    }

    private void PlayDashSfx()
    {
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.PlayGlobalSFX(DashSfxName);
    }

    private void UpdateDashAnimFacing()
    {
        if (anim == null)
            return;

        Vector2 n = dashDir.sqrMagnitude > 0.0001f
            ? dashDir.normalized
            : player.lastMoveDirection;

        if (n.sqrMagnitude < 0.0001f)
            n = Vector2.down;

        anim.SetFloat("xInput", Mathf.Round(n.x));
        anim.SetFloat("yInput", Mathf.Round(n.y));
    }
}