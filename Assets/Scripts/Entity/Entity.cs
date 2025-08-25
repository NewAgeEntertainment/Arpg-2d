using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }

    public StateMachine stateMachine { get; protected set; }

    [HideInInspector] public Vector2 currentDir;

    [Header("KnockBack info")]
    protected bool isKnocked;           // while true, movement code won’t overwrite physics
    private Coroutine knockbakCo;
    private Coroutine slowDownCo;

    // --- New: defer movement to FixedUpdate (prevents tunneling) ---
    private Vector2 _desiredVelocity;   // units/second
    private bool _hasDesiredVelocityThisFrame;

    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        stateMachine = new StateMachine();

        // Harden RB2D so collisions behave for top-down
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    protected virtual void Start() { }

    protected virtual void Update()
    {
        stateMachine.UpdateActiveState();
        // NOTE: do NOT write rb.velocity here anymore.
        // States can keep calling SetVelocity()/SetZeroVelocity(); we’ll apply in FixedUpdate.
    }

    // --- Apply movement ONLY in the physics step ---
    protected virtual void FixedUpdate()
    {
        if (rb == null) return;

        // If knockback is pushing us, don't fight physics with our own move
        if (isKnocked) return;

        if (_hasDesiredVelocityThisFrame)
        {
            // Convert velocity (units/s) into a position step and sweep against colliders
            Vector2 next = rb.position + _desiredVelocity * Time.fixedDeltaTime;
            rb.MovePosition(next);
        }
        else
        {
            // If nothing requested movement this frame, stop
            rb.velocity = Vector2.zero;
        }

        _hasDesiredVelocityThisFrame = false; // clear request until next frame
    }

    // ---------------- Movement API (unchanged signature) ----------------

    public void SetZeroVelocity()
    {
        if (isKnocked) return;
        _desiredVelocity = Vector2.zero;
        _hasDesiredVelocityThisFrame = true;
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        if (isKnocked) return;
        _desiredVelocity = new Vector2(xVelocity, yVelocity);
        _hasDesiredVelocityThisFrame = true;
        // (No rb.velocity write here; FixedUpdate will move us reliably.)
    }

    // ---------------- Knockback ----------------

    public void ReciveKnockback(Vector2 knockback, float duration)
    {
        if (knockbakCo != null) StopCoroutine(knockbakCo);
        knockbakCo = StartCoroutine(KnockbackCo(knockback, duration));
    }

    public virtual IEnumerator KnockbackCo(Vector2 knockback, float duration)
    {
        isKnocked = true;
        if (rb != null) rb.velocity = knockback;
        yield return new WaitForSeconds(duration);
        if (rb != null) rb.velocity = Vector2.zero;
        isKnocked = false;
    }

    // --------------- States / Anim ---------------

    public void CurrentStateAnimationTrigger()
    {
        stateMachine.currentState.AnimationTrigger();
    }

    public virtual void EntityDeath() { }

    public virtual void SlowDownEntity(float duration, float slowMultiplier)
    {
        if (slowDownCo != null) StopCoroutine(slowDownCo);
        slowDownCo = StartCoroutine(SlowDownEntityCo(duration, slowMultiplier));
    }

    protected virtual IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        yield return null;
    }

    protected virtual void OnDrawGizmos() { }
}
