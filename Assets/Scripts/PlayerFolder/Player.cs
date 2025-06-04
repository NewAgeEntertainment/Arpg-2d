using System;
using System.Collections;
using UnityEngine;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    private UI ui;
    public PlayerInputSet input { get; private set; }
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_ThrustState thrustState { get; private set; }
    public Player_BasicAttackState basicAttackState { get; private set; }
    public Player_DeadState deadState { get; private set; }
    public Player_CounterAttackState counterAttackState { get; private set; }

    [SerializeField] private int playerID = 0; // Player ID for multiplayer support
    [SerializeField] private Rewired.Player rPlayer; // Rewired player instance for input handling

    [Header("Attack details")]
    public float[] attackMovement;
    public Vector2 jumpAttackVelocity;
    public float comboResetTime = 1;
    private Coroutine queuedAttackCo;

    
    [Header("Movement details")]
    public float moveSpeed;
    public float jumpForce = 5;
    [Range(0, 1)]
    [Space]
    public float dashDuration = .25f;
    public float dashSpeed = 20;
    public float ThrustDuration;
    public float ThrustSpeed;
    public Vector2 moveInput { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        ui = FindAnyObjectByType<UI>();
        input = new PlayerInputSet();


        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        dashState = new Player_DashState(this, stateMachine, "dash");
        thrustState = new Player_ThrustState(this, stateMachine,"thrust");
        basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
        deadState = new Player_DeadState(this, stateMachine, "dead");
        counterAttackState = new Player_CounterAttackState(this, stateMachine, "counterAttack");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        
    }

    

    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalMoveSpeed = moveSpeed; // Store the original move speed
        float originalJumpForce = jumpForce; // Store the original jump force
        float originalAnimSpeed = anim.speed; // Store the original animation speed
        float originalAttackMovenment = attackMovement[0]; // Store the original attack movement speed  

        float speedMultiplier = 1 - slowMultiplier; // Calculate the speed multiplier

        moveSpeed = moveSpeed * speedMultiplier; // Apply the slowdown to move speed
        jumpForce = jumpForce * speedMultiplier; // Apply the slowdown to jump force
        anim.speed = anim.speed * speedMultiplier; // Apply the slowdown to animation speed
        dashSpeed = dashSpeed * speedMultiplier; // Apply the slowdown to dash speed

        for (int  i = 0;  i < attackMovement.Length;  i++)
        {
            attackMovement[i] = attackMovement[i] * speedMultiplier;
        }

        yield return new WaitForSeconds(duration); // Wait for the slowdown duration

        moveSpeed = originalMoveSpeed; // Restore the original move speed
        jumpForce = originalJumpForce; // Restore the original jump force
        anim.speed = originalAnimSpeed; // Restore the original animation speed
       
        for (int i = 0; i < attackMovement.Length; i++)
        {
            attackMovement[i] = originalAttackMovenment; // Restore the original attack movement speed
        }
    }

    public override void EntityDeath()
    {
        base.EntityDeath();
        OnPlayerDeath?.Invoke(); // Notify subscribers about player death
        stateMachine.ChangeState(deadState);
    }


    public void EnterAttackStateWithDelay()
    {
        if (queuedAttackCo != null)
            StopCoroutine(queuedAttackCo);

        queuedAttackCo = StartCoroutine(EnterAttackStateWithDelayCo());
    }

    private IEnumerator EnterAttackStateWithDelayCo()
    {
        yield return new WaitForEndOfFrame();
        stateMachine.ChangeState(basicAttackState);
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;

        input.Player.ToggleSkillTreeUI.performed += ctx => ui.ToggleSkillTreeUI();

        
    }

    private void OnDisable()
    {
        input.Disable();
    }
    

    
}
