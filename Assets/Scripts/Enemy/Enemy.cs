using System.Collections;
using UnityEngine;

public class Enemy : Entity
{
    public EntityState previousState;

    public Enemy_IdleState idleState;
    public Enemy_MoveState moveState;
    public Enemy_AttackState attackState;
    public Enemy_BattleState battleState;
    public Enemy_PatrollingState patrollingState;
    public Enemy_StunnedState stunnedState;
    public Enemy_DeadState deadState;

    [Header("Attack info")]
    public float attackDistance;
    public float attackCooldown;
    public float range; // Range for the enemy's detection of the player
    [SerializeField] protected LayerMask whatIsPlayer; // Layer mask for the player layer
    [SerializeField] public GameObject attackIndicator; // Reference to the attack signal GameObject
    [HideInInspector] public float lastTimeAttacked;
    public float battleMoveSpeed = 3f;

    [Header("Stunned State details")]
    public float stunnedDuration = 1; // Duration of the stunned state
    public Vector2 stunnedVelocity = new Vector2(7, 7); // Velocity during the stunned state
    [SerializeField] protected bool canBeStunned; // Flag to check if the enemy is stunned

    [Header("Movement details")]
    public float idleTime;
    public float moveSpeed = 1.4f;
    public float pauseDuration;
    public float battleTime; // Time the enemy stays in battle state
    [Range(0, 2)]
    public float moveAnimSpeedMultiplier = 1;

    [Header("Patrol details")]
    public Vector2[] patrolPoints;
    public int currentPatrolIndex;
    public bool isPaused { get; set; } // Flag to check if the enemy is paused

    public Vector2 currentDirection { get; private set; } // Current direction of the enemy

    public Vector2 target;

    [Header("Player detection")]
    public Transform player { get; private set; } // Reference to the player transform

    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalSpeed = moveSpeed; // Store the original speed of the enemy
        float originalBattleSpeed = battleMoveSpeed;
        float oringinalAnimSpeed = anim.speed; // Store the original animation speed multiplier

        float speedMultiplier = 1 - slowMultiplier; // Calculate the speed multiplier based on the slow multiplier

        moveSpeed = moveSpeed * speedMultiplier; // Apply the speed multiplier to the enemy's move speed
        battleMoveSpeed = battleMoveSpeed * speedMultiplier; // Apply the speed multiplier to the enemy's battle move speed
        anim.speed = anim.speed * speedMultiplier; // Apply the speed multiplier to the enemy's animation speed


        yield return new WaitForSeconds(duration);

        // after Yield Return, Reset the enemy's speed and animation to their original values after the slowdown duration
        moveSpeed = originalSpeed; // Reset the enemy's move speed to the original value
        battleMoveSpeed = originalBattleSpeed; // Reset the enemy's battle move speed to the original value
        anim.speed = oringinalAnimSpeed; // Reset the enemy's animation speed to the original value
    }

    // Added missing 'range' field
    //[Header("Detection Range")]
    //[SerializeField] private float range = 5;

    // tryEnterBattleState method is in cause I want to enemy to enter battle state when player attacks
    //public void TryEnterBattleState(Transform player)
    //{

    //    if (stateMachine.currentState == battleState || stateMachine.currentState == attackState) // Check if the current state is already battle state
    //        return;

    //    this.player = player;
    //    stateMachine.ChangeState(battleState); // Change to the battle state
    //}

    public void EnableCounterWindow(bool enable) => canBeStunned = enable; // Enable or disable the counter window for the enemy
    //  (EnableCoounterWindow) returns canBeStunned to true or false

    public override void EntityDeath()
    {
        base.EntityDeath();
        stateMachine.ChangeState(deadState); // Change to the dead state
    }

    public void HandlePlayerDeath()
    {
        // Handle how enemy deal player death. logic here
        // For example, you can trigger a game over screen or respawn the player.
        // or give the enemy a demand.
        stateMachine.ChangeState(idleState); // Change to the dead state
        Debug.Log("Player has died");
    }

    public Transform GetPlayerReference()
    {
        if (player == null)
            player = PlayerDetected().transform; // Get the player reference if not already set

        return player; // Return the player reference
    }

    protected override void Awake()
    {
        base.Awake();
        //target = patrolPoints[0].position // Initialize the target to the first patrol point


    }

    protected override void Start()
    {
        base.Start();
        // Initialize the target to the first patrol point
        StartCoroutine(SetPatrolPoint()); // Move to the next patrol point 
    }



    protected override void Update()
    {
        base.Update();

        stateMachine.currentState.Update(); // Update the current state of the enemy
        //if (isPaused == true)
        //{
        //    rb.linearVelocity = Vector2.zero; // Stop the enemy's movement when paused
        //    Debug.Log("Enemy is paused");
        //    return;
        //}

        //if (isKnocked == true)
        //    return;

        //Vector2 movedirection = ((Vector3)target - transform.position).normalized; // Calculate the direction to the target point  
        //rb.linearVelocity = movedirection * moveSpeed; // Set the enemy's velocity towards the target  

        //if (Vector2.Distance(transform.position, target) < .1f) // check if the enemy has reached the target point  
        //{
        //    StartCoroutine(SetPatrolPoint()); // Move to the next patrol point  
        //}

        stateMachine.currentState.Update();

    }

    //public virtual void ShowAttackIndicator()
    //{
    //    attackIndicator.SetActive(true); // Activate the attack signal
    //}

    //public virtual void HideAttackIndicator()
    //{
    //    attackIndicator.SetActive(false); // Deactivate the attack signal
    //}

    //public virtual void Knockback(Transform playerTransform, float knockbackForce)
    //{
    //    // Calculate the knockback direction based on the player's position
    //    Vector2 knockbackDirection = (transform.position - playerTransform.position).normalized; // Normalize the direction vector
    //    rb.linearVelocity = knockbackDirection * knockbackForce; // Apply the knockback force to the rigidbody
    //    Debug.Log("knockback works");
    //}

    //public virtual IEnumerator HitKnockBack()
    //{
    //    isKnocked = true;
    //    yield return new WaitForSeconds(knockbackDuration);//(0.5f) I use a variable for the duration of the knockback instead of a hardcoded value
    //    isKnocked = false;
    //}



    public virtual IEnumerator SetPatrolPoint() // set patrol point  
    {
        isPaused = true; // Set the pause flag to true  

        yield return new WaitForSeconds(pauseDuration); // Wait for the specified pause duration
        currentDir = target - (Vector2)transform.position; // Calculate the direction to the target  
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; // Ensure index wraps around using modulus operator  
        target = patrolPoints[currentPatrolIndex]; // set the target to the next patrol point  
        isPaused = false; // Set the pause flag to false
        currentDir = target - (Vector2)transform.position; // Calculate the direction to the target  
    }

    public virtual bool IsPlayerDetected() => Physics2D.OverlapCircle(transform.position, range, whatIsPlayer);

    public virtual Collider2D PlayerDetected()
    {
        return Physics2D.OverlapCircle(transform.position, range, whatIsPlayer); // Return the Collider2D detected within the radius  
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); // Call the base class OnDrawGizmos method  

        // Draw a wire sphere to visualize the enemy's detection range  
        Gizmos.color = Color.red; // Set the color of the gizmo to red  
        Gizmos.DrawWireSphere(transform.position, range); // Draw a wire sphere at the enemy's position with the specified range  

        // Fixing the problematic line by correctly accessing the position property of the Transform objects  
        Gizmos.color = Color.yellow;
        Vector3 attackRangePosition = new Vector3(transform.position.x, transform.position.y, 0); // Set the attack range position to the enemy's position  
        Gizmos.DrawWireSphere(attackRangePosition, attackDistance); // Draw a small sphere to represent the attack range  

        // Draw a line between all the patrol points  
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (i == patrolPoints.Length - 1)
            {
                Gizmos.DrawLine(patrolPoints[i], patrolPoints[0]); // Access the position property of the Transform objects  
            }
            else
            {
                Gizmos.DrawLine(patrolPoints[i], patrolPoints[i + 1]); // Access the position property of the Transform objects  
            }
        }
    }

    private void InEnable()
    {
        // subscribe to the Player.OnPlayerDeath event when the enemy is disabled
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        // Unsubscribe from the Player.OnPlayerDeath event when the enemy is disabled
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }




}
