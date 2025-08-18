// Assets/Scripts/Inventory/Inventory_Base.cs
using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;




public class Inventory_Base : MonoBehaviour
{
    public Player player;
    public event Action OnInventoryChange;

    public int maxInventorySize = 10;
    public List<Inventory_Item> itemList = new List<Inventory_Item>();

    // --- Runtime lockout store (per-player, per-effect) ---
    private static readonly Dictionary<string, float> _activeConsumables = new();

    private static string MakeLockKey(Player p, string effectKey)
        => $"{(p ? p.gameObject.GetInstanceID() : 0)}::{effectKey}";

    private static bool IsLockActive(Player p, string effectKey)
        => !string.IsNullOrEmpty(effectKey)
           && _activeConsumables.TryGetValue(MakeLockKey(p, effectKey), out var until)
           && Time.time < until;

    private static void StartLock(Player p, string effectKey, float seconds)
    {
        if (p == null || string.IsNullOrEmpty(effectKey) || seconds <= 0f) return;
        _activeConsumables[MakeLockKey(p, effectKey)] = Time.time + seconds;
    }

    protected virtual void Awake()
    {
        player = GetComponent<Player>();
    }

    public void NotifyInventoryChanged() => OnInventoryChange?.Invoke();

    public virtual bool AddItem(Inventory_Item itemToAdd)
    {
        if (itemToAdd == null || itemToAdd.itemData == null)
        {
            Debug.LogWarning("[Inventory_Base] Tried to add null item.");
            return false;
        }

        Inventory_Item existing = FindStackable(itemToAdd);

        if (existing != null)
        {
            existing.AddStack();
        }
        else
        {
            if (itemList.Count >= maxInventorySize)
            {
                Debug.LogWarning("[Inventory_Base] Inventory full, cannot add item.");
                return false;
            }
            itemList.Add(itemToAdd);
        }

        NotifyInventoryChanged();
        return true;
    }

    public virtual void RemoveOneItem(Inventory_Item itemToRemove)
    {
        Inventory_Item found = itemList.Find(item => item == itemToRemove);
        if (found != null)
        {
            if (found.stackSize > 1) found.RemoveStack();
            else itemList.Remove(found);

            NotifyInventoryChanged();
        }
    }

    public Inventory_Item FindStackable(Inventory_Item item)
        => itemList.Find(i => i.itemData == item.itemData && i.CanAddStack());

    public bool CanAddItem(Inventory_Item itemToAdd)
    {
        if (FindStackable(itemToAdd) != null) return true;
        if (itemToAdd.itemData.maxStackSize > 1) return itemList.Count <= maxInventorySize;
        return itemList.Count < maxInventorySize;
    }

    public Inventory_Item FindItem(ItemDataSO itemData)
        => itemList.Find(item => item.itemData == itemData);

    public Inventory_Item FindSameItem(Inventory_Item itemToFind)
        => itemList.Find(item => item.itemData == itemToFind.itemData);

    // === GUARDS + USE ===
    public bool TryUseItemChecked(Inventory_Item itemToUse, Player targetPlayer)
    {
        if (itemToUse == null || !itemList.Contains(itemToUse)) return false;

        var effect = itemToUse.itemEffect;
        if (effect == null) return false;

        // 🔎 Read gating directly from ItemEffect_DataSO
        bool affectsHealth = effect.AffectsHealth;
        bool affectsMana = effect.AffectsMana;
        float duration = effect.DurationSeconds;
        string effectKey = effect.EffectKey;

        // ❌ Block at full HP
        if (affectsHealth && targetPlayer?.health != null && targetPlayer.stats != null)
        {
            if (targetPlayer.health.GetCurrentHealth() >= targetPlayer.stats.GetMaxHealth())
            {
                Debug.Log("[Inventory] Cannot use: Health already full.");
                return false;
            }
        }

        // ❌ Block at full MP
        if (affectsMana && targetPlayer?.mana != null && targetPlayer.stats != null)
        {
            if (targetPlayer.mana.GetCurrentMana() >= targetPlayer.stats.GetMaxMana())
            {
                Debug.Log("[Inventory] Cannot use: Mana already full.");
                return false;
            }
        }

        // ❌ Block while same effect active
        if (duration > 0f && IsLockActive(targetPlayer, effectKey))
        {
            Debug.Log("[Inventory] Cannot use: Effect still active.");
            return false;
        }

        if (!effect.CanBeUsed(targetPlayer)) return false;

        effect.ExecuteEffect(targetPlayer);

        if (duration > 0f) StartLock(targetPlayer, effectKey, duration);

        if (itemToUse.stackSize > 1) itemToUse.RemoveStack();
        else RemoveOneItem(itemToUse);

        NotifyInventoryChanged();
        return true;
    }


    public void TryUseItem(Inventory_Item itemToUse, Player targetPlayer)
        => TryUseItemChecked(itemToUse, targetPlayer);

    public void TriggerUpdateUI() => OnInventoryChange?.Invoke();
}
