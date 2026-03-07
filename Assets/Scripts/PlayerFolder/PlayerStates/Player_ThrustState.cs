using UnityEngine;
using Rewired;

public class Player_ThrustState : PlayerState
{
    private Vector2 thrustDir;

    private const float fallbackAutoAimRadius = 4f;
    private const float stickDeadzone = 0.25f;

    // AudioDatabaseSO audioName
    private const string ThrustSfxName = "PlayerThrust";

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        Vector2 inputDir = Vector2.zero;
        if (rPlayer != null && ReInput.isReady)
        {
            float x = rPlayer.GetAxisRaw("Horizontal");
            float y = rPlayer.GetAxisRaw("Vertical");
            inputDir = new Vector2(x, y);
        }

        bool hasStickInput = inputDir.sqrMagnitude > stickDeadzone * stickDeadzone;

        if (hasStickInput)
        {
            thrustDir = inputDir.normalized;
        }
        else
        {
            Vector2 baseDir = player.lastMoveDirection.sqrMagnitude > 0.01f
                ? player.lastMoveDirection.normalized
                : Vector2.down;

            thrustDir = baseDir;
        }

        if (thrustDir.sqrMagnitude < 0.0001f)
            thrustDir = Vector2.down;

        Vector2 originForSearch;

        if (player.ThrustTargetCheck != null)
        {
            player.ThrustTargetCheck.localPosition =
                (Vector3)(thrustDir.normalized * player.ThrustTargetOffset);

            originForSearch = player.ThrustTargetCheck.position;
        }
        else
        {
            originForSearch =
                (Vector2)player.transform.position +
                thrustDir.normalized * player.ThrustTargetOffset;
        }

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
                    if (c.transform == player.transform) continue;

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

            if (best != null)
            {
                Vector2 toBest = (Vector2)best.position - (Vector2)player.transform.position;
                if (toBest.sqrMagnitude > 0.0001f)
                    thrustDir = toBest.normalized;
            }
        }

        player.currentDir = thrustDir;
        player.lastMoveDirection = thrustDir;

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

        skillManager.thrust.OnStartEffect();

        // 🔊 Play thrust sound
        PlayThrustSfx();

        float duration = player.ThrustDuration;
        (player.health as Player_Health)?.GrantInvulnerabilityFor("Thrust", duration + 0.05f);

        stateTimer = duration;

        anim.SetFloat("xInput", Mathf.Round(thrustDir.x));
        anim.SetFloat("yInput", Mathf.Round(thrustDir.y));
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(player.ThrustSpeed * thrustDir.x, player.ThrustSpeed * thrustDir.y);

        if (stateTimer < 0f)
            stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();

        (player.health as Player_Health)?.RemoveInvulnerability("Thrust");

        skillManager?.thrust?.OnEndEffect();
        player.SetVelocity(0f, 0f);
    }

    private void PlayThrustSfx()
    {
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.PlayGlobalSFX(ThrustSfxName);
    }
}