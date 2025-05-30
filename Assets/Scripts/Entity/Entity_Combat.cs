using System.Collections.Generic;
using UnityEngine;

public abstract class Entity_Combat : MonoBehaviour
{
    protected Entity _entity;
    private Entity_VFX vfx;
    private Entity_Stats stats; // Reference to the Entity_Stats component, if needed for combat calculations  

    [Header("Target detection")]
    [SerializeField] protected float targetCheckRadius;
    [SerializeField] protected LayerMask whatIsTarget;

    [Header("Status effect details")]
    [SerializeField] private float defaultDuration = 2f; // Default duration for the status effect  
    [SerializeField] private float chillSlowMultiplier = .2f;

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>(); // Replaced TryGetComponent with GetComponent to avoid allocation when no component is found  
        stats = GetComponent<Entity_Stats>(); // Replaced TryGetComponent with GetComponent to avoid allocation when no component is found  
    }

    public void PerformAttack()
    {
        foreach (var target in GetDetectedCollider())
        {
            IDamagable damagable = target.GetComponent<IDamagable>();// If the target does not have an IDamagable component, skip to the next target  
            
            if (damagable == null)
                continue;
            

            float elementalDamage = stats.GetElementalDamage(out ElementType element); // Get the elemental damage and whether it was a critical hit  
            float damage = stats.GetPhysicalDamage(out bool isCrit); // Get the physical damage and whether it was a critical hit  
            
            bool targetGotHit = damagable.TakeDamage(damage, elementalDamage, element, transform); // Call the TakeDamage method on the target's IDamagable component, if it exists  

            if (element != ElementType.None) // If the element is not None, apply the status effect  
                ApplyStatusEffect(target.transform, element); // Apply the status effect to the target  

            if (targetGotHit)
            {
                vfx.UpdateOnHitColor(element);
                vfx?.CreateOnHitVFX(target.transform, isCrit); // Create a hit visual effect at the target's position  
            }
        }
    }

    public void ApplyStatusEffect(Transform target, ElementType element)
    {
        Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();// Use TryGetComponent to avoid allocation if the component is not found  
        {
            if (element == ElementType.Ice && statusHandler.CanBeApplied(ElementType.Ice))
                statusHandler.ApplyChilledEffect(defaultDuration, chillSlowMultiplier);
            
            
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
