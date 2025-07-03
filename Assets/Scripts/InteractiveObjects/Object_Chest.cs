using UnityEngine;

public class Object_Chest : MonoBehaviour, IDamageable
{
    private Rigidbody rb => GetComponentInChildren<Rigidbody>();
    private Animator anim => GetComponentInChildren<Animator>();

    private Entity_VFX fx => GetComponent<Entity_VFX>();

    private Entity_DropManager dropManager => GetComponent<Entity_DropManager>(); // Reference to the Entity_DropManager component for item drops

    [Header("Open Details")]
    [SerializeField] private Vector2 knockback; // Direction and force of the knockback
    [SerializeField] private bool canDropItems = true;

    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (canDropItems == false)
            return false;

        canDropItems = false; // Disable further item drops after the first damage taken
        dropManager?.DropItems(); // Drop an item when the chest is damaged
        fx.PlayOnDamageVfx(); // Play the damage effect
        anim.SetBool("chestOpen", true); // open the chest
        rb.velocity = knockback; // apply a force to the chest

        return true; // Return true to indicate that the damage was taken
        // Drop item
    }


}
