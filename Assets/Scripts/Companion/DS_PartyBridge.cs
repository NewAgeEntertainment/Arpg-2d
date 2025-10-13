using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class DS_PartyBridge : MonoBehaviour
{
    private static bool _registered;

    private void Awake() { TryRegister(); }
    private void OnEnable() { TryRegister(); }

    private void TryRegister()
    {
        if (_registered) return;

        // Register instance methods with the Lua VM:
        Lua.RegisterFunction("PartyRecruit", this,
            SymbolExtensions.GetMethodInfo(() => PartyRecruit(string.Empty)));

        Lua.RegisterFunction("PartyDismiss", this,
            SymbolExtensions.GetMethodInfo(() => PartyDismiss(string.Empty)));

        _registered = true;
        // Debug.Log("[DS_PartyBridge] Registered Lua functions PartyRecruit / PartyDismiss");
    }

    // ----------------- Lua-callable wrappers -----------------

    // Lua: PartyRecruit("Elaina")
    public void PartyRecruit(string rawId)
    {
        try
        {
            if (!NormalizeId(ref rawId)) return;

            var pm = CompanionPartyManager.Instance;
            if (pm == null)
            {
                Debug.LogError("[DS_PartyBridge] PartyRecruit failed: CompanionPartyManager.Instance is null.");
                return;
            }

            var go = pm.Recruit(rawId, silent: false);
            if (go == null)
                Debug.LogWarning($"[DS_PartyBridge] PartyRecruit: id '{rawId}' not found in roster.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DS_PartyBridge] PartyRecruit exception: {ex}");
        }
    }

    // Lua: PartyDismiss("Elaina")
    public void PartyDismiss(string rawId)
    {
        try
        {
            if (!NormalizeId(ref rawId)) return;

            var pm = CompanionPartyManager.Instance;
            if (pm == null)
            {
                Debug.LogError("[DS_PartyBridge] PartyDismiss failed: CompanionPartyManager.Instance is null.");
                return;
            }

            pm.Dismiss(rawId, destroy: false); // safe: your Dismiss already early-outs if id not active
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DS_PartyBridge] PartyDismiss exception: {ex}");
        }
    }

    // ----------------- helpers -----------------

    /// <summary>Trim quotes/whitespace; returns false if empty after normalization.</summary>
    private static bool NormalizeId(ref string id)
    {
        if (id == null) { Debug.LogWarning("[DS_PartyBridge] Empty id."); return false; }
        id = id.Trim();
        if (id.Length >= 2 && id[0] == '"' && id[^1] == '"') // defensive if quotes slip through again
            id = id.Substring(1, id.Length - 2).Trim();

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[DS_PartyBridge] Blank id after normalization.");
            return false;
        }
        return true;
    }
}
