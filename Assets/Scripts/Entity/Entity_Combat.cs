using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    private Entity _entity;
    private Entity_VFX vfx;
    public float damage = 10f; // damage amount

    [Header("Target detection")]
    // [SerializeField] private Transform[] targetCheck;
    [SerializeField] private float targetCheckRadius;
    [SerializeField] private LayerMask whatIsTarget;

    [SerializeField] private Transform _targetCheck_Left;
    [SerializeField] private Transform _targetCheck_Right;
    [SerializeField] private Transform _targetCheck_Up;
    [SerializeField] private Transform _targetCheck_Down;

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>(); // Get the Entity_VFX component attached to the same GameObject
    }



    public void PerformAttack()
    {
        foreach (var target in GetDetectedCollider())
        {
            if (!target.TryGetComponent(out IDamagable damagable)) // If the target does not have an IDamagable component, skip to the next target  
            {
                continue;
            }

            bool targetGotHit = damagable.TakeDamage(damage, transform); // Call the TakeDamage method on the target's IDamagable component, if it exists  

            if (targetGotHit)
                vfx?.CreateOnHitVFX(target.transform); // Create a hit visual effect at the target's position  
        }
    }


    protected Collider2D[] GetDetectedCollider() // method to get detected colliders  
    {
        // Initialize an empty list to store detected colliders  
        List<Collider2D> detected = new List<Collider2D>();

        // Iterate through each Transform in the targetCheck array  
        // foreach (Transform check in targetCheck)
        // {
        //     // Use OverlapCircleAll for each Transform's position  
        //     Collider2D[] colliders = Physics2D.OverlapCircleAll(check.position, targetCheckRadius, whatIsTarget);

        //     // Combine the detected colliders into the list  
        //     detected.AddRange(colliders);
        // }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(GetTargetTransform().position, targetCheckRadius, whatIsTarget);

        // Combine the detected colliders into the list  
        detected.AddRange(colliders);

        // Return the combined colliders as an array  
        return detected.ToArray();
    }

    private Transform GetTargetTransform()
    {
        if (_entity.currentDir.y >= 1)
        {
            return _targetCheck_Up;
        }
        if (_entity.currentDir.y <= -1)
        {
            return _targetCheck_Down;
        }
        if (_entity.currentDir.x <= -1)
        {
            return _targetCheck_Left;
        }
        return _targetCheck_Right;
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
        // Fix: Declare and initialize targetCheck to avoid CS0103 error  
        Transform[] targetCheck = { _targetCheck_Left, _targetCheck_Right, _targetCheck_Up, _targetCheck_Down };

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
