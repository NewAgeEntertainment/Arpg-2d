using UnityEngine;

[RequireComponent(typeof(InteractionTooltipTrigger2D))]
public class GiveRewardOnInteract : MonoBehaviour
{
    [System.Serializable]
    private class ItemReward
    {
        public ItemDataSO item;
        [Min(1)] public int amount = 1;
    }

    [Header("Item Rewards (optional)")]
    [SerializeField] private ItemReward[] itemRewards;
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
            inv = actor.GetComponentInParent<Inventory_Player>();
        }

        if (inv == null)
        {
            Debug.LogWarning($"[{name}] Could not find Inventory_Player on '{actor.name}'.");
            return;
        }

        bool gaveAnything = false;

        // 1) Give item(s) if configured
        if (itemRewards != null && itemRewards.Length > 0)
        {
            if (requireAllItemsToFit)
            {
                // Pre-check capacity for ALL rewards before giving anything
                bool allFit = true;

                foreach (var reward in itemRewards)
                {
                    if (reward == null || reward.item == null || reward.amount <= 0)
                        continue;

                    int canFit = 0;
                    for (int i = 0; i < reward.amount; i++)
                    {
                        var temp = new Inventory_Item(reward.item);
                        if (inv.CanAddItem(temp)) canFit++;
                        else break;
                    }

                    if (canFit < reward.amount)
                    {
                        Debug.Log($"[{name}] Not enough space to give {reward.amount}x {reward.item.itemName}.");
                        allFit = false;
                        break;
                    }
                }

                if (allFit)
                {
                    foreach (var reward in itemRewards)
                    {
                        if (reward == null || reward.item == null || reward.amount <= 0)
                            continue;

                        for (int i = 0; i < reward.amount; i++)
                        {
                            inv.AddItem(new Inventory_Item(reward.item));
                        }

                        Debug.Log($"[{name}] Gave {reward.amount}x {reward.item.itemName} to {actor.name}.");
                        gaveAnything = true;
                    }
                }
            }
            else
            {
                // Best-effort: give as many as will fit for each reward
                foreach (var reward in itemRewards)
                {
                    if (reward == null || reward.item == null || reward.amount <= 0)
                        continue;

                    int given = 0;
                    for (int i = 0; i < reward.amount; i++)
                    {
                        var entry = new Inventory_Item(reward.item);
                        if (inv.CanAddItem(entry) && inv.AddItem(entry))
                            given++;
                        else
                            break;
                    }

                    if (given > 0)
                    {
                        Debug.Log($"[{name}] Gave {given}x {reward.item.itemName} to {actor.name}.");
                        gaveAnything = true;
                    }
                    else
                    {
                        Debug.Log($"[{name}] Inventory full: could not give {reward.item.itemName}.");
                    }
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

    public void Give(Transform actor)
    {
        HandleUse(actor);
    }
}
