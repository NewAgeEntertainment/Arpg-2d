using UnityEngine;
using Rewired;

public class Player_ThrustState : PlayerState
{
    private Vector2 thrustDir;

    private bool hasThrustStopPoint;
    private bool stoppedAtEnemy;
    private Vector2 thrustStartPosition;
    private Vector2 thrustStopPosition;

    private const float fallbackAutoAimRadius = 4f;
    private const float stickDeadzone = 0.25f;

    // How far away from the enemy the player should stop.
    private const float stopDistanceFromEnemy = 0.75f;

    // Prevents tiny movement from feeling broken if enemy is very close.
    private const float minThrustDistance = 0.2f;

    private const string ThrustSfxName = "PlayerThrust";

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        thrustStartPosition = player.transform.position;
        hasThrustStopPoint = false;
        stoppedAtEnemy = false;

        Transform lockedEnemy = null;

        Vector2 inputDir = GetRawMoveInput();
        bool hasStickInput = inputDir.sqrMagnitude > stickDeadzone * stickDeadzone;

        // -------------------------------------------------------
        // 1. Holding direction:
        // Use input direction, but snap to enemy if aimed near one.
        // -------------------------------------------------------
        if (hasStickInput)
        {
            thrustDir = inputDir.normalized;

            Transform aimedEnemy = FindEnemyInAimDirection(thrustDir);

            if (aimedEnemy != null)
            {
                lockedEnemy = aimedEnemy;

                Vector2 toEnemy =
                    (Vector2)aimedEnemy.position -
                    (Vector2)player.transform.position;

                if (toEnemy.sqrMagnitude > 0.0001f)
                    thrustDir = toEnemy.normalized;
            }
        }
        else
        {
            // -------------------------------------------------------
            // 2. No direction:
            // Auto-aim to closest enemy.
            // -------------------------------------------------------
            Transform closestEnemy = FindClosestEnemy();

            if (closestEnemy != null)
            {
                lockedEnemy = closestEnemy;

                Vector2 toEnemy =
                    (Vector2)closestEnemy.position -
                    (Vector2)player.transform.position;

                if (toEnemy.sqrMagnitude > 0.0001f)
                    thrustDir = toEnemy.normalized;
                else
                    thrustDir = GetFallbackFacingDirection();
            }
            else
            {
                // -------------------------------------------------------
                // 3. No enemy:
                // Use last facing direction.
                // -------------------------------------------------------
                thrustDir = GetFallbackFacingDirection();
            }
        }

        if (thrustDir.sqrMagnitude < 0.0001f)
            thrustDir = Vector2.down;

        SetupStopPointIfEnemyLocked(lockedEnemy);

        player.currentDir = thrustDir;
        player.lastMoveDirection = thrustDir;

        // Move the target check safely.
        // Do NOT allow this to move the actual Player transform.
        if (player.ThrustTargetCheck != null &&
            player.ThrustTargetCheck != player.transform &&
            player.ThrustTargetCheck.IsChildOf(player.transform))
        {
            player.ThrustTargetCheck.localPosition =
                (Vector3)(thrustDir.normalized * player.ThrustTargetOffset);
        }
        else if (player.ThrustTargetCheck == player.transform)
        {
            Debug.LogWarning("[Thrust] ThrustTargetCheck is assigned to the Player transform. Assign a child object instead.");
        }

        // -------------------------------------------------------
        // Skill check
        // -------------------------------------------------------
        if (skillManager == null || skillManager.thrust == null)
        {
            Debug.LogWarning("[Thrust] No thrust skill found on Player_SkillManager.");
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (!skillManager.thrust.CanUseSkillCheck(out var reason))
        {
            Debug.Log($"[Thrust] Blocked: {reason}");
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (!skillManager.thrust.CommitUse())
        {
            Debug.Log("[Thrust] CommitUse failed.");
            stateMachine.ChangeState(player.idleState);
            return;
        }

        skillManager.thrust.OnStartEffect();

        PlayThrustSfx();

        float duration = player.ThrustDuration;

        (player.health as Player_Health)?.GrantInvulnerabilityFor(
            "Thrust",
            duration + 0.05f
        );

        stateTimer = duration;

        UpdateThrustAnimationFacing();
    }

    public override void Update()
    {
        base.Update();

        // -------------------------------------------------------
        // Stop movement when reaching enemy,
        // but DO NOT exit the state.
        // This allows the full thrust animation and damage frame to play.
        // -------------------------------------------------------
        if (hasThrustStopPoint && !stoppedAtEnemy)
        {
            Vector2 currentPos = player.transform.position;
            Vector2 toStop = thrustStopPosition - currentPos;

            // Close enough to the stop point.
            if (toStop.sqrMagnitude <= 0.08f * 0.08f)
            {
                StopMovementButKeepAnimation();
            }
            else
            {
                // Safety: if we passed the stop point, stop movement.
                Vector2 fromStartToStop = thrustStopPosition - thrustStartPosition;
                Vector2 fromStartToPlayer = currentPos - thrustStartPosition;

                if (fromStartToStop.sqrMagnitude > 0.0001f &&
                    Vector2.Dot(fromStartToPlayer, fromStartToStop) >= fromStartToStop.sqrMagnitude)
                {
                    StopMovementButKeepAnimation();
                }
            }
        }

        if (stoppedAtEnemy)
        {
            player.SetVelocity(0f, 0f);
        }
        else
        {
            player.SetVelocity(
                player.ThrustSpeed * thrustDir.x,
                player.ThrustSpeed * thrustDir.y
            );
        }

        UpdateThrustAnimationFacing();

        // Let the full thrust duration finish.
        // This keeps the animation alive long enough for the damage frame.
        if (stateTimer < 0f)
            stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();

        (player.health as Player_Health)?.RemoveInvulnerability("Thrust");

        skillManager?.thrust?.OnEndEffect();

        player.SetVelocity(0f, 0f);

        hasThrustStopPoint = false;
        stoppedAtEnemy = false;
    }

    private void StopMovementButKeepAnimation()
    {
        stoppedAtEnemy = true;
        hasThrustStopPoint = false;

        player.SetVelocity(0f, 0f);

        Debug.Log("[Thrust] Reached stop point. Movement stopped, animation continues.");
    }

    private Vector2 GetRawMoveInput()
    {
        if (rPlayer == null || !ReInput.isReady)
            return Vector2.zero;

        float x = rPlayer.GetAxisRaw("Horizontal");
        float y = rPlayer.GetAxisRaw("Vertical");

        return new Vector2(x, y);
    }

    private Vector2 GetFallbackFacingDirection()
    {
        if (player.lastMoveDirection.sqrMagnitude > 0.01f)
            return player.lastMoveDirection.normalized;

        return Vector2.down;
    }

    private void SetupStopPointIfEnemyLocked(Transform enemy)
    {
        hasThrustStopPoint = false;

        if (enemy == null)
            return;

        Vector2 playerPos = player.transform.position;
        Vector2 enemyPos = enemy.position;

        Vector2 toEnemy = enemyPos - playerPos;

        if (toEnemy.sqrMagnitude < 0.0001f)
            return;

        float distanceToEnemy = toEnemy.magnitude;
        float travelDistance = distanceToEnemy - stopDistanceFromEnemy;

        if (travelDistance < minThrustDistance)
            travelDistance = minThrustDistance;

        thrustStopPosition = playerPos + thrustDir.normalized * travelDistance;
        hasThrustStopPoint = true;

        Debug.Log(
            $"[Thrust] Locked enemy: {enemy.name}. " +
            $"Distance={distanceToEnemy:F2}, StopDistance={stopDistanceFromEnemy:F2}, Travel={travelDistance:F2}"
        );
    }

    private Transform FindClosestEnemy()
    {
        float radius = player.ThrustTargetRadius > 0f
            ? player.ThrustTargetRadius
            : fallbackAutoAimRadius;

        LayerMask mask = GetTargetMask();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            player.transform.position,
            radius,
            mask
        );

        Transform closest = null;
        float closestDistSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!IsValidEnemyTarget(hit))
                continue;

            Transform targetTransform = GetTargetRoot(hit);

            Vector2 toTarget =
                (Vector2)targetTransform.position -
                (Vector2)player.transform.position;

            float distSqr = toTarget.sqrMagnitude;

            if (distSqr < 0.0001f)
                continue;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = targetTransform;
            }
        }

        return closest;
    }

    private Transform FindEnemyInAimDirection(Vector2 aimDirection)
    {
        float range = player.ThrustSoftAimRange > 0f
            ? player.ThrustSoftAimRange
            : fallbackAutoAimRadius;

        float maxAngle = player.ThrustSoftAimAngle;

        LayerMask mask = GetTargetMask();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            player.transform.position,
            range,
            mask
        );

        Transform best = null;
        float bestScore = float.NegativeInfinity;

        foreach (Collider2D hit in hits)
        {
            if (!IsValidEnemyTarget(hit))
                continue;

            Transform targetTransform = GetTargetRoot(hit);

            Vector2 toTarget =
                (Vector2)targetTransform.position -
                (Vector2)player.transform.position;

            float distance = toTarget.magnitude;

            if (distance < 0.0001f)
                continue;

            Vector2 targetDir = toTarget / distance;

            float dot = Vector2.Dot(aimDirection.normalized, targetDir);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

            if (angle > maxAngle)
                continue;

            // Higher score = more directly aimed + closer.
            float distanceScore = 1f - Mathf.Clamp01(distance / range);
            float score = dot * 2f + distanceScore;

            if (score > bestScore)
            {
                bestScore = score;
                best = targetTransform;
            }
        }

        return best;
    }

    private LayerMask GetTargetMask()
    {
        if (player.ThrustTargetMask.value != 0)
            return player.ThrustTargetMask;

        if (player.combat != null)
            return player.combat.TargetLayerMask;

        return ~0;
    }

    private bool IsValidEnemyTarget(Collider2D hit)
    {
        if (hit == null)
            return false;

        if (hit.transform == player.transform)
            return false;

        if (hit.transform.IsChildOf(player.transform))
            return false;

        IDamageable damageable =
            hit.GetComponent<IDamageable>() ??
            hit.GetComponentInParent<IDamageable>();

        return damageable != null;
    }

    private Transform GetTargetRoot(Collider2D hit)
    {
        Entity entity = hit.GetComponentInParent<Entity>();

        if (entity != null)
            return entity.transform;

        return hit.transform;
    }

    private void UpdateThrustAnimationFacing()
    {
        if (anim == null)
            return;

        Vector2 n = thrustDir.sqrMagnitude > 0.0001f
            ? thrustDir.normalized
            : Vector2.down;

        anim.SetFloat("xInput", Mathf.Round(n.x));
        anim.SetFloat("yInput", Mathf.Round(n.y));
    }

    private void PlayThrustSfx()
    {
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.PlayGlobalSFX(ThrustSfxName);
    }
}