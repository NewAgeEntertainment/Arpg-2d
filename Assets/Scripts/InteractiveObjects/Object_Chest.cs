using UnityEngine;

public class Object_Chest : MonoBehaviour, IDamageable
{
    private Rigidbody rb => GetComponentInChildren<Rigidbody>();
    private Animator anim => GetComponentInChildren<Animator>();

    private Entity_VFX fx => GetComponent<Entity_VFX>();

    [Header("Open Details")]
    [SerializeField] private Vector2 knockback; // Direction and force of the knockback

    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        fx.PlayOnDamageVfx(); // Play the damage effect
        anim.SetBool("chestOpen", true); // open the chest
        rb.linearVelocity = knockback; // apply a force to the chest

        return true; // Return true to indicate that the damage was taken
        // Drop item
    }


}
