using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class DS_NPCFollowBridge : MonoBehaviour
{
    private static bool registered;

    private void Awake() { TryRegister(); }
    private void OnEnable() { TryRegister(); }

    private void TryRegister()
    {
        if (registered) return;

        Lua.RegisterFunction("NPCFollowStart", this,
            SymbolExtensions.GetMethodInfo(() => NPCFollowStart(string.Empty)));
        Lua.RegisterFunction("NPCFollowStop", this,
            SymbolExtensions.GetMethodInfo(() => NPCFollowStop(string.Empty)));

        registered = true;
    }

    // Lua: NPCFollowStart("Elaina")
    public void NPCFollowStart(string id)
    {
        var npc = FindNPCById(id);
        if (!npc)
        {
            Debug.LogWarning($"[DS_NPCFollowBridge] NPC '{id}' not found.");
            return;
        }

        Transform target = PlayerLocator.Current ? PlayerLocator.Current : null;
        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) target = go.transform;
        }

        npc.StartFollow(target);
    }

    // Lua: NPCFollowStop("Elaina")
    public void NPCFollowStop(string id)
    {
        var npc = FindNPCById(id);
        if (!npc)
        {
            Debug.LogWarning($"[DS_NPCFollowBridge] NPC '{id}' not found.");
            return;
        }
        npc.StopFollow();
    }

    private NPC FindNPCById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        var all = Object.FindObjectsByType<NPCIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var ident = all[i];
            if (ident != null && ident.id == id)
            {
                var npc = ident.GetComponent<NPC>();
                if (!npc) npc = ident.GetComponentInParent<NPC>(true);
                return npc;
            }
        }
        return null;
    }
}
