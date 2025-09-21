using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Companion : Entity
{
    [HideInInspector] public CompanionCombat combat;

    // ── Follow Settings ──────────────────────────────────────────────────
    [Header("Follow Settings")]
    public Transform playerTarget;
    public float followStartDistance = 3f;
    public float followStopDistance = 1.2f;
    public float maxFollowDistance = 10f;
    public float moveSpeed = 3.5f;

    // ── Battle Settings ──────────────────────────────────────────────────
    [Header("Battle Settings")]
    public float chaseRadius = 6f;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    // ── Animation Settings ───────────────────────────────────────────────
    [Header("Animation Settings")]
    public float battleMoveSpeed = 4.5f;
    public float moveAnimSpeedMultiplier = 1f;

    // ── Party ────────────────────────────────────────────────────────────
    [Header("Party")]
    [SerializeField] private bool startInParty = false; // Inspector toggle
    public bool StartInParty => startInParty;
    public bool InParty { get; private set; }
    public Action<bool> OnPartyFlagChanged;
    private bool _queueFollow; // if SetInParty happens before Initialize(...)

    // ── Auto-Resolve Player (GLUE you asked for) ─────────────────────────
    [Header("Auto-Resolve Player")]
    [Tooltip("Keep trying to bind to the current Player if the ref is missing.")]
    [SerializeField] private bool alwaysAutoResolvePlayer = true;
    [SerializeField] private float recheckEvery = 0.5f; // seconds
    private float _nextResolveAt;
    private Coroutine bindCo;

    // ── States (created here unless subclass overrides in Awake) ─────────
    [HideInInspector] public Companion_FollowState followState;
    [HideInInspector] public Companion_ChaseState chaseState;
    [HideInInspector] public Companion_AttackState attackState;
    [HideInInspector] public Companion_ReturnState returnState;
    [HideInInspector] public Companion_IdleState idleState;

    // ====================================================================
    // Unity lifecycle
    // ====================================================================

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();
        combat = GetComponent<CompanionCombat>();
    }

    private void OnEnable()
    {
        // Subscribe to the single source of truth for Player (PlayerLocator)
        PlayerLocator.OnChanged += HandleLocatorChanged;

        // Optional: listen to PartyManager's player-resolved event if present
        CompanionPartyManager.OnPlayerResolved += HandleManagerResolved;

        TryBindFromLocatorOrWorld(); // first attempt
        _nextResolveAt = 0f;

        // If still null (e.g., player spawns late), keep trying briefly
        if (playerTarget == null)
        {
            if (bindCo != null) StopCoroutine(bindCo);
            bindCo = StartCoroutine(BindPlayerWhenAvailable());
        }
    }

    private void OnDisable()
    {
        PlayerLocator.OnChanged -= HandleLocatorChanged;
        CompanionPartyManager.OnPlayerResolved -= HandleManagerResolved;

        if (bindCo != null) StopCoroutine(bindCo);
        bindCo = null;
    }

    protected override void Start()
    {
        // Create states if not provided by subclass (e.g., Companion_Rabbie)
        if (followState == null) followState = new Companion_FollowState(this, stateMachine);
        if (chaseState == null) chaseState = new Companion_ChaseState(this, stateMachine);
        if (attackState == null) attackState = new Companion_AttackState(this, stateMachine);
        if (returnState == null) returnState = new Companion_ReturnState(this, stateMachine);
        if (idleState == null) idleState = new Companion_IdleState(this, stateMachine);

        if (stateMachine.currentState == null)
            stateMachine.Initialize(idleState);

        AfterStateMachineInitialized();
    }

    protected override void Update()
    {
        // Keep trying to bind if we ever lose the player reference
        if (alwaysAutoResolvePlayer && playerTarget == null && Time.unscaledTime >= _nextResolveAt)
        {
            _nextResolveAt = Time.unscaledTime + recheckEvery;
            TryBindFromLocatorOrWorld();
        }

        stateMachine.UpdateActiveState();
    }

    // ====================================================================
    // Player binding (GLUE)
    // ====================================================================

    private void HandleLocatorChanged(Transform t)
    {
        if (t == null) return;
        playerTarget = t;

        // If we're actively in party, ensure we resume following on rebind.
        if (InParty && followState != null && stateMachine?.currentState != null)
            stateMachine.ChangeState(followState);
    }

    private void HandleManagerResolved(Transform t)
    {
        if (t == null) return;
        playerTarget = t;

        if (InParty && followState != null && stateMachine?.currentState != null)
            stateMachine.ChangeState(followState);
    }

    /// <summary>Try several sources to bind the Player reference.</summary>
    private bool TryBindFromLocatorOrWorld()
    {
        // 1) PlayerLocator (single source of truth)
        if (PlayerLocator.Current != null) { HandleLocatorChanged(PlayerLocator.Current); return true; }

        // 2) PartyManager (if available)
        if (CompanionPartyManager.Instance && CompanionPartyManager.Instance.player)
        { HandleManagerResolved(CompanionPartyManager.Instance.player); return true; }

        // 3) GameManager singleton (if you use one)
        var gmPlayer = GameManager.Instance ? GameManager.Instance.Player : null;
        if (gmPlayer) { HandleManagerResolved(gmPlayer.transform); return true; }

        // 4) Tag lookup
        var byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag) { HandleManagerResolved(byTag.transform); return true; }

        // 5) Component search across ALL loaded scenes (incl. DDOL)
        var comp = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (comp) { HandleManagerResolved(comp.transform); return true; }

        return false;
    }

    /// <summary>Coroutine to keep trying for a short time when the player spawns late.</summary>
    private IEnumerator BindPlayerWhenAvailable(float timeout = 8f)
    {
        float end = Time.unscaledTime + timeout;
        while (playerTarget == null && Time.unscaledTime < end)
        {
            if (TryBindFromLocatorOrWorld()) break;
            yield return null;
        }
        bindCo = null;

        if (playerTarget != null && InParty && followState != null && stateMachine?.currentState != null)
            stateMachine.ChangeState(followState);
    }

    // ====================================================================
    // Movement & distances
    // ====================================================================

    /// <summary>Moves toward a world position, updating animator x/y.</summary>
    public void MoveTo(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f)
        {
            SetZeroVelocity(); // <- Entity API
            return;
        }

        dir.Normalize();
        SetVelocity(dir.x * moveSpeed, dir.y * moveSpeed); // <- Entity API

        anim.SetFloat("xInput", dir.x);
        anim.SetFloat("yInput", dir.y);
    }

    public void StopMovement() => SetZeroVelocity(); // <- Entity API

    public float DistanceToPlayer() =>
        playerTarget ? Vector2.Distance(transform.position, playerTarget.position) : float.PositiveInfinity;

    public bool ShouldStartFollowing() =>
        playerTarget != null && DistanceToPlayer() > followStartDistance;

    public bool IsTooFarFromPlayer()
    {
        if (playerTarget == null) return false;
        return Vector2.Distance(transform.position, playerTarget.position) > maxFollowDistance;
    }

    public bool IsCloseEnoughToPlayer()
    {
        if (playerTarget == null) return false;
        return Vector2.Distance(transform.position, playerTarget.position) <= followStopDistance;
    }

    public void FaceTarget(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        // Snap to 4-directional for classic top-down anims
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            anim.SetFloat("xInput", direction.x > 0 ? 1 : -1);
            anim.SetFloat("yInput", 0);
        }
        else
        {
            anim.SetFloat("xInput", 0);
            anim.SetFloat("yInput", direction.y > 0 ? 1 : -1);
        }
    }

    // ====================================================================
    // Enemy sensing helpers (used by states)
    // ====================================================================

    /// <summary>True if at least one enemy is within chaseRadius.</summary>
    public bool HasEnemyInChaseRadius()
    {
        return Physics2D.OverlapCircle(transform.position, chaseRadius, enemyLayer) != null;
        // Or: return Physics2D.OverlapCircleAll(transform.position, chaseRadius, enemyLayer).Length > 0;
    }

    /// <summary>Closest enemy Transform within chaseRadius, or null if none.</summary>
    public Transform GetNearestEnemy()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, chaseRadius, enemyLayer);
        float closest = Mathf.Infinity;
        Transform best = null;

        foreach (var h in hits)
        {
            if (!h) continue;
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < closest) { closest = d; best = h.transform; }
        }
        return best;
    }

    public bool IsEnemyInAttackRange(Transform enemy)
    {
        if (!enemy) return false;
        return Vector2.Distance(transform.position, enemy.position) <= attackRange;
    }

    public bool IsEnemyInChaseRadius(Transform enemy)
    {
        if (!enemy) return false;
        return Vector2.Distance(transform.position, enemy.position) <= chaseRadius;
    }

    // ====================================================================
    // Party API
    // ====================================================================

    /// <summary>Called by CompanionPartyManager when recruiting/dismissing.</summary>
    public void SetInParty(bool value)
    {
        if (InParty == value) return;
        InParty = value;
        OnPartyFlagChanged?.Invoke(value);

        if (value)
        {
            if (playerTarget == null)
                TryBindFromLocatorOrWorld();

            if (followState != null && stateMachine?.currentState != null)
                stateMachine.ChangeState(followState);
            else
                _queueFollow = true;
        }
        else
        {
            if (idleState != null && stateMachine?.currentState != null)
                stateMachine.ChangeState(idleState);
            StopMovement();
        }
    }

    /// <summary>Call once immediately after Initialize(idleState).</summary>
    public void AfterStateMachineInitialized()
    {
        if ((InParty || StartInParty || _queueFollow) &&
            followState != null && stateMachine?.currentState != null)
        {
            _queueFollow = false;
            if (!InParty && StartInParty) InParty = true; // keep event noise low
            stateMachine.ChangeState(followState);
        }
    }

    // ====================================================================
    // Gizmos
    // ====================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, followStartDistance);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, followStopDistance);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, chaseRadius);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
