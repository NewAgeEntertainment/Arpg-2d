using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity_Combat : MonoBehaviour
{
    protected Entity_Mana mana; // still fine to keep if others want mana later
    [SerializeField] private List<Transform> targetCheckPoints;

    public event Action<float> OnDoingPhysicalDamage;
    protected Entity _entity;
    protected Entity_SFX sfx;
    protected Entity_VFX vfx;
    protected Entity_Stats stats;

    [Header("Basic Attack Scaling")]
    public DamageScaleData basicAttackScale;

    [Header("Target detection")]
    [SerializeField] protected float targetCheckRadius;
    [SerializeField] protected LayerMask whatIsTarget;

    // ===================== HIT WINDOW (Ys-style) =====================

    private bool hitboxActive = false;
    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>();
        sfx = GetComponent<Entity_SFX>();
        stats = GetComponent<Entity_Stats>();
        mana = GetComponent<Entity_Mana>();

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

    private void Update()
    {
        if (!hitboxActive) return;

        Collider2D[] detected = GetDetectedCollider();
        if (detected == null || detected.Length == 0)
            return;

        TryApplyAttackToTargets(detected, respectHitList: true);
    }

    // ===================== PUBLIC ATTACK API =====================

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

        TryApplyAttackToTargets(detectedColliders, respectHitList: false);
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
        if (colliders == null) return;

        foreach (var target in colliders)
        {
            if (target == null) continue;

            if (respectHitList && hitThisSwing.Contains(target))
                continue;

            bool hit = TryApplyDamageToSingleTarget(target);
            if (hit && respectHitList)
                hitThisSwing.Add(target);
        }
    }

    private bool TryApplyDamageToSingleTarget(Collider2D target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
            return false;

        DamageScaleData scaleToUse = basicAttackScale ?? new DamageScaleData();

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
            // ✅ FIRST: mana on hit (player only)
            if (_entity is Player player && player.mana != null)
            {
                player.mana.RestoreManaOnHitWithScaling(player.Level);
                Debug.Log($"[ManaOnHit] Restored mana for {player.name}. Now: {player.mana.GetCurrentMana()}");
            }

            // then the rest
            OnDoingPhysicalDamage?.Invoke(physicalDamage);
            vfx?.CreateOnHitVFX(target.transform, attackData.isCrit, element);
            sfx?.PlayAttackHit();   // this may still throw, but mana already updated
        }
        else
        {
            sfx?.PlayAttackMiss();
        }

        return targetGotHit;
    }


    // 🔹 Option 2: virtual hook, default does nothing.
    protected virtual void OnSuccessfulHit(Collider2D target, AttackData attackData)
    {
        // Base entities don’t do anything extra on hit.
        // Player / special enemies can override this.
    }

    // ===================== COLLIDER DETECTION API =====================

    public abstract Collider2D[] GetDetectedCollider();

    private Collider2D[] CombineColliders(Collider2D[] array1, Collider2D[] array2)
    {
        Collider2D[] combined = new Collider2D[array1.Length + array2.Length];
        array1.CopyTo(combined, 0);
        array2.CopyTo(combined, array1.Length);
        return combined;
    }

    // Expose radius / mask if you like:
    public float TargetCheckRadius => targetCheckRadius;
    public LayerMask TargetLayerMask => whatIsTarget;
}
