using System.Collections.Generic;
using UnityEngine;
using PixelCrushers;
using PixelCrushers.QuestMachine;

[System.Serializable]
public class ItemReward_QM
{
    [Header("Simple Item (optional)")]
    [Tooltip("Optional single item reward (legacy-style).")]
    public ItemDataSO item;

    [Tooltip("How many to give. Can be constant or use counters/variables.")]
    public QuestNumber count = new QuestNumber();   // ⬅ no m_floatValue
}

/// <summary>
/// Quest Machine action that gives the player gold and items by talking to Inventory_Player.
/// </summary>
public class GivePlayerInventoryQuestAction : QuestAction
{
    [Header("Gold")]
    [Tooltip("Amount of gold to give (can be negative). Can be constant or driven by counters/variables.")]
    public QuestNumber goldAmount = new QuestNumber();

    [Tooltip("Clamp so that gold never goes below zero when subtracting.")]
    public bool clampGoldToZero = true;

    [Header("Simple Item (optional)")]
    [Tooltip("Optional single item reward (legacy-style).")]
    public ItemDataSO item;

    [Tooltip("Count for the single item reward. Can be constant or use counters/variables.")]
    public QuestNumber itemCount = new QuestNumber();

    [Header("Multiple Item Rewards (optional)")]
    [Tooltip("Additional (or multiple) item rewards to give.")]
    public List<ItemReward_QM> itemRewards = new List<ItemReward_QM>();

    // ===================== Editor Name =====================

    public override string GetEditorName()
    {
        // Build a friendly label for the node inspector.
        var goldVal = goldAmount.GetValue(quest);
        var singleCt = itemCount.GetValue(quest);

        int multiItemCount = 0;
        if (itemRewards != null)
        {
            foreach (var r in itemRewards)
            {
                if (r != null && r.item != null)
                    multiItemCount++;
            }
        }

        var parts = new List<string>();

        if (Mathf.Abs(goldVal) > 0.0001f)
            parts.Add($"{goldVal:+0;-0;0} gold");

        if (item != null && singleCt > 0f)
            parts.Add($"{singleCt}x {item.itemName}");

        if (multiItemCount > 0)
            parts.Add($"{multiItemCount} extra item reward(s)");

        if (parts.Count == 0)
            return "Give Player Inventory Rewards";

        return "Give " + string.Join(", ", parts) + " to player";
    }

    // ===================== Execute =====================

    public override void Execute()
    {
        base.Execute();

        // Find Inventory_Player in the scene.
        var inventory = Object.FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        if (inventory == null)
        {
            Debug.LogWarning("[GivePlayerInventoryQuestAction] No Inventory_Player found in scene.");
            return;
        }

        bool gaveSomething = false;

        // ---------- GOLD ----------
        int goldDelta = Mathf.RoundToInt(goldAmount.GetValue(quest));
        if (goldDelta != 0)
        {
            GiveGold(inventory, goldDelta);
            gaveSomething = true;
        }

        // ---------- SIMPLE (LEGACY) ITEM ----------
        if (item != null)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(itemCount.GetValue(quest)));
            if (count > 0)
            {
                GiveItem(inventory, item, count);
                gaveSomething = true;
            }
        }

        // ---------- MULTIPLE ITEMS ----------
        if (itemRewards != null)
        {
            foreach (var reward in itemRewards)
            {
                if (reward == null || reward.item == null) continue;

                int count = Mathf.Max(1, Mathf.RoundToInt(reward.count.GetValue(quest)));
                if (count <= 0) continue;

                GiveItem(inventory, reward.item, count);
                gaveSomething = true;
            }
        }

        if (!gaveSomething)
            Debug.LogWarning("[GivePlayerInventoryQuestAction] Execute() called but no rewards configured.");
    }

    // ===================== Internals =====================

    private void GiveGold(Inventory_Player inventory, int amount)
    {
        int delta = amount;

        if (clampGoldToZero && amount < 0 && inventory.gold + amount < 0)
            delta = -inventory.gold;

        if (delta == 0)
        {
            Debug.Log("[GivePlayerInventoryQuestAction] Gold change would be 0 (clamped). Nothing done.");
            return;
        }

        inventory.AddGold(delta); // Your Inventory_Player should raise OnGoldChanged etc.
        Debug.Log($"[GivePlayerInventoryQuestAction] Gold {(delta >= 0 ? "+" : "")}{delta}. New total: {inventory.gold}");
    }

    private void GiveItem(Inventory_Player inventory, ItemDataSO data, int count)
    {
        if (data == null || count <= 0) return;

        // If your ItemDataSO represents currency, convert to gold instead.
        if (data.isGold)
        {
            int goldValue = data.goldValue * count;
            GiveGold(inventory, goldValue);
            return;
        }

        var invItem = new Inventory_Item(data)
        {
            stackSize = Mathf.Max(1, count)
        };

        bool added = inventory.AddItem(invItem);

        if (!added)
        {
            Debug.LogWarning($"[GivePlayerInventoryQuestAction] Inventory refused item {data.itemName} x{count} (full?).");
        }
        else
        {
            Debug.Log($"[GivePlayerInventoryQuestAction] Gave item: {data.itemName} x{count}");
        }
    }
}
