using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class DS_NPCFollowBridge : MonoBehaviour
{
    private static bool registered;

    private void Awake() => TryRegister();
    private void OnEnable() => TryRegister();

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
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[DS_NPCFollowBridge] NPCFollowStart called with empty id");
            return;
        }

        // Tell the global follow manager to start tracking this NPC
        NPCFollowManager.Instance.StartFollow(id);
    }

    // Lua: NPCFollowStop("Elaina")
    public void NPCFollowStop(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[DS_NPCFollowBridge] NPCFollowStop called with empty id");
            return;
        }

        NPCFollowManager.Instance.StopFollow(id);
    }
}
