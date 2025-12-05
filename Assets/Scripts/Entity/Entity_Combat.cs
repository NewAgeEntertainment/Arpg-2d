using System;
using System.Collections;                     // for IEnumerator / coroutines
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

public abstract class Entity_Combat : MonoBehaviour
{
    protected Entity_Mana mana; // ✅ New: for restoring mana on hit
    [SerializeField] private List<Transform> targetCheckPoints;

    public event Action<float> OnDoingPhysicalDamage;
    protected Entity _entity;
    protected Entity_SFX sfx;
    protected Entity_VFX vfx;
    protected Entity_Stats stats; // Reference to the Entity_Stats component, if needed for combat calculations  

    [Header("Basic Attack Scaling")]
    public DamageScaleData basicAttackScale; // Scale data for basic attack damage, chill, burn, poison, and shock effects

    [Header("Target detection")]
    [SerializeField] protected float targetCheckRadius;
    [SerializeField] protected LayerMask whatIsTarget;


    public float TargetCheckRadius => targetCheckRadius;
    public LayerMask TargetLayerMask => whatIsTarget;


    // ===================== HIT WINDOW (Ys-style) =====================

    /// <summary>True while the current attack's hitbox window is active.</summary>
    private bool hitboxActive = false;

    /// <summary>Targets already hit during the current hitbox window.</summary>
    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>();
        sfx = GetComponent<Entity_SFX>();
        stats = GetComponent<Entity_Stats>();
        mana = GetComponent<Entity_Mana>(); // ✅ Get mana if available

        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] No Entity_Stats found on {name}. AttackData will not work correctly.");
        }

        if (basicAttackScale == null)
        {
            Debug.LogWarning($"[Entity_Combat] basicAttackScale is NULL on {name}. " +
                             "Attacks will use a default DamageScaleData unless you assign one in the Inspector.");
        }
    }

    // Called every frame: if we’re in a hit window, keep checking for targets
    private void Update()
    {
        if (!hitboxActive) return;

        // 🔒 SAFETY: if this is the Player and we're NOT in an attack/thrust state anymore,
        // kill the hitbox so it can't auto-hit while walking around.
        if (_entity is Player p)
        {
            var cur = p.stateMachine.currentState;
            if (!(cur is Player_BasicAttackState) &&
                !(cur is Player_ThrustState))
            {
                hitboxActive = false;
                hitThisSwing.Clear();
                return;
            }
        }

        // During the hit window we repeatedly look for new colliders and apply damage
        Collider2D[] detected = GetDetectedCollider();
        if (detected == null || detected.Length == 0)
            return;

        TryApplyAttackToTargets(detected, respectHitList: true);
    }

    // ===================== PUBLIC ATTACK API =====================

    /// <summary>
    /// Classic one-shot attack: used by a single animation event (AttackTrigger).
    /// </summary>
    public virtual void PerformAttack()
    {
        Debug.Log("[Combat] PerformAttack called by " + name);

        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] PerformAttack called on {name} but stats is NULL.");
            return;
        }

        Collider2D[] detectedColliders = GetDetectedCollider();
        Debug.Log("[Combat] Overlap found " + (detectedColliders?.Length ?? 0) + " colliders.");

        if (detectedColliders == null || detectedColliders.Length == 0)
            return;

        // One-shot attack: ignore per-swing hit list
        TryApplyAttackToTargets(detectedColliders, respectHitList: false);
    }

    /// <summary>
    /// Called by animation event at the frame the weapon becomes "active".
    /// Example: in your animation clip, add an event AttackHitStart().
    /// </summary>
    public void BeginAttackHitbox()
    {
        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] BeginAttackHitbox called on {name} but stats is NULL.");
            return;
        }

        hitboxActive = true;
        hitThisSwing.Clear();
        // sfx?.PlayAttackSwing(); // optional
    }

    /// <summary>
    /// Called by animation event at the frame the weapon stops being "active".
    /// Example: in your animation clip, add an event AttackHitEnd().
    /// </summary>
    public void EndAttackHitbox()
    {
        hitboxActive = false;
        hitThisSwing.Clear();
    }

    // ===================== INTERNAL ATTACK LOGIC =====================

    /// <summary>
    /// Shared attack logic used by both one-shot attacks and hit windows.
    /// </summary>
    private void TryApplyAttackToTargets(Collider2D[] colliders, bool respectHitList)
    {
        if (colliders == null) return;

        foreach (var target in colliders)
        {
            if (target == null) continue;

            // If we’re in a window, don’t hit the same enemy twice per swing
            if (respectHitList && hitThisSwing.Contains(target))
                continue;

            bool hit = TryApplyDamageToSingleTarget(target);
            if (hit && respectHitList)
                hitThisSwing.Add(target);
        }
    }

    /// <summary>
    /// Applies damage+status to a single collider, returns true if it actually took damage.
    /// </summary>
    private bool TryApplyDamageToSingleTarget(Collider2D target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
            return false;

        // ✅ Ensure we never pass a null DamageScaleData into AttackData
        DamageScaleData scaleToUse = basicAttackScale;
        if (scaleToUse == null)
        {
            scaleToUse = new DamageScaleData();
            Debug.LogWarning($"[Entity_Combat] basicAttackScale was NULL on {name} during attack. " +
                             "Using a default DamageScaleData. Consider assigning one on this Entity_Combat.");
        }

        AttackData attackData = new AttackData(stats, scaleToUse);
        Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

        float physicalDamage = attackData.physicalDamage;
        float elementalDamage = attackData.elementalDamage;
        ElementType element = attackData.element;

        bool targetGotHit = damageable.TakeDamage(physicalDamage, elementalDamage, element, transform);

        if (element != ElementType.None)
            statusHandler?.ApplyStatusEffect(element, attackData.effectData);

        if (targetGotHit)
        {
            OnDoingPhysicalDamage?.Invoke(physicalDamage);
            vfx?.CreateOnHitVFX(target.transform, attackData.isCrit, element);
            sfx?.PlayAttackHit();

            // ✅ Restore mana on hit (player only)
            if (_entity is Player player && player.mana != null)
            {
                player.mana.RestoreManaOnHitWithScaling(player.Level);
            }

            // ✅ THRUST DRAG: if this hit happens during Thrust, drag the enemy with the player
            if (_entity is Player thrustPlayer &&
                thrustPlayer.stateMachine.currentState is Player_ThrustState)
            {
                StartCoroutine(DragTargetWithPlayerDuringThrust(target.transform, thrustPlayer));
            }
        }
        else
        {
            sfx?.PlayAttackMiss();
        }

        return targetGotHit;
    }

    // ===================== COLLIDER DETECTION API =====================

    public abstract Collider2D[] GetDetectedCollider();

    private Collider2D[] CombineColliders(Collider2D[] array1, Collider2D[] array2) // Combine two arrays of colliders  
    {
        Collider2D[] combined = new Collider2D[array1.Length + array2.Length]; // create a new array with the size of both arrays combined  
        array1.CopyTo(combined, 0);
        array2.CopyTo(combined, array1.Length); // copy the first array to the new array  
        return combined; // return the combined array  
    }

    // ===================== THRUST DRAG (NEW) =====================

    private IEnumerator DragTargetWithPlayerDuringThrust(Transform target, Player player)
    {
        if (target == null || player == null) yield break;

        const float followDistance = 0.4f;   // how far in front of the player the enemy should sit
        const float pullSpeed = 20f;    // how quickly it snaps to that point

        var targetHealth = target.GetComponent<Entity_Health>();

        while (target != null &&
               targetHealth != null &&
               !targetHealth.IsDead &&
               player != null &&
               player.stateMachine.currentState is Player_ThrustState)
        {
            // Use the player's current facing / thrust direction
            Vector2 dir = player.currentDir.sqrMagnitude > 0.0001f
                ? player.currentDir.normalized
                : Vector2.down;

            Vector2 desiredPos = (Vector2)player.transform.position + dir * followDistance;
            target.position = Vector2.Lerp(target.position, desiredPos, pullSpeed * Time.deltaTime);

            yield return null;
        }
    }
}
