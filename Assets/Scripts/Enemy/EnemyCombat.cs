using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : Entity_Combat
{
    [SerializeField] public Transform targetCheck;
    [SerializeField] private float checkOffset = 1f;

    private Enemy enemy;
    private Vector2 lastDir = Vector2.zero;

    public void Update()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (enemy.currentDir != lastDir)
        {
            lastDir = enemy.currentDir;
            FlipTargetCheck();
        }
    }

    public void FlipTargetCheck()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        Vector2 dir = enemy.currentDir.normalized;

        // Fallback direction if idle
        if (dir == Vector2.zero)
            dir = Vector2.right;

        targetCheck.position = transform.position + new Vector3(checkOffset * dir.x, checkOffset * dir.y, 0);
    }

    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
        detected.AddRange(colliders);

        return detected.ToArray();
    }

    private void OnDrawGizmos()
    {
        if (targetCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
        }
    }
}

