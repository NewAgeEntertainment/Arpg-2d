using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

[DefaultExecutionOrder(-500)]     // register early
[DisallowMultipleComponent]
public class DS_PartyBridge : MonoBehaviour
{
    private static bool registered;

    private void Awake() { TryRegister(); }
    private void OnEnable() { TryRegister(); }

    private void OnDisable()
    {
        // Optional to keep functions available even if this gets disabled:
        // Leave them registered. If you prefer to unregister, uncomment below.
        // Unregister();
    }

    private void TryRegister()
    {
        if (registered) return;

        Lua.RegisterFunction("PartyRecruit", this,
            SymbolExtensions.GetMethodInfo(() => PartyRecruit(string.Empty)));
        Lua.RegisterFunction("PartyDismiss", this,
            SymbolExtensions.GetMethodInfo(() => PartyDismiss(string.Empty)));

        registered = true;
        // Debug.Log("[DS_PartyBridge] Registered Lua: PartyRecruit / PartyDismiss");
    }

    private void Unregister()
    {
        if (!registered) return;
        Lua.UnregisterFunction("PartyRecruit");
        Lua.UnregisterFunction("PartyDismiss");
        registered = false;
    }

    // Lua: PartyRecruit("Elaina")
    public void PartyRecruit(string id)
    {
        CompanionPartyManager.Instance?.Recruit(id, silent: false);
    }

    // Lua: PartyDismiss("Elaina")
    public void PartyDismiss(string id)
    {
        CompanionPartyManager.Instance?.Dismiss(id, destroy: false);
    }
}
