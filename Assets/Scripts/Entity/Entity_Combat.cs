using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity_Combat : MonoBehaviour
{
    protected Entity_Mana mana;
    public event Action<float> OnDoingPhysicalDamage;

    protected Entity _entity;
    protected Entity_SFX sfx;
    protected Entity_VFX vfx;
    protected Entity_Stats stats;

    [Header("Basic Attack Scaling")]
    public DamageScaleData basicAttackScale;

    [Header("Target detection")]
    [SerializeField] protected float targetCheckRadius = 0.5f;

    // Player_Combat: EnemyHurtbox only
    // Enemy_Combat:  PlayerHurtbox only
    [SerializeField] protected LayerMask whatIsTarget;

    // ===================== HIT WINDOW (Ys-style) =====================
    private bool hitboxActive = false;
    private bool hitLandedThisSwing = false;

    // Prevent multi-hit on same target during one active hit window
    private readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

    protected virtual void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>();
        sfx = GetComponent<Entity_SFX>();
        stats = GetComponent<Entity_Stats>();
        mana = GetComponent<Entity_Mana>();

        if (stats == null)
            Debug.LogError($"[Entity_Combat] No Entity_Stats found on {name}. AttackData will not work correctly.");

        if (basicAttackScale == null)
            Debug.LogWarning($"[Entity_Combat] basicAttackScale is NULL on {name}. " +
                             "Attacks will use a default DamageScaleData unless you assign one in the Inspector.");
    }

    protected virtual void Update()
    {
        if (!hitboxActive)
            return;

        var detected = GetDetectedCollider();
        if (detected == null || detected.Length == 0)
            return;

        bool hitSomething = TryApplyAttackToTargets(detected, respectHitList: true);

        if (hitSomething)
            hitLandedThisSwing = true;
    }

    // ===================== PUBLIC ATTACK API =====================

    // Use this for single animation-event burst attacks
    public virtual void PerformAttack()
    {
        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] PerformAttack called on {name} but stats is NULL.");
            return;
        }

        var detected = GetDetectedCollider();

        if (detected == null || detected.Length == 0)
        {
            sfx?.PlayAttackMiss();
            return;
        }

        bool hitSomething = TryApplyAttackToTargets(detected, respectHitList: false);

        if (!hitSomething)
            sfx?.PlayAttackMiss();
    }

    // Use this when your animation opens a hit window for several frames
    public void BeginAttackHitbox()
    {
        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] BeginAttackHitbox called on {name} but stats is NULL.");
            return;
        }

        hitboxActive = true;
        hitLandedThisSwing = false;
        hitThisSwing.Clear();
    }

    public void EndAttackHitbox()
    {
        // Only play whiff if the swing ended without any successful hit
        if (hitboxActive && !hitLandedThisSwing)
            sfx?.PlayAttackMiss();

        hitboxActive = false;
        hitLandedThisSwing = false;
        hitThisSwing.Clear();
    }

    // ===================== INTERNAL ATTACK LOGIC =====================

    private bool TryApplyAttackToTargets(Collider2D[] colliders, bool respectHitList)
    {
        bool hitAnything = false;

        foreach (var col in colliders)
        {
            if (col == null)
                continue;

            // Hurtbox is usually on child, damageable lives on parent/root
            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            if (respectHitList && hitThisSwing.Contains(damageable))
                continue;

            bool hit = TryApplyDamageToSingleTarget(col);

            if (hit)
            {
                hitAnything = true;

                if (respectHitList)
                    hitThisSwing.Add(damageable);
            }
        }

        return hitAnything;
    }

    private bool TryApplyDamageToSingleTarget(Collider2D hitCollider)
    {
        var damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return false;

        DamageScaleData scaleToUse = basicAttackScale ?? new DamageScaleData();
        AttackData attackData = new AttackData(stats, scaleToUse);

        var statusHandler = hitCollider.GetComponentInParent<Entity_StatusHandler>();

        float physicalDamage = attackData.physicalDamage;
        float elementalDamage = attackData.elementalDamage;
        ElementType element = attackData.element;

        bool targetGotHit = damageable.TakeDamage(physicalDamage, elementalDamage, element, transform);

        if (targetGotHit)
        {
            if (element != ElementType.None)
                statusHandler?.ApplyStatusEffect(element, attackData.effectData);

            // Player_Combat override hooks in here
            OnSuccessfulHit(hitCollider, attackData);

            OnDoingPhysicalDamage?.Invoke(physicalDamage);
            vfx?.CreateOnHitVFX(hitCollider.transform, attackData.isCrit, element);
            sfx?.PlayAttackHit();
        }

        return targetGotHit;
    }

    protected virtual void OnSuccessfulHit(Collider2D hitCollider, AttackData attackData)
    {
        // Base entities do nothing extra on hit
    }

    // ===================== COLLIDER DETECTION API =====================

    public abstract Collider2D[] GetDetectedCollider();

    public float TargetCheckRadius => targetCheckRadius;
    public LayerMask TargetLayerMask => whatIsTarget;
}