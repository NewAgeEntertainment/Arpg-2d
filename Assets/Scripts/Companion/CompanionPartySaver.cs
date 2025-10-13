using PixelCrushers;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to CompanionPartyManager. Persists a dictionary of companion snapshots
/// keyed by CompanionIdentity.id, so companions retain their state even when
/// dismissed or not currently spawned between scenes/saves.
/// </summary>
[RequireComponent(typeof(CompanionPartyManager))]
public class CompanionPartySaver : Saver
{
    [Serializable]
    public class SavePayload
    {
        public List<CompanionStatsSaver.SaveData> entries = new();
    }

    private CompanionPartyManager pm;

    private void Awake()
    {
        pm = GetComponent<CompanionPartyManager>();
        // When a companion is recruited after load, push any pending snapshot into it
        CompanionPartyManager.OnRecruited += HandleRecruited;
    }

    private void OnDestroy()
    {
        CompanionPartyManager.OnRecruited -= HandleRecruited;
    }

    private void HandleRecruited(string id, GameObject go)
    {
        if (string.IsNullOrEmpty(id) || go == null) return;

        var saver = go.GetComponent<CompanionStatsSaver>();
        if (saver == null) return;

        // If a snapshot was loaded before recruit, ApplyData already handled it in OnEnable.
        // But in case this fired later, try one more time:
        if (CompanionStatsSaver.PendingSnapshots.TryGetValue(id, out var snap) && snap != null)
        {
            saver.ApplySnapshot(snap);
            CompanionStatsSaver.PendingSnapshots.Remove(id);
        }
    }

    public override string RecordData()
    {
        var payload = new SavePayload();

        // 1) Capture all active companions with their live snapshots
        if (pm != null)
        {
            foreach (var id in pm.ActiveIds())
            {
                var go = pm.FindActiveInstance(id);
                if (!go) continue;
                var saver = go.GetComponent<CompanionStatsSaver>();
                if (saver == null) continue;

                var snap = saver.BuildSnapshot();
                if (!string.IsNullOrEmpty(snap.id))
                    payload.entries.Add(snap);
            }
        }

        // 2) Include any pending snapshots we’re holding for dismissed/not-present companions
        foreach (var kv in CompanionStatsSaver.PendingSnapshots)
        {
            // Avoid duplicates (prefer the most recent live snapshot if present)
            bool already = payload.entries.Exists(e => e.id == kv.Key);
            if (!already && kv.Value != null)
                payload.entries.Add(kv.Value);
        }

        return SaveSystem.Serialize(payload);
    }

    public override void ApplyData(string s)
    {
        var payload = SaveSystem.Deserialize<SavePayload>(s);
        if (payload == null || payload.entries == null) return;

        // Put all snapshots into the pending cache.
        // Currently spawned companions will pull from cache in their OnEnable,
        // and future recruits will be handled by HandleRecruited above.
        foreach (var snap in payload.entries)
        {
            if (snap == null || string.IsNullOrEmpty(snap.id)) continue;

            // If companion is already spawned now, apply immediately if possible
            var live = (pm != null) ? pm.FindActiveInstance(snap.id) : null;
            if (live != null)
            {
                var saver = live.GetComponent<CompanionStatsSaver>();
                if (saver != null)
                {
                    saver.ApplySnapshot(snap);
                    continue; // no need to cache
                }
            }

            // Otherwise cache for later spawn
            CompanionStatsSaver.PendingSnapshots[snap.id] = snap;
        }
    }
}
