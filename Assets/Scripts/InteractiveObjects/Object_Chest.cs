using UnityEngine;

public class Object_Chest : MonoBehaviour, IDamageable
{
    private Animator anim => GetComponentInChildren<Animator>();
    private Entity_DropManager dropManager => GetComponent<Entity_DropManager>();

    [SerializeField] private bool canDropItems = true;

    // === Interact hook for InteractionTooltipTrigger2D ===
    public void OnUse(Transform actor)
    {
        OpenChest();
    }

    // Keep damage opening too (optional)
    public bool TakeDamage(float damage, float ele, ElementType type, Transform dealer)
    {
        if (!canDropItems) return false;
        OpenChest();
        return true;
    }

    private void OpenChest()
    {
        if (!canDropItems) return;
        canDropItems = false;

        if (anim) anim.SetBool("chestOpen", true);
        dropManager?.DropItems();

        // Optionally disable collider so it can’t be re-used:
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    // Dev test
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) OnUse(null);
    }
}
