using System;
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
    private Entity_SFX sfx;
    protected Entity_VFX vfx;
    protected Entity_Stats stats; // Reference to the Entity_Stats component, if needed for combat calculations  

    public DamageScaleData basicAttackScale; // Scale data for basic attack damage, chill, burn, poison, and shock effects

    [Header("Target detection")]
    [SerializeField] protected float targetCheckRadius;
    [SerializeField] protected LayerMask whatIsTarget;

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>();
        sfx = GetComponent<Entity_SFX>();
        stats = GetComponent<Entity_Stats>();
        mana = GetComponent<Entity_Mana>(); // ✅ Get mana if available
    }


    public virtual void PerformAttack()
    {
        bool targetGotHit = false;

        foreach (var target in GetDetectedCollider())
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null)
                continue;

            AttackData attackData = new AttackData(stats, basicAttackScale);
            Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

            float physicalDamage = attackData.physicalDamage;
            float elementalDamage = attackData.elementalDamage;
            ElementType element = attackData.element;

            targetGotHit = damageable.TakeDamage(physicalDamage, elementalDamage, element, transform);

            if (element != ElementType.None)
                statusHandler?.ApplyStatusEffect(element, attackData.effectData);

            if (targetGotHit)
            {
                OnDoingPhysicalDamage?.Invoke(physicalDamage);
                vfx.CreateOnHitVFX(target.transform, attackData.isCrit, element);
                sfx?.PlayAttackHit();

                // ✅ Restore mana on hit
                if (_entity is Player player)
                {
                    player.mana.RestoreManaOnHitWithScaling(player.Level);
                }

            }

            if (targetGotHit == false)
                sfx?.PlayAttackMiss();
        }
    }





    public abstract Collider2D[] GetDetectedCollider();

    private Collider2D[] CombineColliders(Collider2D[] array1, Collider2D[] array2) // Combine two arrays of colliders  
    {
        Collider2D[] combined = new Collider2D[array1.Length + array2.Length]; // create a new array with the size of both arrays combined  
        array1.CopyTo(combined, 0);
        array2.CopyTo(combined, array1.Length); // copy the first array to the new array  
        return combined; // return the combined array  
    }
}

// The issue lies in the line where `stats.GetElementalDamage` is called.  
// The method `GetElementalDamage` is expected to return an elemental damage value and an `ElementType` through the `out` parameter.  
// However, the `ElementType` enum only has a single value `None` defined, which means no other elemental types are available.  
// This could lead to incorrect behavior when applying elemental effects or calculating damage.  





//// orignal code to get detected colliders while looping through each target check transform.

////private void GetDetectedCollider() // method to get detected colliders
////{
////    foreach (Transform check in targetCheck) //  loop through each target check transform
////    {
////        Collider2D[] detected = Physics2D.OverlapCircleAll(check.position, targetCheckRadius, whatIsTarget); // check for colliders within the radius of the target check transform
////        if (targetColliders == null)
////        {
////            targetColliders = detected; // if targetColliders is null, assign detected colliders to it
////        }
////        else
////        {
////            targetColliders = CombineColliders(targetColliders, detected); // if targetColliders is not null, combine the existing colliders with the newly detected colliders
////        }
////    }
////}

////if (targetHealth != null) // check if the target has Entity_Health component
////{
////    targetHealth.TakeDamage(10f); // apply damage to the target
////    Debug.Log("Attacked " + target.name); // log the attack
////}
