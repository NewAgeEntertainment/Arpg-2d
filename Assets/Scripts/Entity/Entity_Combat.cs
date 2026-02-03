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

    // IMPORTANT:
    // Player_Combat: set this to EnemyHurtbox layer only
    // EnemyCombat:  set this to PlayerHurtbox layer only
    [SerializeField] protected LayerMask whatIsTarget;

    // ===================== HIT WINDOW (Ys-style) =====================
    private bool hitboxActive = false;

    // Track per swing by IDamageable so multiple colliders on the same target don't double-hit
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
        if (!hitboxActive) return;

        var detected = GetDetectedCollider();
        if (detected == null || detected.Length == 0) return;

        TryApplyAttackToTargets(detected, respectHitList: true);
    }

    // ===================== PUBLIC ATTACK API =====================

    public virtual void PerformAttack()
    {
        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] PerformAttack called on {name} but stats is NULL.");
            return;
        }

        var detected = GetDetectedCollider();
        if (detected == null || detected.Length == 0) return;

        // Single "burst" hit check (your animation event call)
        TryApplyAttackToTargets(detected, respectHitList: false);
    }

    public void BeginAttackHitbox()
    {
        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] BeginAttackHitbox called on {name} but stats is NULL.");
            return;
        }

        hitboxActive = true;
        hitThisSwing.Clear();
    }

    public void EndAttackHitbox()
    {
        hitboxActive = false;
        hitThisSwing.Clear();
    }

    // ===================== INTERNAL ATTACK LOGIC =====================

    private void TryApplyAttackToTargets(Collider2D[] colliders, bool respectHitList)
    {
        foreach (var col in colliders)
        {
            if (col == null) continue;

            // Hurtbox is on child; health is on parent/root
            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            if (respectHitList && hitThisSwing.Contains(damageable))
                continue;

            bool hit = TryApplyDamageToSingleTarget(col);

            if (hit && respectHitList)
                hitThisSwing.Add(damageable);
        }
    }

    private bool TryApplyDamageToSingleTarget(Collider2D hitCollider)
    {
        var damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable == null) return false;

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

            // ✅ hook (Player_Combat override runs here)
            OnSuccessfulHit(hitCollider, attackData);

            OnDoingPhysicalDamage?.Invoke(physicalDamage);
            vfx?.CreateOnHitVFX(hitCollider.transform, attackData.isCrit, element);
            sfx?.PlayAttackHit();
        }
        else
        {
            sfx?.PlayAttackMiss();
        }

        return targetGotHit;
    }

    protected virtual void OnSuccessfulHit(Collider2D hitCollider, AttackData attackData)
    {
        // Base entities do nothing extra on hit.
    }

    // ===================== COLLIDER DETECTION API =====================
    public abstract Collider2D[] GetDetectedCollider();

    // Expose radius / mask if you like:
    public float TargetCheckRadius => targetCheckRadius;
    public LayerMask TargetLayerMask => whatIsTarget;
}
