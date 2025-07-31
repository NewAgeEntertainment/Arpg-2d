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
    public Vector2 currentDirection { get; private set; }
    public Vector2 target;
    public Transform player { get; private set; }

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
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(SetPatrolPoint());
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
    }

    public virtual IEnumerator SetPatrolPoint()
    {
        isPaused = true;
        yield return new WaitForSeconds(pauseDuration);
        currentDirection = target - (Vector2)transform.position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        target = patrolPoints[currentPatrolIndex];
        isPaused = false;
        currentDirection = target - (Vector2)transform.position;
    }

    public virtual bool IsPlayerDetected() => Physics2D.OverlapCircle(transform.position, range, whatIsPlayer);
    public virtual Collider2D PlayerDetected() => Physics2D.OverlapCircle(transform.position, range, whatIsPlayer);

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.yellow;
        Vector3 attackRangePosition = new Vector3(transform.position.x, transform.position.y, 0);
        Gizmos.DrawWireSphere(attackRangePosition, attackDistance);

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (i == patrolPoints.Length - 1)
                Gizmos.DrawLine(patrolPoints[i], patrolPoints[0]);
            else
                Gizmos.DrawLine(patrolPoints[i], patrolPoints[i + 1]);
        }
    }

    private void InEnable()
    {
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }
}
