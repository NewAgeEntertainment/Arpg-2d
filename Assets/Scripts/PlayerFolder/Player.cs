using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Rewired;
using Unity.VisualScripting;
using UnityEditor;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    private UI ui;

    private float xInput; // Horizontal input value
    private float yInput; // Vertical input value

    public Player_SkillManager skillManager { get; private set; }
    public Entity_Mana mana { get; private set; } // Reference to the player's mana system
    public Entity_Health health { get; private set; } // Reference to the player's health system
    public Entity_StatusHandler statusHandler { get; private set; } // Reference to the player's status handler for managing buffs and debuffs
    public Player_Combat combat { get; private set; } // Reference to the player's combat system for handling attacks and abilities

    public Vector2 lastMoveDirection = Vector2.down;

    public Player_VFX vfx { get; private set; }

    #region State Variables
    public PlayerInputSet input { get; private set; }
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_ThrustState thrustState { get; private set; }
    public Player_BasicAttackState basicAttackState { get; private set; }
    public Player_DeadState deadState { get; private set; }
    public Player_CounterAttackState counterAttackState { get; private set; }
    #endregion


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
    public Vector2 moveInput { get; set; }

    protected override void Awake()
    {
        base.Awake();

        ui = FindAnyObjectByType<UI>();
        vfx = GetComponent<Player_VFX>();
        health = GetComponent<Entity_Health>();
        mana = GetComponent<Entity_Mana>();
        skillManager = GetComponent<Player_SkillManager>();
        statusHandler = GetComponent<Entity_StatusHandler>();
        combat = GetComponent<Player_Combat>();

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
        rPlayer = Rewired.ReInput.players.GetPlayer(playerID);

    }

    protected override void Update()
    {
        base.Update();
        // ✅ Rewired Interact input check
        if (rPlayer.GetButtonDown("Interact"))
        {

            TryInteract();
        }
    }


    public void TeleportPlayer(Vector3 position) => transform.position = position;

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

    private void TryInteract()
    {
        Transform closest = null;
        float closestDistance = Mathf.Infinity;
        Collider2D[] objectAround = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        foreach (var target in objectAround)
        {
            IInteractable interactable = target.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = target.transform;
            }
        }

        if (closest == null) return;

        closest.GetComponent<IInteractable>().Interact(); // Call the Interact method on the closest interactable object
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;

        input.Player.ToggleSkillTreeUI.performed += ctx => ui.ToggleSkillTreeUI();
        input.Player.Spell.performed += ctx => skillManager.shard.TryUseSkill();
        input.Player.SwordThrow.performed += ctx => skillManager.swordSpin.TryUseSkill();
        //input.Player.ToggleInventoryUI.performed += ctx => ui.ToggleInventoryUI();
        // The most likely reason for a NullReferenceException in the selected code:  



    }

    private void OnDisable()
    {
        input.Disable();
    }



}
