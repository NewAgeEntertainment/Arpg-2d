#if PIXELCRUSHERS
using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

public class DS_PartyBridge : MonoBehaviour
{
    private void OnEnable()
    {
        Lua.RegisterFunction("PartyRecruit", this, SymbolExtensions.GetMethodInfo(() => PartyRecruit(string.Empty)));
        Lua.RegisterFunction("PartyDismiss", this, SymbolExtensions.GetMethodInfo(() => PartyDismiss(string.Empty)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("PartyRecruit");
        Lua.UnregisterFunction("PartyDismiss");
    }

    // Lua: PartyRecruit("Rabbie")
    public void PartyRecruit(string id)
    {
        var go = CompanionPartyManager.Instance?.Recruit(id, silent: false);
        // If you gated AI with a flag, you could also do:
        // go?.GetComponent<Companion>()?.SetInParty(true);
    }

    // Lua: PartyDismiss("Rabbie")
    public void PartyDismiss(string id)
    {
        // If you gated AI, flip it before hiding:
        // var inst = CompanionPartyManager.Instance?.FindActiveInstance(id);
        // inst?.GetComponent<Companion>()?.SetInParty(false);

        CompanionPartyManager.Instance?.Dismiss(id, destroy: false);
    }
}
#endif
