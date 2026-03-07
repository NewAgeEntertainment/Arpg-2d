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

        Vector2 dash;
        const float inputDeadzoneSq = 0.1f * 0.1f;

        if (rawInput.sqrMagnitude > inputDeadzoneSq)
        {
            Vector2 inDir = rawInput.normalized;
            float dot = Vector2.Dot(inDir, face);

            if (dot < -0.5f)
                dash = -face;
            else
                dash = inDir;
        }
        else
        {
            dash = face;
        }

        if (dash.sqrMagnitude < 0.0001f)
            dash = Vector2.down;

        dashDir = dash.normalized;

        player.lastMoveDirection = dashDir;
        player.currentDir = dashDir;

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

        float duration = player.dashDuration;
        (player.health as Player_Health)?.GrantInvulnerabilityFor("Dash", duration + 0.05f);

        skillManager.dash.OnStartEffect();

        // 🔊 Play dash sound here
        PlayDashSfx();

        stateTimer = duration;

        if (anim != null)
        {
            Vector2 n = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : face;
            anim.SetFloat("xInput", Mathf.Round(n.x));
            anim.SetFloat("yInput", Mathf.Round(n.y));
        }
    }

    public override void Update()
    {
        if (IsPlayerDead())
        {
            (player.health as Player_Health)?.RemoveInvulnerability("Dash");
            if (skillManager != null && skillManager.dash != null)
                skillManager.dash.OnEndEffect();

            player.SetVelocity(0f, 0f);
            return;
        }

        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();

        player.SetVelocity(player.dashSpeed * dashDir.x, player.dashSpeed * dashDir.y);

        if (anim != null)
        {
            Vector2 n = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : player.lastMoveDirection;
            anim.SetFloat("xInput", Mathf.Round(n.x));
            anim.SetFloat("yInput", Mathf.Round(n.y));
        }

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
        if (anim == null) return;

        Vector2 n = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : player.lastMoveDirection;
        anim.SetFloat("xInput", Mathf.Round(n.x));
        anim.SetFloat("yInput", Mathf.Round(n.y));
    }
}