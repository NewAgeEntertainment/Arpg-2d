using System.Collections;
using UnityEngine;

public class Enemy : Entity
{
    public EntityState previousState;
    public Entity_Stats stats { get; private set; }
    public Enemy_IdleState idleState;
    public Enemy_MoveState moveState;
    public Enemy_AttackState attackState;
    public Enemy_BattleState battleState;
    public Enemy_PatrollingState patrollingState;
    public Enemy_StunnedState stunnedState;
    public Enemy_DeadState deadState;

    [Header("Item Drop")]
    [SerializeField] private ItemListDataSO dropList;
    [SerializeField] private GameObject itemPickupPrefab;
    [SerializeField] private int minDrop = 1;
    [SerializeField] private int maxDrop = 3;
    [SerializeField] private float burstRadius = 3f;

    [Header("Gold Drop")]
    [SerializeField] private bool dropGold = true;
    [SerializeField] private int minGold = 10;
    [SerializeField] private int maxGold = 50;
    [SerializeField] private int goldStackMin = 5;
    [SerializeField] private int goldStackMax = 15;
    [SerializeField] private GameObject goldPickupPrefab;

    [Header("Attack info")]
    public float attackDistance;
    public float attackCooldown;
    public float range;
    [SerializeField] protected LayerMask whatIsPlayer;
    [SerializeField] public GameObject attackIndicator;
    [HideInInspector] public float lastTimeAttacked;
    public float battleMoveSpeed = 3f;

    // --- Attack Lunge (simple, constant) ---
    [Header("Attack Lunge")]
    [Tooltip("How fast the enemy lunges at the start of an attack.")]
    public float attackLungeSpeed = 6f;

    [Tooltip("How long the lunge push lasts (seconds). After this, movement is locked until the attack finishes.")]
    public float attackLungeDuration = 0.12f;

    // --- Attack Super-Armor / Stun override ---
    [Header("Attack Super-Armor / Overrides")]
    [Tooltip("If true, knockback is ignored while attacking.")]
    public bool superArmorDuringAttack = true;

    [Tooltip("If true, stun is ignored/cancelled while attacking.")]
    public bool ignoreStunDuringAttack = true;

    // set true in Attack.Enter, false in Attack.Exit
    public bool IsAttacking { get; set; }

    [Header("Stunned State details")]
    public float stunnedDuration = 1;
    public Vector2 stunnedVelocity = new Vector2(7, 7);
    [SerializeField] protected bool canBeStunned;

    [Header("Movement details")]
    public float idleTime;
    public float moveSpeed = 1.4f;
    public float pauseDuration;
    public float battleTime;
    [Range(0, 2)] public float moveAnimSpeedMultiplier = 1;

    [Header("Patrol details")]
    public Vector2[] patrolPoints;
    public int currentPatrolIndex;
    public bool isPaused { get; set; }
    private Vector2 _patrolOrigin;

    private Vector2 _spawnPosition;
    public Vector2 LastDir { get; private set; } = Vector2.down;
    public Vector2 currentDirection { get; set; }    // <-- change to set;
    public Vector2 target;

    public Transform player { get; private set; }

    [Header("Targeting")]
    [Tooltip("Optional point where the soft-lock reticle should appear (e.g. above head).")]
    public Transform lockOnPoint;

    public Transform GetLockOnPoint()
    {
        return lockOnPoint != null ? lockOnPoint : transform;
    }

    // ------------ NEW: setter so states can write currentDirection ------------
    public void SetCurrentDirection(Vector2 dir)
    {
        currentDirection = dir;
    }
    // -------------------------------------------------------------------------

    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalSpeed = moveSpeed;
        float originalBattleSpeed = battleMoveSpeed;
        float originalAnimSpeed = anim.speed;
        float speedMultiplier = 1 - slowMultiplier;

        moveSpeed *= speedMultiplier;
        battleMoveSpeed *= speedMultiplier;
        anim.speed *= speedMultiplier;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed;
        battleMoveSpeed = originalBattleSpeed;
        anim.speed = originalAnimSpeed;
    }

    public void EnableCounterWindow(bool enable) => canBeStunned = enable;

    public override void EntityDeath()
    {
        base.EntityDeath();
        DropItems();
        stateMachine.ChangeState(deadState);
    }

    private void DropItems()
    {
        if (dropList != null && itemPickupPrefab != null && dropList.itemList.Length > 0)
        {
            int dropCount = Random.Range(minDrop, maxDrop + 1);
            for (int i = 0; i < dropCount; i++)
            {
                ItemDataSO itemData = dropList.itemList[Random.Range(0, dropList.itemList.Length)];
                GameObject drop = Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
                Object_ItemPickup pickup = drop.GetComponent<Object_ItemPickup>();
                if (pickup != null)
                {
                    pickup.SetupItem(itemData);
                    pickup.ApplyBurst(Random.insideUnitCircle * burstRadius);
                }
            }
        }

        if (dropGold && goldPickupPrefab != null)
        {
            int totalGold = Random.Range(minGold, maxGold + 1);
            while (totalGold > 0)
            {
                int goldChunk = Mathf.Min(totalGold, Random.Range(goldStackMin, goldStackMax + 1));
                totalGold -= goldChunk;

                GameObject goldDrop = Instantiate(goldPickupPrefab, transform.position, Quaternion.identity);
                Object_ItemPickup pickup = goldDrop.GetComponent<Object_ItemPickup>();
                if (pickup != null)
                {
                    pickup.SetupGold(goldChunk);
                    pickup.ApplyBurst(Random.insideUnitCircle * burstRadius);
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                if (patrolPoints[0] == Vector2.zero)
                    patrolPoints[0] = (Vector2)transform.position;
            }
        }
    }
#endif


    public void HandlePlayerDeath()
    {
        stateMachine.ChangeState(idleState);
        Debug.Log("Player has died");
    }

    public Transform GetPlayerReference()
    {
        if (player == null)
            player = PlayerDetected()?.transform;
        return player;
    }

    protected override void Awake()
    {
        base.Awake();
        stats = GetComponent<Entity_Stats>();
        _spawnPosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();

        // Only auto-set if designer left it at (0,0)
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] == Vector2.zero)
        {
            patrolPoints[0] = transform.position;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            target = patrolPoints[currentPatrolIndex];
            StartCoroutine(SetPatrolPoint());
        }
    }


    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
    }

    public virtual IEnumerator SetPatrolPoint()
    {
        // No patrol → nothing to do
        if (patrolPoints == null || patrolPoints.Length == 0)
            yield break;

        isPaused = true;
        yield return new WaitForSeconds(pauseDuration);

        // If index somehow got out of range, reset
        if (currentPatrolIndex < 0 || currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;
        else
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

        target = patrolPoints[currentPatrolIndex];

        // Update facing direction for anim/combat
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        currentDirection = dir;

        isPaused = false;
    }



    private Vector2 GetWorldPatrolPoint(int index)
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return _patrolOrigin;

        index = Mathf.Clamp(index, 0, patrolPoints.Length - 1);
        return _patrolOrigin + patrolPoints[index];   // spawn position + offset
    }


    public virtual bool IsPlayerDetected() => Physics2D.OverlapCircle(transform.position, range, whatIsPlayer);
    public virtual Collider2D PlayerDetected() => Physics2D.OverlapCircle(transform.position, range, whatIsPlayer);

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // --- Detection range (aggro) ---
        if (range > 0f)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.35f); // yellow-ish
            Gizmos.DrawWireSphere(transform.position, range);
        }

        // --- Attack range ---
        if (attackDistance > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);   // red
            Gizmos.DrawWireSphere(transform.position, attackDistance);
        }

        // --- Existing patrol path gizmos ---
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 p = patrolPoints[i];
            Vector3 q = patrolPoints[(i + 1) % patrolPoints.Length];

            Gizmos.DrawSphere(p, 0.1f);
            Gizmos.DrawLine(p, q);
        }
    }
#endif




    // helper checked by damage/KB code
    public bool ShouldIgnoreKnockback()
    {
        return superArmorDuringAttack && IsAttacking;
    }

    private void InEnable()
    {
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }

    public void SetFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
            LastDir = dir.normalized;

        anim.SetFloat("xInput", LastDir.x);
        anim.SetFloat("yInput", LastDir.y);
    }
}
