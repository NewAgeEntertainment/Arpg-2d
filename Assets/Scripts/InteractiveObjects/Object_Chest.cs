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

        // 🔥 NEW — Trigger GiveRewardOnInteract
        var reward = GetComponent<GiveRewardOnInteract>();
        if (reward != null)
        {
            // Find the player (Interaction sends actor, damage won’t)
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (player != null)
                reward.Give(player);
            else
                Debug.LogWarning("Chest opened but Player not found.");
        }

        // Disable chest collider after use
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }


    // Dev test
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) OnUse(null);
    }
}
