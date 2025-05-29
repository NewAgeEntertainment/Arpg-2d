using UnityEngine;

public class Enemy_AttackHitbox : MonoBehaviour
{
    public float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamagable damagable))
        {
            // Reduce the HP of the collided object by calling TakeDamage
            damagable.TakeDamage(damage, transform);
            Debug.Log($"Enemy hit {collision.name}, reducing HP by {damage}.");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw a red sphere at the position of the hitbox to visualize its area
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // Adjust the radius as needed
    }
}
