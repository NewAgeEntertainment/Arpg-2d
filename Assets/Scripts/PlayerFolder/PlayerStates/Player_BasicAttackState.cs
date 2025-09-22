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
    private const float attackQueueCooldown = 0.08f; // prevents accidental double-queues within the same swing

    // ---- Direction locked per swing ----
    private Vector2 lastAttackDir;

    // ---- Combo ----
    private int comboIndex = 1;
    private int comboLimit = 3;
    private const int FirstComboIndex = 1;

    // ---- Input (Rewired) ----
    // Button-only. Make sure this matches your Rewired Action name.
    private const string AttackActionName = "Attack";

    // ---- Lunge tuning ----
    private const float lungeDuration = 0.10f; // seconds; outside this, velocity is hard-zeroed

    // ---- Debounce so we don't double-consume the same press on the entry frame ----
    private int enteredFrame;

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

        enteredFrame = Time.frameCount;   // ignore AttackDown this frame to avoid double-consume

        // Fresh swing: clear any leftover queue flag
        comboAttackQueued = false;

        ResetComboIndexIfNeeded();
        SyncAttackSpeed();

        // Lock direction for this swing from input or last facing
        Vector2 inputDir = player.moveInput;
        lastAttackDir = (inputDir.sqrMagnitude > 0.01f) ? inputDir.normalized : player.lastMoveDirection;

        // Animator params
        anim.SetInteger("basicAttackIndex", comboIndex);
        SetAnimatorDirectionParams(lastAttackDir);

        // Apply the lunge burst; movement is fully stopped when timer ends
        ApplyAttackVelocity();
    }

    public override void Update()
    {
        base.Update();

        // Stop movement when the lunge window ends
        HandleAttackVelocity();

        // Edge-triggered queue for the next hit (Rewired button only)
        if (rPlayer != null && ReInput.isReady)
        {
            bool attackPressedThisFrame = rPlayer.GetButtonDown("Attack");
            if (!triggerCalled && attackPressedThisFrame)
                TryQueueNextAttack();
        }

        // When the current attack animation signals exit, decide whether to chain
        if (triggerCalled)
            HandleStateExit();
    }


    public override void Exit()
    {
        base.Exit();

        // Finisher -> reset to 1; otherwise advance
        comboIndex = (comboIndex < comboLimit) ? comboIndex + 1 : FirstComboIndex;

        // Keep facing consistent for idle
        player.lastMoveDirection = lastAttackDir;

        lastTimeAttacked = Time.time;
    }

    // ---------------- Internals ----------------

    private void TryQueueNextAttack()
    {
        if (comboAttackQueued) return;                    // already queued once this swing
        if (comboIndex >= comboLimit) return;             // no next step
        if (Time.time - lastQueueTime < attackQueueCooldown) return;

        comboAttackQueued = true;
        lastQueueTime = Time.time;
    }

    private void HandleStateExit()
    {
        bool canChain = comboIndex < comboLimit;

        if (canChain && comboAttackQueued)
        {
            // Consume and chain
            comboAttackQueued = false;
            anim.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else
        {
            // No chain or end of combo
            stateMachine.ChangeState(player.idleState);
        }
    }

    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;

        // When the burst ends, completely stop movement (x & y)
        if (attackVelocityTimer <= 0f)
            player.SetVelocity(0f, 0f);
    }

    private void ApplyAttackVelocity()
    {
        int idx = Mathf.Clamp(comboIndex - 1, 0, Mathf.Max(0, comboLimit - 1));

        float burst = 0f;
        if (player.attackMovement != null && player.attackMovement.Length > 0)
            burst = player.attackMovement[Mathf.Clamp(idx, 0, player.attackMovement.Length - 1)];

        // Lunge in the locked direction
        player.SetVelocity(burst * lastAttackDir.x, burst * lastAttackDir.y);

        attackVelocityTimer = lungeDuration;
    }

    private void ResetComboIndexIfNeeded()
    {
        // Restart chain if we waited too long
        if (Time.time > lastTimeAttacked + player.comboResetTime)
            comboIndex = FirstComboIndex;

        // Clamp just in case
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
