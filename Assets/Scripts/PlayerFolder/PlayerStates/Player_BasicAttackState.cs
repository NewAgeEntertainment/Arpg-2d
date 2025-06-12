using UnityEngine;

public class Player_BasicAttackState : PlayerState
{
    private float attackVelocityTimer;
    private float lastTimeAttacked;

    private bool comboAttackQueued;
    private float xInput;
    private float yInput;
    private float comboInputBufferTime = 0.3f;
    private float comboInputBufferTimer;
    private Vector2 attackDir;
    private Vector2 lastAttackDir;

    private int comboIndex = 1;
    private int comboLimit = 3;
    private const int FirstComboIndex = 1;

    public Player_BasicAttackState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
        if (comboLimit != player.attackMovement.Length)
        {
            Debug.LogWarning("Adjusted combo limit to match attack velocity array!");
            comboLimit = player.attackMovement.Length;
        }
    }

    public override void Enter()
    {
        base.Enter();
        comboAttackQueued = false;
        ResetComboIndexIfNeeded();
        SyncAttackSpeed();

        xInput = player.moveInput.x;
        yInput = player.moveInput.y;

        Vector2 inputDir = new Vector2(xInput, yInput);

        // Use input direction if player is pressing, otherwise use auto-facing
        if (inputDir.sqrMagnitude > 0.01f)
        {
            lastAttackDir = inputDir.normalized;
        }
        else
        {
            lastAttackDir = player.lastMoveDirection;
        }

        SetAttackDirection(lastAttackDir);

        anim.SetInteger("basicAttackIndex", comboIndex);
        ApplyAttackVelocity();
    }

    public override void Update()
    {
        base.Update();
        HandleAttackVelocity();

        // Decrease timer
        if (comboInputBufferTimer > 0)
            comboInputBufferTimer -= Time.deltaTime;


        if (Input.GetKey(KeyCode.E))
            QueueNextAttack();

        if (triggerCalled)
            HandleStateExit();
    }

    public override void Exit()
    {
        base.Exit();
        comboIndex++;
        lastTimeAttacked = Time.time;
    }

    private void HandleStateExit()
    {
        if (comboAttackQueued || comboInputBufferTimer > 0)
        {
            anim.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    private void QueueNextAttack()
    {
        if (comboIndex < comboLimit)
        {
            comboAttackQueued = true;
            comboInputBufferTimer = comboInputBufferTime;
        }
    }

    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;

        if (attackVelocityTimer < 0)
            player.SetVelocity(0, 0);
    }

    private void ApplyAttackVelocity()
    {
        float attackMovement = player.attackMovement[comboIndex - 1];
        player.SetVelocity(attackMovement * lastAttackDir.x, attackMovement * lastAttackDir.y);

        attackVelocityTimer = 0.1f; // Velocity lasts a short time
    }

    private void ResetComboIndexIfNeeded()
    {
        if (Time.time > lastTimeAttacked + player.comboResetTime)
            comboIndex = FirstComboIndex;

        if (comboIndex > comboLimit)
            comboIndex = FirstComboIndex;
    }

    private void SetAttackDirection(Vector2 inputDirection)
    {
        attackDir = inputDirection.normalized;

        float x = Mathf.Round(attackDir.x);
        float y = Mathf.Round(attackDir.y);

        anim.SetFloat("xInput", x);
        anim.SetFloat("yInput", y);
    }
}

