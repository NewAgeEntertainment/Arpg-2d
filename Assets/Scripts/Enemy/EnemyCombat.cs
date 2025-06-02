using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyCombat : Entity_Combat
{

    public Transform targetCheck;
    public void FlipTargetCheck()
    {
        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        // Determine the direction based on currentDir and adjust targetCheck position accordingly  
        if (enemy.currentDir.x > 0) // Facing right  
        {
            targetCheck.position = new Vector3(transform.position.x + 1, transform.position.y, transform.position.z);
        }
        else if (enemy.currentDir.x < 0) // Facing left  
        {
            targetCheck.position = new Vector3(transform.position.x - 1, transform.position.y, transform.position.z);
        }
        else if (enemy.currentDir.y > 0) // Facing up  
        {
            targetCheck.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
        }
        else if (enemy.currentDir.y < 0) // Facing down  
        {
            targetCheck.position = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);
        }
    }

    public void Update()
    {
        // Ensure enemy reference is assigned  
        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        // Call FlipTargetCheck to update targetCheck position based on direction  
        FlipTargetCheck();
    }
    private Enemy enemy;

    

    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);

        // Combine the detected colliders into the list  
        detected.AddRange(colliders);

        // Return the combined colliders as an array  
        return detected.ToArray();
    }

    private void OnDrawGizmos()
    {
        // Draw a wireframe sphere to visualize the target detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }
}
