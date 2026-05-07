using UnityEngine;
using Rewired;

public class Player_BasicAttackState : PlayerState
{
    // ---- Timing ----
    private float attackVelocityTimer;
    private float lastTimeAttacked;

    // ---- Input / chaining ----
    private bool comboAttackQueued;
    private float lastQueueTime;
    private const float attackQueueCooldown = 0.08f;

    // ---- Direction locked per swing ----
    private Vector2 lastAttackDir;

    // ---- Combo ----
    private int comboIndex = 1;
    private int comboLimit = 3;
    private const int FirstComboIndex = 1;

    // ---- Input (Rewired) ----
    private const string AttackActionName = "Attack";
    private const string DashActionName = "Dash";

    // ---- Lunge tuning ----
    private const float lungeDuration = 0.10f;

    private int enteredFrame;

    private const string SkillModifierActionName = "SkillModifier";


    public Player_BasicAttackState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
        // Align combo length with configured bursts
        if (player.attackMovement != null && player.attackMovement.Length > 0)
        {
            if (comboLimit != player.attackMovement.Length)
            {
                Debug.LogWarning("Adjusted combo limit to match attackMovement length.");
                comboLimit = player.attackMovement.Length;
            }
        }
        else
        {
            comboLimit = 1; // fail-safe
        }
    }

    public override void Enter()
    {
        base.Enter();

        enteredFrame = Time.frameCount;

        comboAttackQueued = false;

        ResetComboIndexIfNeeded();
        SyncAttackSpeed();

        // ------------------ LOCK + AUTO-AIM ATTACK DIRECTION ------------------

        // 1) Base direction from movement input or last facing
        bool hasInput = player.moveInput.sqrMagnitude > 0.0001f;

        Vector2 baseDir;
        if (hasInput)
        {
            baseDir = player.moveInput.normalized;
        }
        else if (player.lastMoveDirection.sqrMagnitude > 0.01f)
        {
            baseDir = player.lastMoveDirection.normalized;
        }
        else
        {
            baseDir = Vector2.down;
        }

        Vector2 dir = baseDir;

        // 2) Soft / hard aim assist using Player_Combat.GetSoftAimTarget
        if (player.combat != null)
        {
            // If no input → use almost full circle (like thrust soft lock when neutral)
            float angleToUse = hasInput ? player.ThrustSoftAimAngle : 179.9f;
            float rangeToUse = player.ThrustSoftAimRange;

            Transform softTarget = player.combat.GetSoftAimTarget(
                player.transform.position,
                baseDir,
                angleToUse,
                rangeToUse
            );

            if (softTarget != null)
            {
                Vector2 toTarget = (Vector2)softTarget.position - (Vector2)player.transform.position;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    Vector2 targetDir = toTarget.normalized;

                    if (hasInput)
                    {
                        // Stick held → bias toward what the player is pressing,
                        // but gently nudge toward the enemy.
                        const float blend = 0.3f; // 0 = no assist, 1 = full lock
                        Vector2 blended = Vector2.Lerp(baseDir, targetDir, blend);
                        if (blended.sqrMagnitude > 0.0001f)
                            dir = blended.normalized;
                        else
                            dir = targetDir;
                    }
                    else
                    {
                        // No input at all → hard lock directly at target
                        dir = targetDir;
                    }
                }
            }
        }

        lastAttackDir = dir;

        // Make sure combat hitpoints use this facing
        player.currentDir = lastAttackDir;
        player.lastMoveDirection = lastAttackDir;

        // -----------------------------------------------------------

        anim.SetInteger("basicAttackIndex", comboIndex);
        SetAnimatorDirectionParams(lastAttackDir);

        ApplyAttackVelocity();
    }

    public override void Update()
    {
        base.Update();

        // Let player cancel attack into dash immediately.
        if (TryDashCancel())
            return;

        HandleAttackVelocity();

        if (rPlayer != null && ReInput.isReady)
        {
            // While holding SkillModifier, do not allow basic attack chaining/queueing
            if (rPlayer.GetButton(SkillModifierActionName))
                return;

            bool attackPressedThisFrame = rPlayer.GetButtonDown(AttackActionName);
            if (!triggerCalled && attackPressedThisFrame)
                TryQueueNextAttack();
        }

        if (triggerCalled)
            HandleStateExit();
    }

    private bool TryDashCancel()
    {
        if (rPlayer == null || !ReInput.isReady)
            return false;

        if (!rPlayer.GetButtonDown(DashActionName))
            return false;

        if (player == null || player.skillManager == null || player.skillManager.dash == null)
            return false;

        if (!player.skillManager.dash.CanUseSkillCheck(out _))
            return false;

        stateMachine.ChangeState(player.dashState);
        return true;
    }

    public override void Exit()
    {
        base.Exit();

        comboIndex = (comboIndex < comboLimit) ? comboIndex + 1 : FirstComboIndex;

        player.lastMoveDirection = lastAttackDir;

        lastTimeAttacked = Time.time;
    }

    // ---------------- Internals ----------------

    private void TryQueueNextAttack()
    {
        if (comboAttackQueued) return;
        if (comboIndex >= comboLimit) return;
        if (Time.time - lastQueueTime < attackQueueCooldown) return;

        comboAttackQueued = true;
        lastQueueTime = Time.time;
    }

    private void HandleStateExit()
    {
        bool canChain = comboIndex < comboLimit;

        if (canChain && comboAttackQueued)
        {
            comboAttackQueued = false;
            anim.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;

        if (attackVelocityTimer <= 0f)
            player.SetVelocity(0f, 0f);
    }

    private void ApplyAttackVelocity()
    {
        int idx = Mathf.Clamp(comboIndex - 1, 0, Mathf.Max(0, comboLimit - 1));

        float burst = 0f;
        if (player.attackMovement != null && player.attackMovement.Length > 0)
            burst = player.attackMovement[Mathf.Clamp(idx, 0, player.attackMovement.Length - 1)];

        player.SetVelocity(burst * lastAttackDir.x, burst * lastAttackDir.y);

        attackVelocityTimer = lungeDuration;
    }

    private void ResetComboIndexIfNeeded()
    {
        if (Time.time > lastTimeAttacked + player.comboResetTime)
            comboIndex = FirstComboIndex;

        if (comboIndex > comboLimit || comboIndex < FirstComboIndex)
            comboIndex = FirstComboIndex;
    }

    private void SetAnimatorDirectionParams(Vector2 dir)
    {
        Vector2 n = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.down;
        anim.SetFloat("xInput", Mathf.Round(n.x));
        anim.SetFloat("yInput", Mathf.Round(n.y));
    }
}
