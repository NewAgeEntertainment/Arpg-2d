// EconomyLua.cs
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class EconomyLua : MonoBehaviour
{
    void OnEnable()
    {
        Lua.RegisterFunction("HasGold", this, SymbolExtensions.GetMethodInfo(() => HasGold(0.0)));
        Lua.RegisterFunction("SpendGold", this, SymbolExtensions.GetMethodInfo(() => SpendGold(0.0)));
        Lua.RegisterFunction("TrySpend", this, SymbolExtensions.GetMethodInfo(() => TrySpend(0.0)));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("HasGold");
        Lua.UnregisterFunction("SpendGold");
        Lua.UnregisterFunction("TrySpend");
    }

    public bool HasGold(double amount)
    {
        var inv = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        return inv != null && inv.gold >= (int)amount;
    }

    public void SpendGold(double amount)
    {
        var inv = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        if (inv == null) return;
        inv.gold = Mathf.Max(0, inv.gold - (int)amount);
        UI.Instance?.UpdateGoldUI(inv.gold); // force UI refresh
    }

    public bool TrySpend(double amount)
    {
        var inv = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        if (inv == null) return false;
        int a = (int)amount;
        if (inv.gold < a) return false;
        inv.gold -= a;
        UI.Instance?.UpdateGoldUI(inv.gold);
        return true;
    }
}
