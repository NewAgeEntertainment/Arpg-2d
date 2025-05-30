using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    protected StateMachine stateMachine;

    public Vector2 currentDir;

    [Header("KnockBack info")]
    // Condition Variable
    private bool isKnocked; // Flag to check if the entity is knocked back
    private Coroutine knockbakCo;
    private Coroutine slowDownCo;

    public Transform player { get; private set; } // Reference to the player transform

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

    public virtual void SlowDownEntity(float duration, float slowMultiplier)
    {
        if (slowDownCo != null)
            StopCoroutine(slowDownCo); // Stop any existing slowdown coroutine

        slowDownCo = StartCoroutine(SlowDownEntityCo(duration, slowMultiplier)); // Start the slowdown coroutine
    }

    protected virtual IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        yield return null; // Wait for the next frame to ensure the entity is not in a state that prevents slowing down
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

   
    protected virtual void OnDrawGizmos()
    {
       
    }
}
