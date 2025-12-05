using UnityEngine;

public class Player_ThrustState : PlayerState
{
    private Vector2 thrustDir;

    // Fallback search radius if combat doesn't give us one
    private const float fallbackAutoAimRadius = 4f;

    // How tight the cone is in front of the input (0° = only dead center, 180° = full circle)
    // 0.5 ≈ 60° cone; 0.7 ≈ 45° cone. Adjust to taste.
    private const float minDotForLock = 0.5f;

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 1) Base direction from input or last facing
        thrustDir = moveInput.sqrMagnitude > 0.0001f ? moveInput : player.lastMoveDirection;
        if (thrustDir.sqrMagnitude < 0.0001f)
            thrustDir = Vector2.down;
        thrustDir.Normalize();

        // 2) Soft aim assist: find enemies roughly in front of thrustDir
        Vector2 aimDir = thrustDir;

        // Ask combat for a target in the approximate aim cone (but only for assist, not lock-on)
        Transform softTarget = player.combat.GetSoftAimTarget(
            player.transform.position,
            thrustDir,
            player.ThrustSoftAimAngle,   // e.g. 45 degrees
            player.ThrustSoftAimRange    // e.g. 6–8 units
        );

        if (softTarget != null)
        {
            // Nudge thrust direction toward that target, but keep some of the original
            Vector2 toTarget = (softTarget.position - player.transform.position);
            if (toTarget.sqrMagnitude > 0.001f)
            {
                toTarget.Normalize();

                // Mix: 70% original aim, 30% toward target → feels like Ys magnet
                float blend = 0.3f;
                Vector2 blended = Vector2.Lerp(thrustDir, toTarget, blend);
                if (blended.sqrMagnitude > 0.0001f)
                    thrustDir = blended.normalized;
            }
        }

        // 3) Skill gate & commit (your existing code)
        if (skillManager == null || skillManager.thrust == null)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (!skillManager.thrust.CanUseSkillCheck(out var why) || !skillManager.thrust.CommitUse())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 4) Optional i-frames, VFX, etc.
        skillManager.thrust.OnStartEffect();
        stateTimer = player.ThrustDuration;

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

    // ------------------- Helpers -------------------

    /// <summary>
    /// What direction is the player actually aiming? 
    /// Prefer stick input; fallback to last facing; fallback to down.
    /// </summary>
    private Vector2 GetPlayerAimDirection()
    {
        // moveInput comes from PlayerState base
        if (moveInput.sqrMagnitude > 0.0001f)
            return moveInput.normalized;

        if (player.lastMoveDirection.sqrMagnitude > 0.0001f)
            return player.lastMoveDirection.normalized;

        return Vector2.down;
    }

    /// <summary>
    /// Look for enemies in a cone in front of aimDir, and return a direction aimed at the 
    /// best candidate. Prioritizes ANGLE first (what the player is pointing at), not just closest.
    /// </summary>
    private Vector2 GetDirectionalAimDirection(Vector2 aimDir)
    {
        if (aimDir.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        var combat = player.GetComponent<Player_Combat>();
        if (combat == null)
            return Vector2.zero;

        float radius = combat.TargetCheckRadius > 0 ? combat.TargetCheckRadius : fallbackAutoAimRadius;
        LayerMask mask = combat.TargetLayerMask;

        Vector2 origin = player.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, mask);
        if (hits == null || hits.Length == 0)
            return Vector2.zero;

        Transform best = null;
        float bestDot = minDotForLock;   // must be at least this aligned
        float bestDistSqr = float.MaxValue;

        foreach (var c in hits)
        {
            if (c == null) continue;
            if (c.transform == player.transform) continue; // ignore self just in case

            Vector2 toTarget = (Vector2)c.transform.position - origin;
            float distSqr = toTarget.sqrMagnitude;
            if (distSqr < 0.0001f) continue;

            Vector2 dirToTarget = toTarget.normalized;
            float dot = Vector2.Dot(aimDir, dirToTarget);

            // reject targets outside the cone
            if (dot < minDotForLock)
                continue;

            // Higher dot = more in front of stick; distance breaks ties
            bool isBetter = false;

            if (dot > bestDot + 0.001f)
            {
                isBetter = true;
            }
            else if (Mathf.Approximately(dot, bestDot) && distSqr < bestDistSqr)
            {
                isBetter = true;
            }

            if (isBetter)
            {
                bestDot = dot;
                bestDistSqr = distSqr;
                best = c.transform;
            }
        }

        if (best == null)
            return Vector2.zero;

        // Snap thrust direction exactly toward the chosen target
        return ((Vector2)best.position - origin).normalized;
    }
}
