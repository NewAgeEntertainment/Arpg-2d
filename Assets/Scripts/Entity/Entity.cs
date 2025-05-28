using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    protected StateMachine stateMachine;

    public Vector2 currentDir;

    //private bool facingRight = true;
    //public int facingDir { get; private set; }

    [Header("KnockBack info")]
    //[SerializeField] protected float knockbackDuration;// this float variable is to swap 0.5 with your own inside the inspector // Duration of the knockback effect
    //public float knockbackForce; // Force applied during knockback
    private Coroutine knockbakCo;
    private bool isKnocked; // Flag to check if the entity is knocked back

    public Transform player { get; private set; } // Reference to the player transform

    //[Header("Collision detection")]
    //[SerializeField] protected LayerMask whatIsGround;
    //[SerializeField] private float groundCheckDistance;
    //[SerializeField] private float wallCheckDistance;
    //[SerializeField] private Transform groundCheck;
    //[SerializeField] private Transform primaryWallCheck;
    //[SerializeField] private Transform secondaryWallCheck;
    //public bool groundDetected { get; private set; }
    //public bool wallDetected { get; private set; }

    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine = new StateMachine();
    }


    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {



        // HandleCollisionDetection();
        stateMachine.UpdateActiveState();
    }



    //public virtual void Knockback(Transform playerTransform, float knockbackForce)
    //{
    //    // Calculate the knockback direction based on the player's position
    //    Vector2 knockbackDirection = (transform.position - playerTransform.position).normalized; // Normalize the direction vector
    //    rb.linearVelocity = knockbackDirection * knockbackForce; // Apply the knockback force to the rigidbody
    //    Debug.Log("knockback works");
    //}

    public void ReciveKnockback(Vector2 knockback, float duration)
    {
        if (knockbakCo != null)
            StopCoroutine(knockbakCo); // Stop any existing knockback coroutine
        knockbakCo = StartCoroutine(KnockbackCo(knockback, duration)); // Start the knockback coroutine
    }

    public virtual IEnumerator KnockbackCo(Vector2 knockback, float duration)
    {
        isKnocked = true;
        rb.linearVelocity = knockback; // Apply the knockback force to the rigidbody

        yield return new WaitForSeconds(duration);//(0.5f) I use a variable for the duration of the knockback instead of a hardcoded value

        rb.linearVelocity = Vector2.zero; // Reset the velocity after the knockback duration
        isKnocked = false;
    }

    public void CurrentStateAnimationTrigger()
    {
        // Call the AnimationTrigger method of the current state
        stateMachine.currentState.AnimationTrigger();
    }

    public virtual void EntityDeath()
    {
        // Handle entity death logic here
        // For example, play death animation, disable the entity, etc.
        //overrideable on other scripts because of virtual


    }

    public void SetZeroVelocity()
    {
        if (isKnocked)
            return;
        rb.linearVelocity = Vector2.zero;
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        if (isKnocked)
            return;

        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        //HandleFlip(xVelocity);
    }

    //public void HandleFlip(float xVelcoity)
    //{
    //    if (xVelcoity > 0 && facingRight == false)
    //        Flip();
    //    else if (xVelcoity < 0 && facingRight)
    //        Flip();
    //}

    //public void Flip()
    //{
    //    transform.Rotate(0, 180, 0);
    //    facingRight = !facingRight;
    //    facingDir = facingDir * -1;
    //}

    //private void HandleCollisionDetection()
    //{
    //    groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);


    //    if (secondaryWallCheck != null)
    //    {
    //        wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround)
    //                    && Physics2D.Raycast(secondaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    //    }
    //    else
    //        wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);



    //}


    protected virtual void OnDrawGizmos()
    {
        //    if (attackCheck != null)
        //    {
        //        Gizmos.color = Color.white;
        //        foreach (var check in attackCheck)
        //        {
        //            if (check != null)
        //            {
        //                Gizmos.DrawWireSphere(check.position, attackCheckRadius); // Draw a wire sphere for attack check  
        //            }
        //        }
        //    }


    }
}
