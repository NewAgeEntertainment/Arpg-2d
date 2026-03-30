using System;
using System.Collections;
using UnityEngine;
using Rewired;
using UnityEngine.SceneManagement;

public class Player : Entity
{
    public static event Action OnPlayerDeath;
    public static Player instance;
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

    [Header("Thrust Target Check")]
    [SerializeField] private Transform thrustTargetCheck;
    [SerializeField] private float thrustTargetRadius = 3f;
    [SerializeField] private LayerMask thrustTargetMask = ~0; // default: everything

    /// <summary>How far in front of the player to place the thrust target point.</summary>
    [SerializeField] private float thrustTargetOffset = 1.5f;

    // Public accessors
    public Transform ThrustTargetCheck => thrustTargetCheck;
    public float ThrustTargetRadius => thrustTargetRadius;
    public LayerMask ThrustTargetMask => thrustTargetMask;
    public float ThrustTargetOffset => thrustTargetOffset;


    [Header("Thrust Aim Assist")]
    [Tooltip("Max angle from stick direction that we will 'snap' toward an enemy.")]
    [SerializeField, Range(0f, 90f)] private float thrustSoftAimAngle = 45f;

    [Tooltip("Max distance to look for soft-aim targets when thrusting.")]
    [SerializeField] private float thrustSoftAimRange = 6f;

    // Public accessors used by Player_ThrustState
    public float ThrustSoftAimAngle => thrustSoftAimAngle;
    public float ThrustSoftAimRange => thrustSoftAimRange;


    [Header("Interact scan")]
    [SerializeField] private float interactRadius = 1.8f;
    [SerializeField] private LayerMask interactMask = ~0; // set in Inspector to your “Interactable” layers


    public int rewiredPlayerId => playerID;

    // ===================== Tall Grass (Volume + Overlay) =====================
    [Header("Tall Grass (volume+overlay)")]
    [Tooltip("Tag used by your GrassVolume trigger object(s).")]
    [SerializeField] private string grassTag = "GrassVolume";
    [Tooltip("Optional layer name for grass volumes; left blank to ignore.")]
    [SerializeField] private string grassLayerName = "TallGrass";
    [Tooltip("Optional: particle played at feet while walking in grass if Player_VFX has no method.")]
    [SerializeField] private ParticleSystem grassRustlePrefab;
    [Tooltip("Where to spawn the rustle VFX. Defaults to this transform if null.")]
    [SerializeField] private Transform feetPivot;
    [SerializeField, Range(0.05f, 0.5f)] private float grassRustleInterval = 0.18f;

    private float _lastGrassRustleTime;
    public bool InGrass { get; private set; }
    public event Action<bool> OnGrassStateChanged;
    // ========================================================================

    // ===================== Input/Animation gating for menus =================
    private bool _inputEnabled = true;
    private float _animSpeedBeforePause = 1f;
    public bool InputEnabled => _inputEnabled;

    /// <summary>
    /// Called by UI when opening/closing menus. Disables gameplay input,
    /// zeroes velocity, and freezes animator while false.
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        // Stop motion immediately when disabled
        if (!enabled)
        {
            moveInput = Vector2.zero;

            if (rb != null) rb.velocity = Vector2.zero;

            if (anim != null)
            {
                _animSpeedBeforePause = Mathf.Approximately(anim.speed, 0f) ? 1f : anim.speed;
                anim.updateMode = AnimatorUpdateMode.Normal; // ensure scaled time
                anim.speed = 0f;

                // Reset common movement params to avoid "sliding" anim while paused
                SafeSetAnimFloat("X", 0f);
                SafeSetAnimFloat("Y", 0f);
                SafeSetAnimFloat("Speed", 0f);
            }
        }
        else
        {
            if (anim != null)
            {
                anim.updateMode = AnimatorUpdateMode.Normal;
                anim.speed = (_animSpeedBeforePause <= 0f) ? 1f : _animSpeedBeforePause;
            }
        }
    }

    private void SafeSetAnimFloat(string param, float value)
    {
        if (anim == null) return;
        try
        {
            // Avoid errors if controller doesn't have this param
            if (HasAnimatorParameter(param, AnimatorControllerParameterType.Float))
                anim.SetFloat(param, value);
        }
        catch { /* ignore */ }
    }

    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
    // ========================================================================

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
        // When input is disabled by UI, hard-freeze motion/anim and skip gameplay Update.
        if (!_inputEnabled)
        {
            if (rb != null) rb.velocity = Vector2.zero;

            if (anim != null)
            {
                anim.speed = 0f;
                SafeSetAnimFloat("X", 0f);
                SafeSetAnimFloat("Y", 0f);
                SafeSetAnimFloat("Speed", 0f);
            }
            return;
        }

        base.Update();

        if (rPlayer == null) TryCacheRewiredPlayer();

        if (rPlayer != null)
        {
            if (rPlayer.GetButtonDown("Interact")) TryInteract();
            if (rPlayer.GetButtonDown("TestEXP")) GainEXP(50);
            if (rPlayer.GetButtonDown("TestSexEXP")) GainSexEXP(25);
        }

        // --- Tall grass ambient VFX while moving ---
        HandleGrassFootstepsVFX();
    }

    private void LateUpdate()
    {
        // Safety net: ensure velocity stays zero while paused
        if (!_inputEnabled && rb != null)
            rb.velocity = Vector2.zero;
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
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactMask);
        if (hits == null || hits.Length == 0) return;

        Transform bestT = null;
        float bestSqr = float.PositiveInfinity;
        InteractionTooltipTrigger2D bestTrigger = null;
        IInteractable bestInteractable = null;
        PixelCrushers.DialogueSystem.Usable bestUsable = null;

        foreach (var h in hits)
        {
            if (!h) continue;

            // Prefer components on parent too
            var trig = h.GetComponentInParent<InteractionTooltipTrigger2D>();
            var inter = h.GetComponentInParent<IInteractable>();
            var usab = h.GetComponentInParent<PixelCrushers.DialogueSystem.Usable>();

            if (trig == null && inter == null && usab == null) continue;

            Transform t =
                (trig != null) ? trig.transform :
                (inter != null) ? (inter as Component).transform :
                (usab != null) ? usab.transform : null;

            if (t == null) continue;

            float d = (t.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                bestT = t;
                bestTrigger = trig;
                bestInteractable = inter;
                bestUsable = usab;
            }
        }

        if (bestT == null) return;

        if (bestTrigger != null)
        {
            bestTrigger.Interact(transform);
            return;
        }

        if (bestInteractable != null)
        {
            bestInteractable.Interact();
            return;
        }

        if (bestUsable != null)
        {
            if (!TryInvokeOnUse(bestUsable, transform))
            {
                // Last resort so any matching handler still runs
                bestUsable.gameObject.SendMessage("OnUse", transform, SendMessageOptions.DontRequireReceiver);
            }
        }
    }


    private static bool TryInvokeOnUse(object usable, Transform actor)
    {
        if (usable == null) return false;
        var t = usable.GetType();

        // Dialogue System commonly uses OnUse(Transform actor)
        var m = t.GetMethod("OnUse",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(Transform) }, null);
        if (m != null) { m.Invoke(usable, new object[] { actor }); return true; }

        // Some variants expose Use(Transform)
        m = t.GetMethod("Use",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(Transform) }, null);
        if (m != null) { m.Invoke(usable, new object[] { actor }); return true; }

        // Or parameterless Use()
        m = t.GetMethod("Use",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, System.Type.EmptyTypes, null);
        if (m != null) { m.Invoke(usable, null); return true; }

        return false;
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
            rp.controllers.maps.SetMapsEnabled(true, "Gameplay");
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
        if (ui?.StatusPanel != null && ui.StatusPanel.IsOpen)
            ui.StatusPanel.RefreshCurrentCharacter();
        ui?.inGameUI?.UpdateExpBar(); // harmless (events also repaint)
    }

    public void GainSexEXP(float amount)
    {
        if (stats == null) return;
        stats.AddSexEXP(amount); // stats fires event -> UI repaints
        if (ui?.StatusPanel != null && ui.StatusPanel.IsOpen)
            ui.StatusPanel.RefreshCurrentCharacter();
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

        // >>> Tall Grass enter detection (added) <<<
        if (IsGrassCollider(c))
            EnterGrass();
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        Debug.Log($"Collision: {c.collider.name} (isTrigger={c.collider.isTrigger})");
    }

    // >>> Added: keep/exit logic for grass volumes <<<
    void OnTriggerStay2D(Collider2D c)
    {
        if (IsGrassCollider(c))
            InGrass = true;
    }

    void OnTriggerExit2D(Collider2D c)
    {
        if (IsGrassCollider(c))
            ExitGrass();
    }
    // <<< end grass triggers >>>

    private void RemoveSexLevelBonuses()
    {
        if (stats == null) return;
        stats.sex.maxArousal.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualDamage.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.stroke.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.resilience.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
        stats.sex.sexualRestraint.RemoveModifier(SEX_LEVEL_BONUS_SOURCE);
    }

    // ---------------- Tall Grass helpers (added) ----------------

    private bool IsGrassCollider(Collider2D c)
    {
        if (!c.isTrigger) return false;

        // Tag check
        if (!string.IsNullOrEmpty(grassTag) && c.CompareTag(grassTag))
            return true;

        // Layer check
        if (!string.IsNullOrEmpty(grassLayerName))
        {
            int layer = LayerMask.NameToLayer(grassLayerName);
            if (layer >= 0 && c.gameObject.layer == layer) return true;
        }

        // Component check (in case you added a GrassVolume script)
        return c.GetComponent<GrassVolume>() != null;
    }

    private void EnterGrass()
    {
        if (InGrass) return;
        InGrass = true;
        OnGrassStateChanged?.Invoke(true);

        // Optional: immediate rustle on first step-in
        SpawnGrassRustleVFX();
        // Example: if you have footstep audio routing
        // Audio?.SetFootstepProfile("Grass");
    }

    private void ExitGrass()
    {
        if (!InGrass) return;
        InGrass = false;
        OnGrassStateChanged?.Invoke(false);

        // Audio?.SetFootstepProfile("Default");
    }

    private void HandleGrassFootstepsVFX()
    {
        if (!InGrass) return;

        // Consider either input or rigidbody speed
        bool moving = (rb != null && rb.velocity.sqrMagnitude > 0.01f) ||
                      (moveInput.sqrMagnitude > 0.01f);

        if (!moving) return;

        if (Time.time - _lastGrassRustleTime >= grassRustleInterval)
        {
            _lastGrassRustleTime = Time.time;
            SpawnGrassRustleVFX();
        }
    }

    // Let external triggers toggle grass state safely.
    public void SetInGrass(bool value)
    {
        if (value) EnterGrass();
        else ExitGrass();
    }

    private void SpawnGrassRustleVFX()
    {
        // Prefer your Player_VFX component if it exposes something like this:
        if (vfx != null && vfx.TryPlay("GrassRustle")) return;

        // Fallback: instantiate the optional ParticleSystem prefab at the feet
        if (grassRustlePrefab != null)
        {
            Transform pivot = feetPivot != null ? feetPivot : transform;
            var ps = Instantiate(grassRustlePrefab, pivot.position, Quaternion.identity);
            // Auto-destroy after it finishes
            var main = ps.main;
            Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.25f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Interact radius
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);

        // Thrust target radius (if assigned)
        if (thrustTargetCheck != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(thrustTargetCheck.position, thrustTargetRadius);
        }
    }
#endif



#if UNITY_EDITOR
    private void OnValidate()
    {
        if (feetPivot == null) feetPivot = transform;
    }
#endif




}
