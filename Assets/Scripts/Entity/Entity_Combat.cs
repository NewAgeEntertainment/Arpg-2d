using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    
    public float damage = 10f; // damage amount

    [Header("Target detection")]
    [SerializeField] private Transform[] targetCheck;
    [SerializeField] private float targetCheckRadius;
    [SerializeField] private LayerMask whatIsTarget;

    public void PerformAttack()
    {
        

        foreach (var target in GetDetectedCollider()) //
        {

            IDamagable damable = target.GetComponent<IDamagable>(); // get the IDamagable component from the detected target
            damable?.TakeDamage(damage, transform); // call the TakeDamage method on the target's IDamagable component, if it exists

            Entity_Health targetHealth = target.GetComponent<Entity_Health>(); // get the Entity_Health component from the detected target
            targetHealth?.TakeDamage(damage,transform); // call the TakeDamage method on the target's Entity_Health component, if it exists
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
