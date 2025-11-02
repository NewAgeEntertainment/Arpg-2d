using UnityEngine;

[AddComponentMenu("NPC/Follow/NPC Follow Invoker")]
public class NPCFollowInvoker : MonoBehaviour
{
    [Header("Default Target (optional)")]
    public NPCIdentity npcIdentity;   // optional default
    public string npcId;              // optional default

    [Header("Debug")]
    public bool log = false;

    private string ResolveId(string idOverride)
    {
        if (!string.IsNullOrEmpty(idOverride)) return idOverride;
        if (npcIdentity != null && !string.IsNullOrEmpty(npcIdentity.id)) return npcIdentity.id;
        return npcId;
    }

    // --- Methods you can hook to UnityEvents / Signals / Anim Events ---

    public void StartFollow() => StartFollowById(null);
    public void StopFollow() => StopFollowById(null);
    public void ToggleFollow() => ToggleFollowById(null);

    public void StartFollowById(string id)
    {
        string rid = ResolveId(id);
        if (string.IsNullOrEmpty(rid)) { if (log) Debug.LogWarning("[NPCFollowInvoker] Start: No id."); return; }
        NPCFollowManager.Instance.StartFollow(rid);
        if (log) Debug.Log($"[NPCFollowInvoker] StartFollow('{rid}')");
    }

    public void StopFollowById(string id)
    {
        string rid = ResolveId(id);
        if (string.IsNullOrEmpty(rid)) { if (log) Debug.LogWarning("[NPCFollowInvoker] Stop: No id."); return; }
        NPCFollowManager.Instance.StopFollow(rid);
        if (log) Debug.Log($"[NPCFollowInvoker] StopFollow('{rid}')");
    }

    public void ToggleFollowById(string id)
    {
        string rid = ResolveId(id);
        if (string.IsNullOrEmpty(rid)) { if (log) Debug.LogWarning("[NPCFollowInvoker] Toggle: No id."); return; }
        var mgr = NPCFollowManager.Instance;
        if (mgr.IsFollowing(rid))
        {
            mgr.StopFollow(rid);
            if (log) Debug.Log($"[NPCFollowInvoker] Toggle -> Stop('{rid}')");
        }
        else
        {
            mgr.StartFollow(rid);
            if (log) Debug.Log($"[NPCFollowInvoker] Toggle -> Start('{rid}')");
        }
    }
}
