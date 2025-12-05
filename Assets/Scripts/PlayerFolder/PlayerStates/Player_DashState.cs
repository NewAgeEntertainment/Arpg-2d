// Player_DashState.cs
using Rewired;
using UnityEngine;

public class Player_DashState : PlayerState
{
    private Vector2 dashDir;

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    // 🔹 Helper: use Entity_Health.IsDead so dash never overrides death.
    private bool IsPlayerDead()
    {
        var hp = player.health as Entity_Health;    // Player_Health inherits Entity_Health
        return hp != null && hp.IsDead;
    }

    public override void Enter()
    {
        // If we somehow try to start a dash while already dead,
        // just bounce back to idle (or whichever state requested it).
        if (IsPlayerDead())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        base.Enter();

        // ---------- 1) Read raw input at the exact dash frame ----------
        Vector2 rawInput = Vector2.zero;
        if (rPlayer != null && ReInput.isReady)
        {
            float x = rPlayer.GetAxisRaw("Horizontal");
            float y = rPlayer.GetAxisRaw("Vertical");
            rawInput = new Vector2(x, y);
        }

        // ---------- 2) Get facing direction ----------
        Vector2 face = player.lastMoveDirection.sqrMagnitude > 0.0001f
            ? player.lastMoveDirection.normalized
            : Vector2.down;

        Vector2 dash;

        // Small deadzone so tiny input doesn’t override facing
        const float inputDeadzoneSq = 0.1f * 0.1f;

        if (rawInput.sqrMagnitude > inputDeadzoneSq)
        {
            Vector2 inDir = rawInput.normalized;
            float dot = Vector2.Dot(inDir, face);

            // If stick is mostly opposite of facing -> backdash
            if (dot < -0.5f)
                dash = -face;
            else
                dash = inDir;
        }
        else
        {
            // No meaningful input: dash forward (facing)
            dash = face;
        }

        if (dash.sqrMagnitude < 0.0001f)
            dash = Vector2.down;

        dashDir = dash.normalized;

        // Update player facing for other systems
        player.lastMoveDirection = dashDir;
        player.currentDir = dashDir;

        // ---------- 3) Skill gate (cooldown / mana) ----------
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

        // ---------- 4) I-frames + VFX ----------
        float duration = player.dashDuration;
        (player.health as Player_Health)
            ?.GrantInvulnerabilityFor("Dash", duration + 0.05f);

        skillManager.dash.OnStartEffect();
        player.vfx?.DoImageEchoEffect(duration);

        // ---------- 5) Set dash timer ----------
        stateTimer = duration;

        // ---------- 6) Lock animation facing to dash direction ----------
        if (anim != null)
        {
            Vector2 n = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : face;
            anim.SetFloat("xInput", Mathf.Round(n.x));
            anim.SetFloat("yInput", Mathf.Round(n.y));
        }
    }

    public override void Update()
    {
        // 🛑 If we died during the dash, stop dash logic and let the death state take over.
        if (IsPlayerDead())
        {
            (player.health as Player_Health)?.RemoveInvulnerability("Dash");
            if (skillManager != null && skillManager.dash != null)
                skillManager.dash.OnEndEffect();

            player.SetVelocity(0f, 0f);
            return; // Death flow (Entity_Health / Player_DeathState) runs independently.
        }

        // Instead of base.Update(), do the minimal state work:
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters(); // if you need it; otherwise you can omit

        // Constant dash velocity
        player.SetVelocity(player.dashSpeed * dashDir.x, player.dashSpeed * dashDir.y);

        // Keep anim facing dashDir
        if (anim != null)
        {
            Vector2 n = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : player.lastMoveDirection;
            anim.SetFloat("xInput", Mathf.Round(n.x));
            anim.SetFloat("yInput", Mathf.Round(n.y));
        }

        // End dash when timer runs out (only if we're still alive)
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

    private void UpdateDashAnimFacing()
    {
        if (anim == null) return;

        // face exactly where we’re dashing
        Vector2 n = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : player.lastMoveDirection;
        anim.SetFloat("xInput", Mathf.Round(n.x));
        anim.SetFloat("yInput", Mathf.Round(n.y));
    }
}
