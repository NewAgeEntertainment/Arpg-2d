#if PIXELCRUSHERS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelCrushers;

/// Restores the party membership on load (non-destructive).
/// Attach once (e.g., to your Save System or any persistent manager object).
[DisallowMultipleComponent]
public class CompanionPartySaver : Saver
{
    [System.Serializable]
    private class Data
    {
        public string[] activeIds;
        public Data() { }
        public Data(string[] ids) { activeIds = ids; }
    }

    [Header("Restore Timing")]
    [Tooltip("Wait up to this many seconds for PartyManager & Player to exist before restoring.")]
    public float waitTimeoutSeconds = 6f;

    [Tooltip("Also wait for PlayerLocator.Current (or a Player) before applying.")]
    public bool waitForPlayer = true;

    [Tooltip("Write helpful messages to the Console.")]
    public bool verbose = true;

    // -------------------- Save --------------------
    public override string RecordData()
    {
        var mgr = CompanionPartyManager.Instance;
        if (mgr == null)
        {
            if (verbose) Debug.Log("[CompanionPartySaver] No CompanionPartyManager found at save time.");
            return string.Empty;
        }
        var ids = mgr.ActiveIds().ToArray();
        if (verbose) Debug.Log($"[CompanionPartySaver] Saving party: [{string.Join(", ", ids)}]");
        return SaveSystem.Serialize(new Data(ids));
    }

    // -------------------- Load --------------------
    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;

        var data = SaveSystem.Deserialize<Data>(s);
        if (data == null) return;

        var want = new HashSet<string>(data.activeIds ?? System.Array.Empty<string>());
        if (verbose) Debug.Log($"[CompanionPartySaver] Will restore party: [{string.Join(", ", want)}]");

        // Defer the actual work until the scene is ready and the PartyManager exists.
        StartCoroutine(ApplyWhenReady(want));
    }

    private IEnumerator ApplyWhenReady(HashSet<string> want)
    {
        float end = Time.unscaledTime + Mathf.Max(0.1f, waitTimeoutSeconds);

        // Wait for PartyManager to exist
        while (CompanionPartyManager.Instance == null && Time.unscaledTime < end)
            yield return null;

        var mgr = CompanionPartyManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[CompanionPartySaver] Timed out waiting for CompanionPartyManager.");
            yield break;
        }

        // Optionally also wait for Player to be resolvable (nice for follow behavior immediately after load)
        if (waitForPlayer)
        {
            while (mgr.player == null && Time.unscaledTime < end)
                yield return null;
        }

        // ---- Merge restore (non-destructive) ----

        // Add missing (recruit uses your manager’s rules: promote scene instance or use prefab)
        foreach (var id in want)
        {
            if (mgr.FindActiveInstance(id) == null)
            {
                if (verbose) Debug.Log($"[CompanionPartySaver] Recruit '{id}'");
                mgr.Recruit(id, silent: true);
            }
            else if (verbose) Debug.Log($"[CompanionPartySaver] Already active '{id}'");
        }

        // Hide extras (don’t destroy)
        var current = mgr.ActiveIds().ToList();
        foreach (var id in current)
        {
            if (!want.Contains(id))
            {
                if (verbose) Debug.Log($"[CompanionPartySaver] Dismiss extra '{id}'");
                mgr.Dismiss(id, destroy: false);
            }
        }

        // Optional: if your manager snaps companions near the player on scene load,
        // you don't need to do anything else here. The manager’s own scene-load
        // hook will handle positioning. If you want to force a snap here,
        // you could expose a public method on the manager and call it now.

        if (verbose) Debug.Log("[CompanionPartySaver] Party restore complete.");
    }
}
#endif
