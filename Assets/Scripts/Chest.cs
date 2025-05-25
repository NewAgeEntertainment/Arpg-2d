using UnityEngine;

public class Chest : MonoBehaviour, IDamagable
{
    private Rigidbody rb => GetComponentInChildren<Rigidbody>();
    private Animator anim => GetComponentInChildren<Animator>();

    private Entity_VFX fx => GetComponent<Entity_VFX>();

    [Header("Open Details")]
    [SerializeField] private Vector2 knockback; // Direction and force of the knockback

    public void TakeDamage(float damage, Transform damageDealer)
    {
        fx.PlayOnDamageVfx(); // Play the damage effect
        anim.SetBool("chestOpen", true); // open the chest
        rb.linearVelocity = knockback; // apply a force to the chest

        // Drop item
    }


}
