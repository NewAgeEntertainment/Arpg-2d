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


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamagable damagable))
        {
            // If the target has an IDamagable component, call the TakeDamage method on it
            damagable.TakeDamage(damage, transform);
        }

    }

    


    private void OnDrawGizmos()
    {
        // Draw a green sphere at the position of the enemy to visualize its target detection area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, targetCheckRadius); // Adjust the radius as needed
    }
}
