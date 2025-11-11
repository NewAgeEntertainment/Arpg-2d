using UnityEngine;

[RequireComponent(typeof(InteractionTooltipTrigger2D))]
public class GiveRewardOnInteract : MonoBehaviour
{
    [Header("Item Reward (optional)")]
    [SerializeField] private ItemDataSO itemToGive;
    [SerializeField, Min(1)] private int itemAmount = 1;
    [SerializeField] private bool requireAllItemsToFit = false;

    [Header("Gold Reward (optional)")]
    [SerializeField, Min(0)] private int goldAmount = 0;

    [Header("Behavior")]
    [Tooltip("Destroy this object after a successful reward (item and/or gold).")]
    [SerializeField] private bool destroyAfterGive = true;

    private InteractionTooltipTrigger2D trigger;

    private void Awake()
    {
        trigger = GetComponent<InteractionTooltipTrigger2D>();
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
        if (actor == null)
        {
            Debug.LogWarning($"[{name}] Interact had no actor; cannot grant rewards.");
            return;
        }

        // Find inventory & gold holder from actor (player)
        var inv = actor.GetComponentInParent<Inventory_Player>();
        if (inv == null)
        {
            // Fall back to base inventory if needed
            inv = actor.GetComponentInParent<Inventory_Player>();
        }

        if (inv == null)
        {
            Debug.LogWarning($"[{name}] Could not find Inventory_Player on '{actor.name}'.");
            return;
        }

        bool gaveAnything = false;

        // 1) Give item(s) if configured
        if (itemToGive != null && itemAmount > 0)
        {
            if (requireAllItemsToFit)
            {
                // Pre-check capacity
                int canFit = 0;
                for (int i = 0; i < itemAmount; i++)
                {
                    var temp = new Inventory_Item(itemToGive);
                    if (inv.CanAddItem(temp)) canFit++;
                    else break;
                }

                if (canFit < itemAmount)
                {
                    Debug.Log($"[{name}] Not enough space to give {itemAmount}x {itemToGive.itemName}.");
                }
                else
                {
                    for (int i = 0; i < itemAmount; i++)
                        inv.AddItem(new Inventory_Item(itemToGive));
                    gaveAnything = true;
                }
            }
            else
            {
                int given = 0;
                for (int i = 0; i < itemAmount; i++)
                {
                    var entry = new Inventory_Item(itemToGive);
                    if (inv.CanAddItem(entry) && inv.AddItem(entry))
                        given++;
                    else
                        break;
                }

                if (given > 0)
                {
                    Debug.Log($"[{name}] Gave {given}x {itemToGive.itemName} to {actor.name}.");
                    gaveAnything = true;
                }
                else
                {
                    Debug.Log($"[{name}] Inventory full: could not give {itemToGive.itemName}.");
                }
            }
        }

        // 2) Give gold if configured
        if (goldAmount > 0)
        {
            inv.AddGold(goldAmount); // uses your Inventory_Player.AddGold(int)
            Debug.Log($"[{name}] Gave {goldAmount} gold to {actor.name}.");
            gaveAnything = true;
        }

        if (gaveAnything && destroyAfterGive)
        {
            Destroy(gameObject);
        }
    }
}
