using System;
using System.Collections;
using UnityEngine;
using Rewired;
using UnityEngine.SceneManagement;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    public UI ui { get; private set; }

    public int Level => stats != null ? stats.CurrentLevel : 1;
    public float CurrentExp => stats != null ? stats.CurrentEXP : 0f;
    public float NextLevelExp => stats != null ? stats.GetNextLevelRequirement() : 0f;

    // <-- Forwarded to Player_Stats so there’s only one source of truth
    public int SexLevel => stats != null ? stats.CurrentSexLevel : 1;
    public float CurrentSexExp => stats != null ? stats.CurrentSexEXP : 0f;
    public float NextSexLevelSexExp => stats != null ? stats.GetNextSexLevelRequirement() : 0f;

    [Header("Sex Level Stat Bonuses (per level)")]
    [SerializeField] private float sexBonus_MaxArousalPerLevel = 5f;
    [SerializeField] private float sexBonus_SexualDamagePerLevel = 1f;
    [SerializeField] private float sexBonus_StrokePerLevel = 1f;
    [SerializeField] private float sexBonus_ResiliencePerLevel = 0.5f;
    [SerializeField] private float sexBonus_SexualRestraintPerLevel = 0.5f;

    private const string SEX_LEVEL_BONUS_SOURCE = "SexLevelBonus";

    public Player_SkillManager skillManager { get; private set; }
    public Entity_Mana mana { get; private set; }
    public Entity_Health health { get; private set; }
    public Entity_StatusHandler statusHandler { get; private set; }
    public Player_Combat combat { get; private set; }
    public Inventory_Player inventory { get; private set; }
    public Player_Stats stats { get; private set; }
    public Player_VFX vfx { get; private set; }

    public Vector2 lastMoveDirection = Vector2.down;

    public PlayerInputSet input { get; private set; }

    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_ThrustState thrustState { get; private set; }
    public Player_BasicAttackState basicAttackState { get; private set; }
    public Player_DeathState deadState { get; private set; }
    public Player_CounterAttackState counterAttackState { get; private set; }

    [Header("Rewired")]
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

    public int rewiredPlayerId => playerID;

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
        deadState = new Player_DeathState(this, stateMachine, "dead");
        counterAttackState = new Player_CounterAttackState(this, stateMachine, "counterAttack");

        idleState.SetRewiredPlayerId(playerID);
        moveState.SetRewiredPlayerId(playerID);
        dashState.SetRewiredPlayerId(playerID);
        thrustState.SetRewiredPlayerId(playerID);
        basicAttackState.SetRewiredPlayerId(playerID);
        deadState.SetRewiredPlayerId(playerID);
        counterAttackState.SetRewiredPlayerId(playerID);

        DontDestroyOnLoad(gameObject);
    }

    protected override void Start()
    {
        base.Start();

        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);

        TryCacheRewiredPlayer();

        if (health != null) health.OnHealthUpdate += UpdateMainUIHealth;
        if (mana != null) mana.OnManaUpdate += UpdateMainUIMana;

        UpdateMainUIHealth();
        UpdateMainUIMana();

        ApplySexLevelBonuses();
        ui?.inGameUI?.ForceRefreshFromCurrentState();
    }

    protected override void Update()
    {
        base.Update();

        if (rPlayer == null) TryCacheRewiredPlayer();

        if (rPlayer != null)
        {
            if (rPlayer.GetButtonDown("Interact")) TryInteract();
            if (rPlayer.GetButtonDown("TestEXP")) GainEXP(50);
            if (rPlayer.GetButtonDown("TestSexEXP")) GainSexEXP(25);
        }
    }

    public void InitializeAfterSpawn()
    {
        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);

        TryCacheRewiredPlayer();
        ui?.inGameUI?.ForceRefreshFromCurrentState();
    }

    private void TryCacheRewiredPlayer()
    {
        try { rPlayer = ReInput.players.GetPlayer(playerID); }
        catch { /* try again next frame if ReInput not ready */ }
    }

    private void UpdateMainUIHealth()
    {
        ui?.playerHealthBar?.UpdateHealth(health.GetCurrentHealth(), stats.GetMaxHealth());
    }

    private void UpdateMainUIMana()
    {
        ui?.playerManaBar?.UpdateMana(mana.GetCurrentMana(), stats.GetMaxMana());
    }

    public void TeleportPlayer(Vector3 position) => transform.position = position;

    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalMoveSpeed = moveSpeed;
        float originalJumpForce = jumpForce;
        float originalAnimSpeed = anim.speed;
        float originalAttackMovement = attackMovement != null && attackMovement.Length > 0 ? attackMovement[0] : 0f;
        float speedMultiplier = 1 - slowMultiplier;

        moveSpeed *= speedMultiplier;
        jumpForce *= speedMultiplier;
        anim.speed *= speedMultiplier;
        dashSpeed *= speedMultiplier;

        if (attackMovement != null)
            for (int i = 0; i < attackMovement.Length; i++) attackMovement[i] *= speedMultiplier;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        jumpForce = originalJumpForce;
        anim.speed = originalAnimSpeed;

        if (attackMovement != null)
            for (int i = 0; i < attackMovement.Length; i++) attackMovement[i] = originalAttackMovement;
    }

    public override void EntityDeath()
    {
        base.EntityDeath();
        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
    }

    public void EnterAttackStateWithDelay()
    {
        if (queuedAttackCo != null) StopCoroutine(queuedAttackCo);
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
        var objectAround = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        foreach (var target in objectAround)
        {
            var interactable = target.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance < closestDistance) { closestDistance = distance; closest = target.transform; }
        }

        if (closest == null) return;
        closest.GetComponent<IInteractable>()?.Interact();
    }

    // --- Revive/anim safety after scene loads or new game ---
    private bool _reviveHooked;

    private void OnEnable()
    {
        // keep your existing OnEnable
        input.Enable();

        if (!_reviveHooked)
        {
            SceneManager.sceneLoaded += OnSceneLoaded_EnsureAlive;
            _reviveHooked = true;
        }
    }

    private void OnDisable()
    {
        input.Disable();

        if (_reviveHooked)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded_EnsureAlive;
            _reviveHooked = false;
        }
    }

    // Runs after every scene load (including SaveSystem loads)
    private void OnSceneLoaded_EnsureAlive(Scene s, LoadSceneMode m)
    {
        StartCoroutine(EnsureAliveNextFrame());
    }

    private IEnumerator EnsureAliveNextFrame()
    {
        // let spawners finish
        yield return null;

        ResetToAliveAfterLoad(fullHealIfZero: true);

        // repaint HUD
        ui?.inGameUI?.ForceRefreshFromCurrentState();
    }

    // Call this from anywhere if needed
    public void ResetToAliveAfterLoad(bool fullHealIfZero)
    {
        // 1) clear dead flag + health
        if (health != null)
        {
            if (fullHealIfZero && health.GetCurrentHealth() <= 0f)
                health.ForceReviveToFull();      // your helper on Entity_Health
            else
                health.ForceRevive();             // leaves current HP if > 0, sets at least 1 HP otherwise
        }

        // 2) animator / state machine
        if (anim != null)
        {
            anim.ResetTrigger("dead");
            anim.SetBool("dead", false);         // name must match your death anim bool
        }

        // If the SM was never initialized (fresh spawn), initialize. If it was dead, go idle.
        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);
        else if (stateMachine.currentState == deadState)
            stateMachine.ChangeState(idleState);

        // 3) stop residual motion
        SetVelocity(0f, 0f);

        // 4) re-enable default input maps (in case a UI or state disabled them)
        try
        {
            var rp = ReInput.players.GetPlayer(playerID);
            rp.controllers.maps.SetMapsEnabled(true, "Default");
        }
        catch { /* Rewired not ready yet */ }
    }


    // --- Compatibility shim for old UI code ---
    // Old UI calls Player.GetNextSexLevelRequirementSex(); forward to Player_Stats now.
    public float GetNextSexLevelRequirementSex()
    {
        return stats != null ? stats.GetNextSexLevelRequirement() : 0f;
    }


    // ===== EXP APIs =====

    public void GainEXP(float amount)
    {
        if (stats == null) return;
        stats.AddEXP(amount);
        ui?.StatusPanel?.UpdateStatus(this);
        ui?.inGameUI?.UpdateExpBar(); // harmless (events also repaint)
    }

    public void GainSexEXP(float amount)
    {
        if (stats == null) return;
        stats.AddSexEXP(amount); // stats fires event -> UI repaints
        ui?.StatusPanel?.UpdateStatus(this);
        ui?.inGameUI?.UpdateSexExpBar(); // harmless (events also repaint)
    }

    // Optional forwards if something else sets values:
    public void SetSexLevel(int level) => stats?.SetSexLevelAndExp(level, stats.CurrentSexEXP);
    public void SetCurrentSexEXP(float exp) => stats?.SetSexLevelAndExp(stats.CurrentSexLevel, exp);

    private void ApplySexLevelBonuses()
    {
        RemoveSexLevelBonuses();
        if (stats == null) return;

        int lvl = stats.CurrentSexLevel;
        stats.sex.maxArousal.AddModifier(sexBonus_MaxArousalPerLevel * lvl, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualDamage.AddModifier(sexBonus_SexualDamagePerLevel * lvl, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.stroke.AddModifier(sexBonus_StrokePerLevel * lvl, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.resilience.AddModifier(sexBonus_ResiliencePerLevel * lvl, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualRestraint.AddModifier(sexBonus_SexualRestraintPerLevel * lvl, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        Debug.Log($"Trigger: {c.name} (isTrigger={c.isTrigger}, layer={LayerMask.LayerToName(c.gameObject.layer)})");
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        Debug.Log($"Collision: {c.collider.name} (isTrigger={c.collider.isTrigger})");
    }



    private void RemoveSexLevelBonuses()
    {
        if (stats == null) return;
        stats.sex.maxArousal.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualDamage.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.stroke.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.resilience.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualRestraint.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
    }

    
}
