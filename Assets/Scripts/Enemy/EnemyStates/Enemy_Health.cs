// Enemy_Health.cs
using UnityEngine;
using System.Collections;
using System.Reflection;   // for safe, flexible EXP calls

#if REWIRED
using Rewired;
#endif

/// <summary>
/// Health for enemies. Plays death anim, despawns, and now grants EXP to the Player
/// and (if applicable) to the Companion who dealt the killing blow.
/// </summary>
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

    // ---------- Attack Super-Armor / Stun Override ----------
    [Header("Attack Overrides")]
    [Tooltip("Ignore/clear knockback while this enemy is in its Attack state.")]
    [SerializeField] private bool superArmorDuringAttack = true;

    [Tooltip("If something forces Stun during an attack, immediately leave Stunned and return to Battle.")]
    [SerializeField] private bool ignoreStunDuringAttack = true;

    // ----- caches -----
    private Enemy enemy;         // to peek at states
    private Animator anim;
    private Rigidbody2D rb2d;
    private Collider2D[] cols;
    private bool _despawnStarted;

    // reflection cache to cancel knockback from Entity
    private static FieldInfo _fiIsKnocked;
    private static FieldInfo _fiKnockbackCo;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponentInChildren<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        cols = GetComponentsInChildren<Collider2D>(true);
        enemy = GetComponent<Enemy>();

        if (_fiIsKnocked == null)
            _fiIsKnocked = typeof(Entity).GetField("isKnocked", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (_fiKnockbackCo == null)
            _fiKnockbackCo = typeof(Entity).GetField("knockbakCo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

#if REWIRED
        TryCacheRewired();
#endif
    }

    private void OnEnable()
    {
        // Base raises these:
        OnDied += HandleDied;
        OnRevived += HandleRevived;

        // NEW: includes the last damage dealer transform so we can reward companions
        OnDiedWithKiller += HandleDiedWithKiller;
    }

    private void OnDisable()
    {
        OnDied -= HandleDied;
        OnRevived -= HandleRevived;
        OnDiedWithKiller -= HandleDiedWithKiller;
    }

    private void Update()
    {
        if (!enableKillOnInput || IsDead) return;

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
            KillNow();
    }

    // Safety watcher: if OnDied wasn't caught for any reason, we still despawn when health reaches 0.
    private void LateUpdate()
    {
        if (!_despawnStarted && IsDead)
        {
            if (debugLogs) Debug.Log($"[{name}] LateUpdate saw IsDead=true; starting despawn.");
            HandleDied();
        }

        // If we should ignore Stun during attack and somehow ended up in Stunned, pop back to Battle.
        if (ignoreStunDuringAttack && IsCurrentlyAttacking())
        {
            var cur = enemy != null ? enemy.stateMachine.currentState : null;
            if (enemy != null && cur == enemy.stunnedState)
            {
                if (debugLogs) Debug.Log($"[{name}] Attack overrides Stunned -> returning to Battle.");
                enemy.stateMachine.ChangeState(enemy.battleState);
            }
        }
    }

    /// <summary>Instantly kill this enemy (for QA/debug). Goes through the normal death flow.</summary>
    public void KillNow()
    {
        if (IsDead) return;
        if (debugLogs) Debug.Log($"[{name}] KillNow() invoked.");
        ReduceHealth(GetCurrentHealth()); // triggers Die() in base
    }

    // ========================= Death / Despawn =========================

    private void HandleDied()
    {
        if (_despawnStarted) return;
        _despawnStarted = true;

        if (rb2d) { rb2d.velocity = Vector2.zero; rb2d.simulated = false; }
        if (cols != null) foreach (var c in cols) if (c) c.enabled = false;

        if (anim)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            if (forcePlayDeathState && !string.IsNullOrEmpty(deathStateName))
            {
                anim.CrossFadeInFixedTime(deathStateName, 0f, animatorLayer, 0f);
                if (debugLogs) Debug.Log($"[{name}] CrossFade to '{deathStateName}' on layer {animatorLayer}.");
            }
        }
        else if (debugLogs) Debug.LogWarning($"[{name}] No Animator found; using hard timeout.");

        StartCoroutine(DespawnAfterDeathAnim());
    }

    private void HandleRevived()
    {
        _despawnStarted = false;
        if (rb2d) rb2d.simulated = true;
        if (cols != null) foreach (var c in cols) if (c) c.enabled = true;
        if (debugLogs) Debug.Log($"[{name}] Revived; physics & colliders re-enabled.");
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

            // 2) Fallback if we never matched:
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

    // ========================= Super-Armor / Stun =========================

    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        bool result = base.TakeDamage(damage, elementalDamage, element, damageDealer);

        // If we're in the Attack state and super-armor is enabled, cancel / ignore knockback
        if (result && superArmorDuringAttack && IsCurrentlyAttacking())
        {
            TryCancelKnockbackImmediate();
        }

        // If stun should be ignored during attack, kick out of Stunned right away (if something forced it)
        if (ignoreStunDuringAttack && IsCurrentlyAttacking())
        {
            var cur = enemy != null ? enemy.stateMachine.currentState : null;
            if (enemy != null && cur == enemy.stunnedState)
            {
                if (debugLogs) Debug.Log($"[{name}] Damage tried to stun during attack -> returning to Battle.");
                enemy.stateMachine.ChangeState(enemy.battleState);
            }
        }

        return result;
    }

    private bool IsCurrentlyAttacking()
    {
        return enemy != null
            && enemy.stateMachine != null
            && enemy.attackState != null
            && enemy.stateMachine.currentState == enemy.attackState;
    }

    private void TryCancelKnockbackImmediate()
    {
        if (enemy == null) return;

        try
        {
            var co = _fiKnockbackCo?.GetValue(enemy) as Coroutine;
            if (co != null)
                enemy.StopCoroutine(co);

            _fiIsKnocked?.SetValue(enemy, false);
        }
        catch { /* reflection may fail in IL2CPP; fail-soft */ }

        if (enemy.rb != null) enemy.rb.velocity = Vector2.zero;
        enemy.SetVelocity(0f, 0f);

        if (debugLogs) Debug.Log($"[{name}] Super-armor: canceled knockback during attack.");
    }

    // ========================= EXP distribution =========================

    /// <summary>
    /// Raised by base class when we die, including the last damage dealer Transform.
    /// Here we grant EXP to Player (always) and the killer Companion (if any).
    /// </summary>
    private void HandleDiedWithKiller(Transform killer)
    {
        // 👉 call the reward hook (e.g., companion mana on kill)
        TryGrantRewardsToKiller(killer);

        var comp = killer ? killer.GetComponentInParent<Companion>() : null;

        float exp = GetExpRewardSafe();
        if (exp <= 0f) return;

        RewardPlayer(exp);

        if (comp != null)
            RewardCompanion(comp, exp);
    }


    private void TryGrantRewardsToKiller(Transform killer)
    {
        if (!killer) return;

        // Companion → restore mana on kill
        var comp = killer.GetComponentInParent<Companion>();
        if (comp)
        {
            var cMana = comp.GetComponent<Companion_Mana>();
            cMana?.RestoreManaOnKill();
        }
    }

    private float GetExpRewardSafe()
    {
        try
        {
            var mi = typeof(Entity_Health).GetMethod("GetEXPReward",
                        BindingFlags.Instance | BindingFlags.NonPublic);
            if (mi != null)
            {
                object v = mi.Invoke(this, null);
                if (v is float f) return f;
            }

            var fi = typeof(Entity_Health).GetField("expReward",
                        BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null)
            {
                object v = fi.GetValue(this);
                if (v is float f) return f;
            }
        }
        catch { }
        return 0f;
    }

    private void RewardPlayer(float exp)
    {
        // Your project has Player.GainEXP(float)
        var player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.GainEXP(exp);
            if (debugLogs) Debug.Log($"[Enemy_Health] Gave {exp} EXP to Player.");
            return;
        }

        // Fallback: try Player_Stats (or similar) by reflection if needed
        var pStats = FindAnyObjectByType<Player_Stats>();
        if (!TryGrantExpViaReflection(pStats, exp) && debugLogs)
            Debug.LogWarning("[Enemy_Health] Could not find a way to give EXP to Player.");
    }

    private void RewardCompanion(Companion comp, float exp)
    {
        // 1) Companion component might expose GainEXP
        if (TryGrantExpViaReflection(comp, exp))
        {
            if (debugLogs) Debug.Log($"[Enemy_Health] Gave {exp} EXP to companion '{comp.name}'.");
            return;
        }

        // 2) Else try the companion's stats
        Component stats =
            (Component)comp.GetComponent("Player_Stats") ??
            (Component)comp.GetComponent("Companion_Stats") ??
            comp.GetComponent<Entity_Stats>();

        if (TryGrantExpViaReflection(stats, exp))
        {
            if (debugLogs) Debug.Log($"[Enemy_Health] Gave {exp} EXP to companion '{comp.name}' (via stats).");
            return;
        }

        if (debugLogs) Debug.LogWarning($"[Enemy_Health] Could not grant EXP to companion '{comp.name}' (no GainEXP-like method).");
    }

    /// <summary>
    /// Tries common EXP APIs by reflection:
    /// - GainEXP(float), AddEXP(float), AddExperience(float), GainExperience(float)
    /// - or, as a last resort, increments a CurrentEXP property/field.
    /// </summary>
    private static bool TryGrantExpViaReflection(object target, float exp)
    {
        if (target == null) return false;

        // Try common method names first
        string[] methodNames = { "GainEXP", "AddEXP", "AddExperience", "GainExperience" };
        foreach (var m in methodNames)
        {
            var mi = target.GetType().GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                try { mi.Invoke(target, new object[] { exp }); return true; }
                catch { /* try next */ }
            }
        }

        // Fallback: bump a CurrentEXP field/property if present
        try
        {
            var pi = target.GetType().GetProperty("CurrentEXP", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null)
            {
                float cur = ConvertToFloat(pi.GetValue(target));
                pi.SetValue(target, cur + exp);
                return true;
            }

            var fi = target.GetType().GetField("CurrentEXP", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
            {
                float cur = ConvertToFloat(fi.GetValue(target));
                fi.SetValue(target, cur + exp);
                return true;
            }
        }
        catch { }

        return false;
    }

    private static float ConvertToFloat(object v)
    {
        if (v is float f) return f;
        if (v is int i) return i;
        if (v is double d) return (float)d;
        if (v is long l) return l;
        if (v is short s) return s;
        return 0f;
    }
}
