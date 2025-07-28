using System;
using System.Collections;
using UnityEngine;
using Rewired;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    public UI ui { get; private set; }

    // ---------- Normal EXP exposed (still owned by Player_Stats) ----------
    public int Level => stats.CurrentLevel;
    public float CurrentExp => stats.CurrentEXP;
    public float NextLevelExp => stats.GetNextLevelRequirement();

    // ---------- Sex EXP (NOW OWNED BY PLAYER) ----------
    public int SexLevel { get; private set; } = 1;
    public float CurrentSexExp { get; private set; } = 0f;

    public float NextSexLevelSexExp => GetNextSexLevelRequirementSex();

    [SerializeField] private float BASE_SEX_EXP_REQUIREMENT = 50f;
    [SerializeField] private float SEX_EXP_GROWTH_RATE = 1.35f;

    public Player_SkillManager skillManager { get; private set; }
    public Entity_Mana mana { get; private set; }
    public Entity_Health health { get; private set; }
    public Entity_StatusHandler statusHandler { get; private set; }
    public Player_Combat combat { get; private set; }

    public Vector2 lastMoveDirection = Vector2.down;

    public Inventory_Player inventory { get; private set; }
    public Player_Stats stats { get; private set; }
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

    [SerializeField] private int playerID = 0;
    [SerializeField] private Rewired.Player rPlayer;

    [Header("Attack details")]
    public float[] attackMovement;
    public Vector2 jumpAttackVelocity;
    public float comboResetTime = 1;
    private Coroutine queuedAttackCo;

    [Header("Player Info")]
    public Sprite Portrait;
    [TextArea(3, 10)] public string Bio;

    [Header("Movement details")]
    public float moveSpeed;
    public float jumpForce = 5;
    [Range(0, 1)] public float dashDuration = .25f;
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
        inventory = GetComponent<Inventory_Player>();
        stats = GetComponent<Player_Stats>();

        input = new PlayerInputSet();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        dashState = new Player_DashState(this, stateMachine, "dash");
        thrustState = new Player_ThrustState(this, stateMachine, "thrust");
        basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
        deadState = new Player_DeadState(this, stateMachine, "dead");
        counterAttackState = new Player_CounterAttackState(this, stateMachine, "counterAttack");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        rPlayer = ReInput.players.GetPlayer(playerID);

        health.OnHealthUpdate += UpdateMainUIHealth;
        UpdateMainUIHealth();
        UpdateMainUIMana();
    }

    protected override void Update()
    {
        base.Update();

        if (rPlayer.GetButtonDown("Interact"))
            TryInteract();

        // Debug/testing inputs (optional)
        if (rPlayer.GetButtonDown("TestEXP"))
            GainEXP(50);

        if (rPlayer.GetButtonDown("TestSexEXP"))
            GainSexEXP(25);
    }

    #region UI Updates

    private void UpdateMainUIHealth()
    {
        if (ui != null && ui.playerHealthBar != null)
            ui.playerHealthBar.UpdateHealth(health.GetCurrentHealth(), stats.GetMaxHealth());
    }

    private void UpdateMainUIMana()
    {
        if (ui != null && ui.playerManaBar != null)
            ui.playerManaBar.UpdateMana(mana.GetCurrentMana(), stats.GetMaxMana());
    }

    #endregion

    public void TeleportPlayer(Vector3 position) => transform.position = position;

    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalMoveSpeed = moveSpeed;
        float originalJumpForce = jumpForce;
        float originalAnimSpeed = anim.speed;
        float originalAttackMovement = attackMovement[0];

        float speedMultiplier = 1 - slowMultiplier;

        moveSpeed *= speedMultiplier;
        jumpForce *= speedMultiplier;
        anim.speed *= speedMultiplier;
        dashSpeed *= speedMultiplier;

        for (int i = 0; i < attackMovement.Length; i++)
            attackMovement[i] *= speedMultiplier;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        jumpForce = originalJumpForce;
        anim.speed = originalAnimSpeed;

        for (int i = 0; i < attackMovement.Length; i++)
            attackMovement[i] = originalAttackMovement;
    }

    public override void EntityDeath()
    {
        base.EntityDeath();
        OnPlayerDeath?.Invoke();
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
        closest.GetComponent<IInteractable>()?.Interact();
    }

    #region EXP APIs

    // Normal EXP still lives in Player_Stats
    public void GainEXP(float amount)
    {
        stats.AddEXP(amount);

        if (ui != null)
        {
            ui.StatusPanel?.UpdateStatus(this);
            ui.inGameUI?.UpdateExpBar();
        }
    }

    // Sex EXP is owned by Player
    public void GainSexEXP(float amount)
    {
        CurrentSexExp += amount;
        Debug.Log($"[Player] Gained Sex EXP: {amount} | Total Sex EXP: {CurrentSexExp}");

        while (CurrentSexExp >= GetNextSexLevelRequirementSex())
            LevelUpSex();

        if (ui != null)
        {
            ui.StatusPanel?.UpdateStatus(this);
            ui.inGameUI?.UpdateSexExpBar();
        }
    }

    private void LevelUpSex()
    {
        CurrentSexExp -= GetNextSexLevelRequirementSex();
        SexLevel++;
        Debug.Log($"[Player] Sex Level Up! New Sex Level: {SexLevel}");
        // TODO: grant bonuses, popups, etc.
    }

    public float GetNextSexLevelRequirementSex()
    {
        return BASE_SEX_EXP_REQUIREMENT * Mathf.Pow(SEX_EXP_GROWTH_RATE, SexLevel - 1);
    }

    #endregion

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();
}
