using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : Entity_Combat
{
    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, targetCheckRadius, whatIsTarget);

        // Combine the detected colliders into the list  
        detected.AddRange(colliders);

        // Return the combined colliders as an array  
        return detected.ToArray();
    }
}
