using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Animator anim { get; private set; }     // gameplay (child) animator
    public Rigidbody2D rb { get; private set; }
    public Entity_SFX sfx { get; private set; }
    public StateMachine stateMachine { get; protected set; }

    [HideInInspector] public Vector2 currentDir;

    [Header("KnockBack info")]
    protected bool isKnocked;
    private Coroutine knockbakCo;
    private Coroutine slowDownCo;

    // --- Defer movement to FixedUpdate ---
    private Vector2 _desiredVelocity;
    private bool _hasDesiredVelocityThisFrame;

    protected virtual void Awake()
    {
        // DO NOT: anim = GetComponentInChildren<Animator>();
        anim = ResolveGameplayAnimator();               // <-- key change
        sfx = GetComponent<Entity_SFX>();
        rb = GetComponent<Rigidbody2D>();
        stateMachine = new StateMachine();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!Application.isPlaying)
            anim = ResolveGameplayAnimator();
    }
#endif

    // Picks the child animator that actually has a controller.
    private Animator ResolveGameplayAnimator()
    {
        // Prefer explicit mapping from TimelineOnlyAnimator if present
        var dual = GetComponent<TimelineOnlyAnimator>();
        if (dual && dual.gameplayAnimator && dual.gameplayAnimator.runtimeAnimatorController != null)
            return dual.gameplayAnimator;

        // Otherwise, scan children and choose the first with a controller (skip root)
        var anims = GetComponentsInChildren<Animator>(true);
        foreach (var a in anims)
        {
            if (a == null) continue;
            if (a.gameObject == this.gameObject) continue; // skip root animator
            if (a.runtimeAnimatorController != null) return a;
        }

        // Fallback: return any child animator (may be null)
        foreach (var a in anims)
            if (a != null && a.gameObject != this.gameObject) return a;

        return null;
    }

    // Call this after spawning/reparenting if needed
    public void ReacquireAnimatorIfNeeded()
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            anim = ResolveGameplayAnimator();
    }

    protected virtual void Start() { }

    protected virtual void Update()
    {
        stateMachine.UpdateActiveState();
    }

    protected virtual void FixedUpdate()
    {
        if (rb == null) return;
        if (isKnocked) return;

        // Always move using the last requested velocity
        Vector2 next = rb.position + _desiredVelocity * Time.fixedDeltaTime;
        rb.MovePosition(next);
    }

    // ---------------- Movement API ----------------
    public void SetZeroVelocity()
    {
        if (isKnocked) return;
        _desiredVelocity = Vector2.zero;
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        if (isKnocked) return;
        _desiredVelocity = new Vector2(xVelocity, yVelocity);
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

    public void CancelKnockbackImmediate()
    {
        if (knockbakCo != null) StopCoroutine(knockbakCo);
        isKnocked = false;
        if (rb != null) rb.velocity = Vector2.zero;
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
