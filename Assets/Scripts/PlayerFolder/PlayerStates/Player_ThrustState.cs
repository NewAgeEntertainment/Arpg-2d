using UnityEngine;
using Rewired;

public class Player_ThrustState : PlayerState
{
    private Vector2 thrustDir;

    // Fallback search radius if we can’t get one from Player
    private const float fallbackAutoAimRadius = 4f;

    // Deadzone for deciding if player is really aiming with the stick
    private const float stickDeadzone = 0.25f; // adjust to taste

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // ---------- 1) Read fresh stick input from Rewired ----------
        Vector2 inputDir = Vector2.zero;
        if (rPlayer != null && ReInput.isReady)
        {
            float x = rPlayer.GetAxisRaw("Horizontal");
            float y = rPlayer.GetAxisRaw("Vertical");
            inputDir = new Vector2(x, y);
        }

        bool hasStickInput = inputDir.sqrMagnitude > stickDeadzone * stickDeadzone;

        // ---------- 2) Decide initial thrust direction ----------
        if (hasStickInput)
        {
            // 🟢 Stick pressed: go EXACTLY where the player aimed (no auto-aim)
            thrustDir = inputDir.normalized;
        }
        else
        {
            // 🔵 No stick input: start from last facing or down
            Vector2 baseDir = player.lastMoveDirection.sqrMagnitude > 0.01f
                ? player.lastMoveDirection.normalized
                : Vector2.down;

            thrustDir = baseDir;
        }

        // Safety fallback
        if (thrustDir.sqrMagnitude < 0.0001f)
            thrustDir = Vector2.down;

        // ---------- 3) Compute search origin for thrust target ----------
        Vector2 originForSearch;

        if (player.ThrustTargetCheck != null)
        {
            // place the thrust target point in front of us by adjustable offset
            player.ThrustTargetCheck.localPosition =
                (Vector3)(thrustDir.normalized * player.ThrustTargetOffset);

            originForSearch = player.ThrustTargetCheck.position;
        }
        else
        {
            // no explicit transform; still offset search in front of player
            originForSearch =
                (Vector2)player.transform.position +
                thrustDir.normalized * player.ThrustTargetOffset;
        }

        // ---------- 4) Auto-aim ONLY when stick is NOT pressed ----------
        if (!hasStickInput)
        {
            float radius = player.ThrustTargetRadius > 0f
                ? player.ThrustTargetRadius
                : fallbackAutoAimRadius;

            LayerMask mask = player.ThrustTargetMask.value != 0
                ? player.ThrustTargetMask
                : (player.combat != null ? player.combat.TargetLayerMask : ~0);

            Collider2D[] hits = Physics2D.OverlapCircleAll(originForSearch, radius, mask);

            Transform best = null;
            float bestDistSqr = float.MaxValue;

            if (hits != null)
            {
                foreach (var c in hits)
                {
                    if (c == null) continue;
                    if (c.transform == player.transform) continue; // ignore self

                    Vector2 toTarget = (Vector2)c.transform.position - (Vector2)player.transform.position;
                    float distSqr = toTarget.sqrMagnitude;
                    if (distSqr < 0.0001f) continue;

                    if (distSqr < bestDistSqr)
                    {
                        bestDistSqr = distSqr;
                        best = c.transform;
                    }
                }
            }

            // 🔒 If we found an enemy, lock directly toward it
            if (best != null)
            {
                Vector2 toBest = (Vector2)best.position - (Vector2)player.transform.position;
                if (toBest.sqrMagnitude > 0.0001f)
                    thrustDir = toBest.normalized;
            }
        }

        // ---------- 5) Share facing with the rest of the systems ----------
        player.currentDir = thrustDir;
        player.lastMoveDirection = thrustDir;

        // ---------- 6) Skill gate & commit ----------
        if (skillManager == null || skillManager.thrust == null)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (!skillManager.thrust.CanUseSkillCheck(out var reason) || !skillManager.thrust.CommitUse())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // ---------- 7) VFX / i-frames / duration ----------
        skillManager.thrust.OnStartEffect();

        // 🛡️ Make player invulnerable for the whole thrust (like Dash)
        float duration = player.ThrustDuration;
        (player.health as Player_Health)
            ?.GrantInvulnerabilityFor("Thrust", duration + 0.05f);

        stateTimer = duration;

        // Animator facing
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

        // 🛡️ Clear i-frames for thrust (mirrors DashState.Exit)
        (player.health as Player_Health)?.RemoveInvulnerability("Thrust");

        skillManager?.thrust?.OnEndEffect();
        player.SetVelocity(0f, 0f);
    }
}
