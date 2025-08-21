// ✅ Full Player.cs with EXP, SexEXP, UI hooks, level logic, stat bonuses, and spawn-safe init.

using System;
using System.Collections;
using UnityEngine;
using Rewired;
using UnityEngine.SceneManagement;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    public UI ui { get; private set; }

    public int Level => stats.CurrentLevel;
    public float CurrentExp => stats.CurrentEXP;
    public float NextLevelExp => stats.GetNextLevelRequirement();

    public int SexLevel { get; private set; } = 1;
    public float CurrentSexExp { get; private set; } = 0f;
    public float NextSexLevelSexExp => GetNextSexLevelRequirementSex();

    [Header("Sex Level Stat Bonuses (per level)")]
    [SerializeField] private float sexBonus_MaxArousalPerLevel = 5f;
    [SerializeField] private float sexBonus_SexualDamagePerLevel = 1f;
    [SerializeField] private float sexBonus_StrokePerLevel = 1f;
    [SerializeField] private float sexBonus_ResiliencePerLevel = 0.5f;
    [SerializeField] private float sexBonus_SexualRestraintPerLevel = 0.5f;

    private const string SEX_LEVEL_BONUS_SOURCE = "SexLevelBonus";
    [SerializeField] private float BASE_SEX_EXP_REQUIREMENT = 50f;
    [SerializeField] private float SEX_EXP_GROWTH_RATE = 1.35f;

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
    [SerializeField] private int playerID = 0; // <- This is the Rewired Player ID you’re using
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

    /// <summary>
    /// Public exposure in case other systems need to read which Rewired Player this instance uses.
    /// </summary>
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

        // --- Construct states
        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        dashState = new Player_DashState(this, stateMachine, "dash");
        thrustState = new Player_ThrustState(this, stateMachine, "thrust");
        basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
        deadState = new Player_DeathState(this, stateMachine, "dead");
        counterAttackState = new Player_CounterAttackState(this, stateMachine, "counterAttack");

        // --- Propagate Rewired Player ID to all states (so PlayerState can fetch rPlayer)
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

        // If something already initialized the SM (e.g., via spawner hook), don’t re-init.
        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);

        // Cache Rewired player safely
        TryCacheRewiredPlayer();

        // Hook UI signals
        if (health != null) health.OnHealthUpdate += UpdateMainUIHealth;
        if (mana != null) mana.OnManaUpdate += UpdateMainUIMana;

        UpdateMainUIHealth();
        UpdateMainUIMana();

        ApplySexLevelBonuses();

        // Optional: repaint HUD fully if your UI has that API
        ui?.inGameUI?.ForceRefreshFromCurrentState();
    }

    protected override void Update()
    {
        base.Update();

        // Guard: make sure rPlayer exists (e.g., if Rewired init lagged)
        if (rPlayer == null) TryCacheRewiredPlayer();

        if (rPlayer != null)
        {
            if (rPlayer.GetButtonDown("Interact"))
                TryInteract();

            if (rPlayer.GetButtonDown("TestEXP"))
                GainEXP(50);

            if (rPlayer.GetButtonDown("TestSexEXP"))
                GainSexEXP(25);
        }
    }




    /// <summary>
    /// Called by PlayerSpawner / Player_InitHook immediately after instantiation.
    /// Guarantees the state machine enters a state before Update() so skills can fire.
    /// </summary>
    public void InitializeAfterSpawn()
    {
        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);

        TryCacheRewiredPlayer();
        HardReviveAndSnapToIdle();           // << add this line
        ui?.inGameUI?.ForceRefreshFromCurrentState();
    }


    private void TryCacheRewiredPlayer()
    {
        try
        {
            rPlayer = ReInput.players.GetPlayer(playerID);
            // Debug.Log($"[Player] Cached Rewired Player {playerID}: {(rPlayer != null)}");
        }
        catch
        {
            // ReInput might not be ready yet; will try again next frame
        }
    }

    private void UpdateMainUIHealth()
    {
        ui?.playerHealthBar?.UpdateHealth(health.GetCurrentHealth(), stats.GetMaxHealth());
    }

    private void UpdateMainUIMana()
    {
        // Debug.Log("🔵 Updating Main Mana Bar");
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
        {
            for (int i = 0; i < attackMovement.Length; i++)
                attackMovement[i] *= speedMultiplier;
        }

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        jumpForce = originalJumpForce;
        anim.speed = originalAnimSpeed;

        if (attackMovement != null)
        {
            for (int i = 0; i < attackMovement.Length; i++)
                attackMovement[i] = originalAttackMovement;
        }
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

    public void GainEXP(float amount)
    {
        stats.AddEXP(amount);
        ui?.StatusPanel?.UpdateStatus(this);
        ui?.inGameUI?.UpdateExpBar();
    }

    public void GainSexEXP(float amount)
    {
        CurrentSexExp += amount;
        Debug.Log($"[Player] Gained Sex EXP: {amount} | Total Sex EXP: {CurrentSexExp}");

        while (CurrentSexExp >= GetNextSexLevelRequirementSex())
            LevelUpSex();

        ui?.StatusPanel?.UpdateStatus(this);
        ui?.inGameUI?.UpdateSexExpBar();
    }

    public void SetSexLevel(int level)
    {
        SexLevel = level;
        ApplySexLevelBonuses();
    }

    public void SetCurrentSexEXP(float exp)
    {
        CurrentSexExp = exp;
    }

    private void LevelUpSex()
    {
        CurrentSexExp -= GetNextSexLevelRequirementSex();
        SexLevel++;
        Debug.Log($"[Player] Sex Level Up! New Sex Level: {SexLevel}");

        ApplySexLevelBonuses();
    }

    public float GetNextSexLevelRequirementSex()
    {
        return BASE_SEX_EXP_REQUIREMENT * Mathf.Pow(SEX_EXP_GROWTH_RATE, SexLevel - 1);
    }

    #endregion

    private void ApplySexLevelBonuses()
    {
        RemoveSexLevelBonuses();

        if (stats == null)
        {
            Debug.LogWarning("[Player] Missing stats, cannot apply Sex Level bonuses.");
            return;
        }

        stats.sex.maxArousal.AddModifier(sexBonus_MaxArousalPerLevel * SexLevel, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualDamage.AddModifier(sexBonus_SexualDamagePerLevel * SexLevel, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.stroke.AddModifier(sexBonus_StrokePerLevel * SexLevel, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.resilience.AddModifier(sexBonus_ResiliencePerLevel * SexLevel, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualRestraint.AddModifier(sexBonus_SexualRestraintPerLevel * SexLevel, StatModType.Flat, SEX_LEVEL_BONUS_SOURCE);
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

    // Runs after any scene load: ensure we aren't stuck in dead pose/state after loading a save/new game
    private void OnSceneLoaded_ReviveGuard(Scene scene, LoadSceneMode mode)
    {
        HardReviveAndSnapToIdle();

        // Make sure refs are valid
        if (health == null) health = GetComponent<Entity_Health>();
        if (stats == null) stats = GetComponent<Player_Stats>();

        // If we were dead or have zero health, hard revive to a clean state.
        if (health != null && (health.IsDead || health.GetCurrentHealth() <= 0.001f))
        {
            health.ForceReviveToFull();
        }

        // Clear animator's "dead" latch and jump to Idle at time 0.
        if (anim != null)
        {
            anim.ResetTrigger("die");        // if you used a trigger
            anim.SetBool("dead", false);     // matches your Death state's animBoolName
            anim.Play("idle", 0, 0f);        // <- use your actual Idle state name if different
        }

        // Reset SM + movement
        stateMachine.ChangeState(idleState);
        SetVelocity(0f, 0f);

        // Hide any lingering GameOver screen (in case we came from that flow)
        UI_GameOver.HideStatic(0f);
    }


    public void ReviveIfStuckDead(bool fullHeal)
    {
        if (health == null) return;

        bool animDead = (anim != null) && anim.GetBool("dead");
        if (stateMachine.currentState == deadState || animDead || health.IsDead || health.GetCurrentHealth() <= 0f)
        {
            // clear death
            health.ForceRevive(fullHeal ? (float?)stats.GetMaxHealth() : null);

            if (anim != null) anim.SetBool("dead", false);
            SetVelocity(0f, 0f);

            if (stateMachine.currentState == deadState)
                stateMachine.ChangeState(idleState);
            else if (stateMachine.currentState == null)
                stateMachine.Initialize(idleState);
            else
                stateMachine.ChangeState(idleState);

            UI_GameOver.HideStatic(0f);
        }
    }



    private void OnEnable()
    {
        input.Enable();

        // Guard against double-subscribe by removing first.
        SceneManager.sceneLoaded -= OnSceneLoaded_ReviveGuard;
        SceneManager.sceneLoaded += OnSceneLoaded_ReviveGuard;
    }

    private void OnDisable()
    {
        // Always unsubscribe when this component is disabled/destroyed.
        SceneManager.sceneLoaded -= OnSceneLoaded_ReviveGuard;

        input.Disable();
    }



    // Call this after starting a new game OR after loading a save.
    public void HardReviveAndSnapToIdle()
    {
        // 1) Health logic
        if (health == null) health = GetComponent<Entity_Health>();
        if (stats == null) stats = GetComponent<Player_Stats>();
        if (health != null && (health.IsDead || health.GetCurrentHealth() <= 0f))
            health.ForceRevive(); // or ForceReviveToFull();

        // 2) Animator reset + jump to Idle
        if (anim != null)
        {
            anim.ResetTrigger("die");
            anim.ResetTrigger("Death");
            anim.SetBool("dead", false);
            anim.SetBool("isDead", false);

            anim.Rebind();
            anim.Update(0f);

            // Try common Idle names; if not found, default layer reset is enough.
            TryPlayIdle(anim);
        }

        // 3) State machine & motion
        stateMachine.ChangeState(idleState);
        SetVelocity(0f, 0f);
    }

    private static void TryPlayIdle(Animator a)
    {
        // Try common state names (layer 0)
        string[] names = { "Base Layer.Idle", "Idle", "idle", "Locomotion", "Movement" };
        for (int i = 0; i < names.Length; i++)
        {
            int h = Animator.StringToHash(names[i]);
            if (a.HasState(0, h)) { a.Play(h, 0, 0f); return; }
        }
    }

}
