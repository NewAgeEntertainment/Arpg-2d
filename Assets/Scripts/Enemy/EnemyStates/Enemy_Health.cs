using UnityEngine;
using System.Collections;

#if REWIRED
using Rewired;
#endif

public class Enemy_Health : Entity_Health, IDamageable
{
    [Header("Despawn After Death Animation")]
    [Tooltip("Animator layer that plays the death state (usually 0).")]
    [SerializeField] private int animatorLayer = 0;

    [Tooltip("Exact Animator state NAME of the death state (e.g., 'dead' or 'Rabbie death 0').")]
    [SerializeField] private string deathStateName = "dead";

    [Tooltip("Force the Animator to play the death state immediately on death.")]
    [SerializeField] private bool forcePlayDeathState = true;

    [Tooltip("Extra delay after the death anim finishes before despawn.")]
    [SerializeField] private float extraDespawnDelay = 0.1f;

    [Tooltip("Hard timeout so we never hang, even if the state name is wrong or looping.")]
    [SerializeField] private float fallbackTimeout = 2.0f;

    [Tooltip("If true, SetActive(false) instead of Destroy (for pooling).")]
    [SerializeField] private bool usePooling = false;

    [Header("Debug / QA Kill")]
    [SerializeField] private bool enableKillOnInput = true;

#if REWIRED
    [SerializeField] private bool useRewired = true;
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private string rewiredKillAction = "DebugKillEnemy";
    private Rewired.Player rPlayer;
#endif

    [SerializeField] private KeyCode fallbackKillKey = KeyCode.K;

    [Header("Debug Logs")]
    [SerializeField] private bool debugLogs = true;

    private Animator anim;
    private Rigidbody2D rb2d;
    private Collider2D[] cols;
    private bool _despawnStarted;   // prevents double start

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponentInChildren<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        cols = GetComponentsInChildren<Collider2D>(true);

#if REWIRED
        TryCacheRewired();
#endif
    }

    private void OnEnable()
    {
        // If your Entity_Health exposes a Died event, hook it. Otherwise LateUpdate() handles the fallback.
        try { Died += HandleDied; } catch { /* ok if event doesn't exist */ }
    }

    private void OnDisable()
    {
        try { Died -= HandleDied; } catch { /* ok if event doesn't exist */ }
    }

    private void Update()
    {
        if (!enableKillOnInput || IsDead) return;

        // Rewired first (if enabled), otherwise fall back to a KeyCode.
#if REWIRED
        if (useRewired)
        {
            if (rPlayer == null) TryCacheRewired();
            if (rPlayer != null && rPlayer.GetButtonDown(rewiredKillAction))
            {
                KillNow();
                return;
            }
        }
#endif
        if (Input.GetKeyDown(fallbackKillKey))
        {
            KillNow();
        }
    }

    // Safety watcher: if Died wasn’t invoked for any reason, we still despawn when health reaches 0.
    private void LateUpdate()
    {
        if (!_despawnStarted && IsDead)
        {
            if (debugLogs) Debug.Log($"[{name}] LateUpdate saw IsDead=true; starting despawn.");
            HandleDied();
        }
    }

    /// <summary>Instantly kill this enemy (for QA/debug). Goes through the normal death flow.</summary>
    public void KillNow()
    {
        if (IsDead) return;
        if (debugLogs) Debug.Log($"[{name}] KillNow() invoked.");
        // Reduce exactly current health to reach 0 and trigger Die().
        ReduceHealth(GetCurrentHealth());
    }

    private void HandleDied()
    {
        if (_despawnStarted) return;
        _despawnStarted = true;

        // Freeze physics & make corpse non-interactive
        if (rb2d) { rb2d.velocity = Vector2.zero; rb2d.simulated = false; }
        if (cols != null) foreach (var c in cols) if (c) c.enabled = false;

        if (anim)
        {
            // Ensure the anim keeps updating even if you change timeScale on death
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;

            if (forcePlayDeathState && !string.IsNullOrEmpty(deathStateName))
            {
                // If name is wrong, CrossFade does nothing—fallback below handles timing.
                anim.CrossFadeInFixedTime(deathStateName, 0f, animatorLayer, 0f);
                if (debugLogs) Debug.Log($"[{name}] CrossFade to '{deathStateName}' on layer {animatorLayer}.");
            }
        }
        else if (debugLogs) Debug.LogWarning($"[{name}] No Animator found; using hard timeout.");

        StartCoroutine(DespawnAfterDeathAnim());
    }

    private IEnumerator DespawnAfterDeathAnim()
    {
        float startRT = Time.realtimeSinceStartup;
        bool matchedDeadState = false;

        if (anim != null)
        {
            // 1) Wait until we actually enter the named death state or timeout
            while (Time.realtimeSinceStartup - startRT < fallbackTimeout)
            {
                var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
                if (!string.IsNullOrEmpty(deathStateName) && st.IsName(deathStateName))
                {
                    matchedDeadState = true;
                    if (debugLogs) Debug.Log($"[{name}] Entered state '{deathStateName}'. Waiting for completion…");

                    float clipLen = GetCurrentClipLength(anim, animatorLayer);

                    if (!st.loop)
                    {
                        while (st.normalizedTime < 1f)
                        {
                            yield return null;
                            st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
                        }
                    }
                    else
                    {
                        if (clipLen > 0f) yield return new WaitForSecondsRealtime(clipLen);
                    }
                    break;
                }
                yield return null;
            }

            // 2) Fallback: if we never matched the state name, wait clip length or timeout
            if (!matchedDeadState)
            {
                float clipLen = GetCurrentClipLength(anim, animatorLayer);
                if (debugLogs)
                    Debug.LogWarning($"[{name}] Never matched state '{deathStateName}'. " +
                                     $"Fallback wait: {(clipLen > 0f ? $"{clipLen:0.00}s (clip)" : $"{fallbackTimeout:0.00}s (timeout)")}");

                if (clipLen > 0f) yield return new WaitForSecondsRealtime(clipLen);
                else yield return new WaitForSecondsRealtime(fallbackTimeout);
            }
        }
        else
        {
            // No Animator at all — just wait the timeout
            yield return new WaitForSecondsRealtime(fallbackTimeout);
        }

        if (extraDespawnDelay > 0f)
            yield return new WaitForSecondsRealtime(extraDespawnDelay);

        if (usePooling)
        {
            if (debugLogs) Debug.Log($"[{name}] Despawn -> SetActive(false).");
            gameObject.SetActive(false);
        }
        else
        {
            if (debugLogs) Debug.Log($"[{name}] Despawn -> Destroy(gameObject).");
            Destroy(gameObject);
        }
    }

    private static float GetCurrentClipLength(Animator animator, int layer)
    {
        var info = animator.GetCurrentAnimatorClipInfo(layer);
        return (info != null && info.Length > 0 && info[0].clip != null) ? info[0].clip.length : 0f;
    }

#if REWIRED
    private void TryCacheRewired()
    {
        try { rPlayer = ReInput.players.GetPlayer(rewiredPlayerId); } catch { rPlayer = null; }
    }
#endif

    // Damage remains base behavior
    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        return base.TakeDamage(damage, elementalDamage, element, damageDealer);
    }
}
