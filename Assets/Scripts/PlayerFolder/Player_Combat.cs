using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : Entity_Combat
{
    [Header("Counter Attack details")]
    [SerializeField]
    private float counterRecovery = .1f;

    [SerializeField] private Transform _targetCheck_Left;
    [SerializeField] private Transform _targetCheck_Right;
    [SerializeField] private Transform _targetCheck_Up;
    [SerializeField] private Transform _targetCheck_Down;

    public bool CounterAttackPerformed()
    {
        bool hasPerformedCounter = false;

        // This method checks if a counter attack was performed by looking for targets that implement the ICounterable interface
        // for each detected collider in the target area, it checks if the target has an ICounterable component
        foreach (Collider2D target in GetDetectedCollider()) // loop through all detected colliders in the target area
        {
            // check if the target has an ICounterable component
            // counterable equals the ICounterable component of the target collider

            if (!target.TryGetComponent(out ICounterable counterable))
                continue; // if the target does not have an ICounterable component, skip to the next target

            // if the target has an ICounterable component, call its HandleCounter method
            if (counterable.CanBeCountered)
            {
                // Perform the counter attack by calling the HandleCounter method on the target's ICounterable component
                counterable.HandleCounter(); // call the HandleCounter method on the target's ICounterable component
                hasPerformedCounter = true; // if we found a counterable target, set the flag to true
            }
        }

        return hasPerformedCounter; // return true if we countered at least one target, false otherwise

    }

    public float GetCounterRecoveryDuration()
    {
        return counterRecovery; // return the duration of the counter attack
    }

    public override Collider2D[] GetDetectedCollider()
    {
        // Initialize an empty list to store detected colliders  
        List<Collider2D> detected = new List<Collider2D>();

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
