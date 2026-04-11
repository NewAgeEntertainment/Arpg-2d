using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to CompanionPartyManager. Persists:
/// - companion snapshots
/// - which companions were active in the party
///
/// On load:
/// - restores all snapshots into PendingSnapshots
/// - defers active party restoration until after scene load completes
/// </summary>
[RequireComponent(typeof(CompanionPartyManager))]
public class CompanionPartySaver : Saver
{
    [Serializable]
    public class SavePayload
    {
        public List<CompanionStatsSaver.SaveData> entries = new();
        public List<string> activePartyIds = new();
    }

    private CompanionPartyManager pm;

    // Active party IDs we still need to restore after load/scene handoff.
    private readonly HashSet<string> pendingActiveRestore = new();

    private Coroutine restoreCoroutine;
    private bool subscribedToSceneLoaded;

    private void Awake()
    {
        pm = GetComponent<CompanionPartyManager>();
        CompanionPartyManager.OnRecruited += HandleRecruited;
        SubscribeSceneLoaded();
    }

    private void OnDestroy()
    {
        CompanionPartyManager.OnRecruited -= HandleRecruited;
        UnsubscribeSceneLoaded();
    }

    private void SubscribeSceneLoaded()
    {
        if (subscribedToSceneLoaded) return;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        subscribedToSceneLoaded = true;
    }

    private void UnsubscribeSceneLoaded()
    {
        if (!subscribedToSceneLoaded) return;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        subscribedToSceneLoaded = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryScheduleRestore();
    }

    private void HandleRecruited(string id, GameObject go)
    {
        if (string.IsNullOrEmpty(id) || go == null) return;

        var saver = go.GetComponent<CompanionStatsSaver>();
        if (saver == null) return;

        if (CompanionStatsSaver.PendingSnapshots.TryGetValue(id, out var snap) && snap != null)
        {
            saver.ApplySnapshot(snap);
            CompanionStatsSaver.PendingSnapshots.Remove(id);
        }

        pendingActiveRestore.Remove(id);
    }

    public override string RecordData()
    {
        var payload = new SavePayload();

        if (pm != null)
        {
            foreach (var id in pm.ActiveIds())
            {
                if (string.IsNullOrEmpty(id)) continue;

                if (!payload.activePartyIds.Contains(id))
                    payload.activePartyIds.Add(id);

                var go = pm.FindActiveInstance(id);
                if (go == null) continue;

                var saver = go.GetComponent<CompanionStatsSaver>();
                if (saver == null) continue;

                var snap = saver.BuildSnapshot();
                if (snap != null && !string.IsNullOrEmpty(snap.id))
                {
                    bool already = payload.entries.Exists(e => e != null && e.id == snap.id);
                    if (!already)
                        payload.entries.Add(snap);
                }
            }
        }

        // Keep snapshots for dismissed/not-currently-spawned companions too.
        foreach (var kv in CompanionStatsSaver.PendingSnapshots)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;

            bool already = payload.entries.Exists(e => e != null && e.id == kv.Key);
            if (!already)
                payload.entries.Add(kv.Value);
        }

        return SaveSystem.Serialize(payload);
    }

    public override void ApplyData(string s)
    {
        var payload = SaveSystem.Deserialize<SavePayload>(s);
        if (payload == null) return;

        // Stop any previous restore attempt.
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
            restoreCoroutine = null;
        }

        pendingActiveRestore.Clear();
        CompanionStatsSaver.PendingSnapshots.Clear();

        // 1) Restore all snapshots to pending cache first.
        if (payload.entries != null)
        {
            foreach (var snap in payload.entries)
            {
                if (snap == null || string.IsNullOrEmpty(snap.id)) continue;
                CompanionStatsSaver.PendingSnapshots[snap.id] = snap;
            }
        }

        // 2) Remember which companions should be active.
        if (payload.activePartyIds != null)
        {
            foreach (var id in payload.activePartyIds)
            {
                if (!string.IsNullOrEmpty(id))
                    pendingActiveRestore.Add(id);
            }
        }

        // 3) Defer actual re-recruit until after scene load settles.
        TryScheduleRestore();
    }

    private void TryScheduleRestore()
    {
        if (!isActiveAndEnabled) return;
        if (pendingActiveRestore.Count == 0) return;

        if (restoreCoroutine != null)
            StopCoroutine(restoreCoroutine);

        restoreCoroutine = StartCoroutine(RestoreActivePartyDeferred());
    }

    private IEnumerator RestoreActivePartyDeferred()
    {
        // Let scene objects finish enabling/spawning.
        yield return null;
        yield return null;

        if (pm == null)
            pm = GetComponent<CompanionPartyManager>();

        if (pm == null)
        {
            Debug.LogError("[CompanionPartySaver] Missing CompanionPartyManager during deferred restore.");
            restoreCoroutine = null;
            yield break;
        }

        var toRestore = new List<string>(pendingActiveRestore);

        foreach (var id in toRestore)
        {
            if (string.IsNullOrEmpty(id)) continue;

            GameObject live = pm.FindActiveInstance(id);
            if (live == null)
                live = pm.Recruit(id, silent: true);

            if (live == null)
            {
                Debug.LogWarning($"[CompanionPartySaver] Deferred restore failed for '{id}'.");
                continue;
            }

            var saver = live.GetComponent<CompanionStatsSaver>();
            if (saver != null &&
                CompanionStatsSaver.PendingSnapshots.TryGetValue(id, out var snap) &&
                snap != null)
            {
                saver.ApplySnapshot(snap);
                CompanionStatsSaver.PendingSnapshots.Remove(id);
            }

            pendingActiveRestore.Remove(id);
        }

        restoreCoroutine = null;
    }
}