using UnityEngine;

public class Object_Chest : MonoBehaviour, IDamageable
{
    private Animator anim => GetComponentInChildren<Animator>();
    private Entity_DropManager dropManager => GetComponent<Entity_DropManager>();

    [SerializeField] private bool canDropItems = true;

    public bool TakeDamage(float damage, float ele, ElementType type, Transform dealer)
    {
        if (!canDropItems)
            return false;

        canDropItems = false;

        anim.SetBool("chestOpen", true);
        dropManager?.DropItems(); // 👈 Triggers multiple drops

        return true;
    }

    // Optional: Dev test key
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(1, 0, ElementType.None, null);
        }
    }
}
