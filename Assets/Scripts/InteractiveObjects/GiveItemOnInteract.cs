using UnityEngine;

/// <summary>
/// Attach to the SAME object as InteractionTooltipTrigger2D.
/// On interact, gives the specified item(s) to the player's Inventory_Base.
/// </summary>
[RequireComponent(typeof(InteractionTooltipTrigger2D))]
public class GiveItemOnInteract : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private ItemDataSO itemToGive;
    [SerializeField, Min(1)] private int amount = 1;

    [Header("Options")]
    [Tooltip("If true, destroy this object after successfully giving the items.")]
    [SerializeField] private bool destroyAfterGive = false;

    [Tooltip("If true, all items must be given; if inventory is full partway, nothing happens.")]
    [SerializeField] private bool requireAllToFit = false;

    private InteractionTooltipTrigger2D trigger;

    private void Reset()
    {
        trigger = GetComponent<InteractionTooltipTrigger2D>();
    }

    private void Awake()
    {
        if (trigger == null) trigger = GetComponent<InteractionTooltipTrigger2D>();
    }

    private void OnEnable()
    {
        if (trigger != null)
            trigger.onUse.AddListener(HandleUse);
    }

    private void OnDisable()
    {
        if (trigger != null)
            trigger.onUse.RemoveListener(HandleUse);
    }

    private void HandleUse(Transform actor)
    {
        if (itemToGive == null)
        {
            Debug.LogWarning($"[{name}] No ItemDataSO assigned.");
            return;
        }

        // Find the player's inventory from the actor the trigger passed in
        var inv = (actor != null)
            ? actor.GetComponentInParent<Inventory_Base>()
            : null;

        if (inv == null)
        {
            Debug.LogWarning($"[{name}] Could not find Inventory_Base on actor '{actor?.name ?? "NULL"}'.");
            return;
        }

        // If we require all to fit, pre-check capacity
        if (requireAllToFit)
        {
            int simul = 0;
            for (int i = 0; i < amount; i++)
            {
                var temp = new Inventory_Item(itemToGive);
                if (inv.CanAddItem(temp)) simul++;
                else break;
            }
            if (simul < amount)
            {
                Debug.Log($"[{name}] Not enough space to give {amount}x {itemToGive.itemName}.");
                return;
            }
        }

        // Add items (best-effort if not requiring all to fit)
        int given = 0;
        for (int i = 0; i < amount; i++)
        {
            var entry = new Inventory_Item(itemToGive);
            if (inv.CanAddItem(entry))
            {
                if (inv.AddItem(entry)) given++;
            }
            else if (requireAllToFit)
            {
                // Shouldn't happen due to pre-check, but bail just in case
                break;
            }
        }

        if (given > 0)
        {
            Debug.Log($"[{name}] Gave {given}x {itemToGive.itemName} to {actor.name}.");
            if (destroyAfterGive) Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[{name}] Inventory full: could not give {itemToGive.itemName}.");
        }
    }
}
