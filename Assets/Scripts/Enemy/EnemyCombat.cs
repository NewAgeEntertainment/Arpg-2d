using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyCombat : Entity_Combat
{
    [SerializeField] private Transform _targetCheck_Left;
    [SerializeField] private Transform _targetCheck_Right;
    [SerializeField] private Transform _targetCheck_Up;
    [SerializeField] private Transform _targetCheck_Down;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        // Get the appropriate target check based on enemy's direction
        Transform targetCheck = GetTargetTransform();
        if (targetCheck != null)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
            detected.AddRange(colliders);
        }

        return detected.ToArray();
    }

    private Transform GetTargetTransform()
    {
        if (enemy.currentDir.y >= 1)
        {
            return _targetCheck_Up;
        }
        if (enemy.currentDir.y <= -1)
        {
            return _targetCheck_Down;
        }
        if (enemy.currentDir.x <= -1)
        {
            return _targetCheck_Left;
        }
        return _targetCheck_Right;
    }

    private void OnDrawGizmos()
    {
        Transform[] targetChecks = { _targetCheck_Left, _targetCheck_Right, _targetCheck_Up, _targetCheck_Down };

        foreach (Transform check in targetChecks)
        {
            if (check != null)
            {
                // Check if there are any players in this direction's target check area
                Collider2D[] colliders = Physics2D.OverlapCircleAll(check.position, targetCheckRadius, whatIsTarget);
                
                // Only show the gizmo if there are players detected in this direction
                if (colliders.Length > 0)
                {
                    // Check if any of the detected colliders are the player
                    foreach (Collider2D collider in colliders)
                    {
                        if (collider.CompareTag("Player"))
                        {
                            // Draw the target check sphere and line only if player is detected
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireSphere(check.position, targetCheckRadius);
                            Gizmos.DrawLine(transform.position, check.position);
                            break; // Exit the loop once we find the player
                        }
                    }
                }
            }
        }
    }
}
