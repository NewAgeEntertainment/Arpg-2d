using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    private Entity_VFX vfx;
    private Entity_Stats stats; // Reference to the Entity_Stats component, if needed for combat calculations




    [Header("Target detection")]
    [SerializeField] private Transform[] targetCheck;
    [SerializeField] private float targetCheckRadius;
    [SerializeField] private LayerMask whatIsTarget;

    private void Awake()
    {
        vfx = GetComponent<Entity_VFX>(); // Get the Entity_VFX component attached to the same GameObject
        stats = GetComponent<Entity_Stats>(); // Get the Entity_Stats component attached to the same GameObject, if needed for combat calculations
    }

    

public void PerformAttack()
    {
        foreach (var target in GetDetectedCollider())
        {
            IDamagable damagable = target.GetComponent<IDamagable>(); // Corrected variable name from 'damgable' to 'damagable'  

            if (damagable == null) // If the target does not have an IDamagable component, skip to the next target  
            {
                continue;
            }

            float elementalDamage = stats.GetElementalDamage(out ElementType element); // Get the elemental damage and whether it was a critical hit
            float damage = stats.GetPhysicalDamage(out bool isCrit); // Get the physical damage and whether it was a critical hit
            bool targetGotHit = damagable.TakeDamage(damage, elementalDamage, element, transform); // Call the TakeDamage method on the target's IDamagable component, if it exists  
            
            if(targetGotHit)
                vfx?.CreateOnHitVFX(target.transform,isCrit); // Create a hit visual effect at the target's position  
        }
    }


    protected Collider2D[] GetDetectedCollider() // method to get detected colliders  
    {
        // Initialize an empty list to store detected colliders  
        List<Collider2D> detected = new List<Collider2D>();

        // Iterate through each Transform in the targetCheck array  
        foreach (Transform check in targetCheck)
        {
            // Use OverlapCircleAll for each Transform's position  
            Collider2D[] colliders = Physics2D.OverlapCircleAll(check.position, targetCheckRadius, whatIsTarget);

            // Combine the detected colliders into the list  
            detected.AddRange(colliders);
        }

        // Return the combined colliders as an array  
        return detected.ToArray();
    }

    private Collider2D[] CombineColliders(Collider2D[] array1, Collider2D[] array2) // Combine two arrays of colliders
    {
        Collider2D[] combined = new Collider2D[array1.Length + array2.Length]; // create a new array with the size of both arrays combined
        array1.CopyTo(combined, 0);
        array2.CopyTo(combined, array1.Length); // copy the first array to the new array
        return combined; // return the combined array
    }

    private void OnDrawGizmos()
    {
        if (targetCheck != null)
        {
            foreach (Transform check in targetCheck)
            {
                Gizmos.DrawWireSphere(check.position, targetCheckRadius);
            }
        }
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
