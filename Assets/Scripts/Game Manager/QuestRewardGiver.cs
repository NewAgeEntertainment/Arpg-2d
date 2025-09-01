using UnityEngine;

/// <summary>
/// Gives rewards to the player (XP, gold, item) and can drop a pickup if adding fails.
/// Hook from Quest Machine via Message → GiveConfigured (or a UnityEvent).
/// </summary>
[AddComponentMenu("Game/Quests/Quest Reward Giver")]
public class QuestRewardGiver : MonoBehaviour
{
    [Header("Auto-found if left empty")]
    [SerializeField] private Player_Stats playerStats;
    [SerializeField] private Inventory_Player inventory;

    [Header("Configured rewards (used by GiveConfigured)")]
    [Min(0)] public int xp = 0;
    public int gold = 0;                     // can be negative (charge)
    public ItemDataSO item;
    [Min(1)] public int itemCount = 1;

    [Header("Duplicate Safety")]
    [Tooltip("If true, ignores subsequent calls after the first successful GiveConfigured().")]
    public bool oneShot = true;
    private bool _alreadyGiven;

    [Header("Fallback to world pickup if inventory add fails")]
    public bool spawnPickupIfCantAdd = true;
    public GameObject itemPickupPrefab;     // must have Object_ItemPickup
    public Transform dropAt;                // defaults to this.transform

    private void Reset()
    {
        TryResolveRefs();
        if (dropAt == null) dropAt = transform;
        if (itemCount <= 0) itemCount = 1;
    }

    private void Awake()
    {
        TryResolveRefs();
        if (dropAt == null) dropAt = transform;
        if (itemCount <= 0) itemCount = 1;
    }

    private void TryResolveRefs()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<Player_Stats>(FindObjectsInactive.Include);
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
    }

    // ===================== Public API =====================

    /// <summary>Give xp/gold/item based on the inspector fields above.</summary>
    public void GiveConfigured()
    {
        if (oneShot && _alreadyGiven)
        {
            Debug.Log("[QuestRewardGiver] Duplicate GiveConfigured ignored (oneShot).");
            return;
        }

        bool gaveSomething = false;

        if (xp > 0) { GiveXP(xp); gaveSomething = true; }
        if (gold != 0) { GiveGold(gold); gaveSomething = true; }
        if (item != null && itemCount > 0) { GiveItem(item, itemCount); gaveSomething = true; }

        if (!gaveSomething)
            Debug.LogWarning("[QuestRewardGiver] GiveConfigured called but no rewards set.");

        _alreadyGiven = true;
    }

    public void GiveXP(int amount)
    {
        if (amount <= 0) return;
        TryResolveRefs();

        if (playerStats == null)
        {
            Debug.LogWarning("[QuestRewardGiver] No Player_Stats found to give XP.");
            return;
        }

        playerStats.AddEXP(amount);
        Debug.Log($"[QuestRewardGiver] Gave XP: +{amount}");
    }

    /// <summary>
    /// Adds (or subtracts) gold using Inventory_Player.AddGold.
    /// If subtracting would go below zero, clamps to zero.
    /// </summary>
    public void GiveGold(int amount)
    {
        if (amount == 0) return;
        TryResolveRefs();

        if (inventory == null)
        {
            Debug.LogWarning("[QuestRewardGiver] No Inventory_Player found to give gold.");
            return;
        }

        int delta = amount;

        // Clamp so gold never goes below 0.
        if (amount < 0 && inventory.gold + amount < 0)
            delta = -inventory.gold;

        inventory.AddGold(delta); // AddGold handles raising OnGoldChanged

        Debug.Log($"[QuestRewardGiver] Gold {(delta >= 0 ? "+" : "")}{delta}. New total: {inventory.gold}");
    }

    public void GiveItem(ItemDataSO data) => GiveItem(data, 1);

    public void GiveItem(ItemDataSO data, int count)
    {
        if (data == null || count <= 0) return;
        TryResolveRefs();

        // If your ItemDataSO represents currency, convert to gold
        if (data.isGold && inventory != null)
        {
            GiveGold(data.goldValue * count);
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[QuestRewardGiver] No Inventory_Player found; spawning pickup instead.");
            TrySpawnPickup(data, count);
            return;
        }

        var invItem = new Inventory_Item(data) { stackSize = Mathf.Max(1, count) };
        bool added = inventory.AddItem(invItem);

        if (!added)
        {
            Debug.LogWarning("[QuestRewardGiver] Inventory refused item; attempting world drop.");
            TrySpawnPickup(data, count);
        }
        else
        {
            Debug.Log($"[QuestRewardGiver] Gave item: {data.itemName} x{count}");
        }
    }

    public void ResetOneShot() => _alreadyGiven = false;

    // ===================== Helpers =====================

    private void TrySpawnPickup(ItemDataSO data, int count)
    {
        if (!spawnPickupIfCantAdd || itemPickupPrefab == null)
        {
            Debug.LogWarning("[QuestRewardGiver] Pickup fallback disabled or prefab missing. Item not awarded.");
            return;
        }

        var t = dropAt != null ? dropAt : transform;

        // Spawn one + (count-1) duplicates if your pickup doesn’t support stacks.
        var go = Instantiate(itemPickupPrefab, t.position, Quaternion.identity);
        var pickup = go.GetComponent<Object_ItemPickup>();
        if (pickup != null) pickup.SetupItem(data);

        for (int i = 1; i < count; i++)
        {
            var extra = Instantiate(itemPickupPrefab, t.position, Quaternion.identity);
            extra.GetComponent<Object_ItemPickup>()?.SetupItem(data);
        }

        Debug.Log($"[QuestRewardGiver] Spawned pickup for {data.itemName} x{count} at {t.name}.");
    }
}
