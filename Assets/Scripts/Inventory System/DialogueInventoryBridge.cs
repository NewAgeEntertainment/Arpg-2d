using System.Linq;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Registers Lua functions so dialogue nodes can give/take items & gold.
/// Works with your Inventory_* classes that use ItemDataSO + Inventory_Item.
/// Looks up ItemDataSO by name from Resources (any subfolder).
/// </summary>
public class DialogueInventoryBridge : MonoBehaviour
{
    // Optional: assign explicitly; otherwise it will find by tag "Player" or by type.
    [SerializeField] private Inventory_Base playerInventory;
    [SerializeField] private Player player; // used for gold/stat checks if you keep gold on Player/Inventory

    private void Awake()
    {
        if (player == null)
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged) player = tagged.GetComponent<Player>();
            if (player == null) player = FindObjectOfType<Player>();
        }

        if (playerInventory == null)
        {
            if (player != null)
            {
                playerInventory = player.GetComponent<Inventory_Base>();
                if (playerInventory == null) playerInventory = player.GetComponentInChildren<Inventory_Base>();
            }
            if (playerInventory == null) playerInventory = FindObjectOfType<Inventory_Base>();
        }
    }

    private void OnEnable()
    {
        // Register Lua functions (Dialogue System)
        Lua.RegisterFunction("GiveItem", this, SymbolExtensions.GetMethodInfo(() => Lua_GiveItem("", 0.0)));
        Lua.RegisterFunction("RemoveItem", this, SymbolExtensions.GetMethodInfo(() => Lua_RemoveItem("", 0.0)));
        Lua.RegisterFunction("HasItem", this, SymbolExtensions.GetMethodInfo(() => Lua_HasItem("", 0.0)));
        Lua.RegisterFunction("ItemCount", this, SymbolExtensions.GetMethodInfo(() => Lua_ItemCount("")));
        Lua.RegisterFunction("GiveGold", this, SymbolExtensions.GetMethodInfo(() => Lua_GiveGold(0.0)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("GiveItem");
        Lua.UnregisterFunction("RemoveItem");
        Lua.UnregisterFunction("HasItem");
        Lua.UnregisterFunction("ItemCount");
        Lua.UnregisterFunction("GiveGold");
    }

    // -------- Lua-callable methods (Lua passes numbers as double!) --------

    // GiveItem("Potion", 2)  -> returns true/false
    public bool Lua_GiveItem(string itemName, double amountD)
    {
        int amount = Mathf.Max(1, (int)amountD);
        var inv = playerInventory;
        if (inv == null) { Debug.LogWarning("[DialogueBridge] No Inventory found."); return false; }

        var itemSO = FindItemSO(itemName);
        if (itemSO == null) { Debug.LogWarning($"[DialogueBridge] ItemDataSO '{itemName}' not found in Resources."); return false; }

        bool anyAdded = false;
        for (int i = 0; i < amount; i++)
        {
            var item = new Inventory_Item(itemSO);
            if (inv.CanAddItem(item))
            {
                anyAdded |= inv.AddItem(item);
            }
            else
            {
                Debug.LogWarning("[DialogueBridge] Inventory full while giving items.");
                break;
            }
        }
        return anyAdded;
    }

    // RemoveItem("Potion", 1) -> returns true if fully removed
    public bool Lua_RemoveItem(string itemName, double amountD)
    {
        int amount = Mathf.Max(1, (int)amountD);
        var inv = playerInventory;
        if (inv == null) return false;

        var itemSO = FindItemSO(itemName);
        if (itemSO == null) return false;

        int remaining = amount;
        // Remove from stacks first
        while (remaining > 0)
        {
            var slot = inv.FindItem(itemSO);
            if (slot == null) break;
            if (slot.stackSize > 1)
            {
                slot.RemoveStack();
            }
            else
            {
                inv.RemoveOneItem(slot);
            }
            remaining--;
        }
        inv.NotifyInventoryChanged();
        return remaining == 0;
    }

    // HasItem("Key", 1) -> bool
    public bool Lua_HasItem(string itemName, double amountD)
    {
        int amount = Mathf.Max(1, (int)amountD);
        var inv = playerInventory;
        if (inv == null) return false;

        var itemSO = FindItemSO(itemName);
        if (itemSO == null) return false;

        int count = 0;
        foreach (var it in inv.itemList)
            if (it.itemData == itemSO) count += Mathf.Max(1, it.stackSize);

        return count >= amount;
    }

    // ItemCount("Potion") -> number
    public double Lua_ItemCount(string itemName)
    {
        var inv = playerInventory;
        if (inv == null) return 0;

        var itemSO = FindItemSO(itemName);
        if (itemSO == null) return 0;

        int count = 0;
        foreach (var it in inv.itemList)
            if (it.itemData == itemSO) count += Mathf.Max(1, it.stackSize);
        return count;
    }

    // GiveGold(100) -> returns new gold (if you store gold on player or inventory)
    public double Lua_GiveGold(double amountD)
    {
        int amount = (int)amountD;
        if (player == null) return 0;

        // Adapt this to your gold holder. Many of your systems use a single int on player/inventory.
        // Example:
        // player.gold += amount;  // or playerInventory.gold += amount;
        // player.ui?.RefreshGold();
        // return player.gold;

        // Placeholder: no-op if you don't have a gold field here.
        Debug.Log($"[DialogueBridge] GiveGold called for {amount}. Implement the add-gold line here.");
        return amount;
    }

    // -------- Helpers --------

    private static ItemDataSO FindItemSO(string nameOrId)
    {
        // Searches ALL Resources for ItemDataSO (supports your multiple folders setup).
        // Uses itemName match, which you already rely on in Inventory_Item.
        var all = Resources.LoadAll<ItemDataSO>("");
        return all.FirstOrDefault(s => s != null && s.itemName == nameOrId);
    }
}
