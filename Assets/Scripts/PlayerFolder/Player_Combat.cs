using UnityEngine;

public class Player_Combat : Entity_Combat
{
    [Header("Counter Attack details")]
    [SerializeField] private float counterRecovery = .1f;

    public bool CounterAttackPerformed()
    {
        bool hasPerformedCounter = false;

        // This method checks if a counter attack was performed by looking for targets that implement the ICounterable interface
        // for each detected collider in the target area, it checks if the target has an ICounterable component
        foreach (var target in GetDetectedCollider()) // loop through all detected colliders in the target area
        {
            // check if the target has an ICounterable component
            // counterable equals the ICounterable component of the target collider
            ICounterable counterable = target.GetComponent<ICounterable>();

            if (counterable == null)
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

}
