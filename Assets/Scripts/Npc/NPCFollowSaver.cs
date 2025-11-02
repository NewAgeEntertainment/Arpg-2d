using PixelCrushers;
using System;
using UnityEngine;

/// <summary>
/// Persists NPC follow-state (list of NPCIdentity.id strings that are currently set to follow)
/// into the PixelCrushers save data. Works alongside NPCFollowManager which also uses PlayerPrefs
/// as a fallback. This saver calls NPCFollowManager.OnBeforeSave/OnAfterLoad to coordinate.
/// </summary>
public class NPCFollowSaver : Saver
{
    [Serializable]
    public class SavePayload
    {
        public string[] followingIds = Array.Empty<string>();
    }

    // Must match the PlayerPrefs key used by NPCFollowManager (fallback). If you change NPCFollowManager's key,
    // update this string accordingly.
    private const string PlayerPrefsKey_FollowingIds = "NPCFollowManager_FollowingIds_v1";

    /// <summary>
    /// Called by PixelCrushers SaveSystem when creating a save. Return a JSON string (or empty)
    /// that will be included in the save file.
    /// </summary>
    public override string RecordData()
    {
        try
        {
            // Let the manager snapshot its state into PlayerPrefs (best-effort).
            try
            {
                if (NPCFollowManager.Instance != null)
                {
                    NPCFollowManager.Instance.OnBeforeSave();
                }
            }
            catch (Exception) { /* ignore if manager isn't present or throws */ }

            // Read whatever the manager persisted into PlayerPrefs as the canonical snapshot.
            string json = PlayerPrefs.GetString(PlayerPrefsKey_FollowingIds, string.Empty);

            // If there's no PlayerPrefs entry, return an empty payload JSON for clarity.
            if (string.IsNullOrEmpty(json))
            {
                var empty = new SavePayload { followingIds = Array.Empty<string>() };
                return SaveSystem.Serialize(empty);
            }

            // The PlayerPrefs value is already JSON serialized by the manager, but to be robust
            // we wrap/normalize it into our payload so the saved-data structure is consistent.
            try
            {
                // Try to parse the PlayerPrefs JSON in the same shape the manager uses:
                // { "ids": ["id1","id2", ...] }
                var wrapper = JsonUtility.FromJson<FollowingIdsWrapper>(json);
                if (wrapper != null && wrapper.ids != null)
                {
                    var payload = new SavePayload { followingIds = wrapper.ids };
                    return SaveSystem.Serialize(payload);
                }
            }
            catch (Exception)
            {
                // If parsing fails, fall back to storing the raw string into a single-element array.
            }

            // Fallback: store raw PlayerPrefs string
            var fallback = new SavePayload { followingIds = new string[] { json } };
            return SaveSystem.Serialize(fallback);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NPCFollowSaver] RecordData failed: " + e.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Called by PixelCrushers SaveSystem when loading saved data. Apply the saved state.
    /// </summary>
    public override void ApplyData(string s)
    {
        try
        {
            if (string.IsNullOrEmpty(s)) return;

            var payload = SaveSystem.Deserialize<SavePayload>(s);
            if (payload == null)
            {
                Debug.LogWarning("[NPCFollowSaver] ApplyData: failed to deserialize payload.");
                return;
            }

            // Normalize into the same PlayerPrefs JSON shape NPCFollowManager expects:
            var wrapper = new FollowingIdsWrapper { ids = payload.followingIds ?? Array.Empty<string>() };
            var json = JsonUtility.ToJson(wrapper);

            // Restore into PlayerPrefs (so the manager's RestoreFromPrefs/OnAfterLoad can read it)
            PlayerPrefs.SetString(PlayerPrefsKey_FollowingIds, json);
            PlayerPrefs.Save();

            // --- NEW: Also populate the in-memory pending set so NPCs that spawn immediately after load can see it.
            NPCFollowManager.PendingFollowingIds.Clear();
            foreach (var id in wrapper.ids)
            {
                if (!string.IsNullOrEmpty(id)) NPCFollowManager.PendingFollowingIds.Add(id);
            }

            // Notify manager to restore follow-state and rebind NPCs.
            try
            {
                if (NPCFollowManager.Instance != null)
                {
                    NPCFollowManager.Instance.OnAfterLoad();
                }
            }
            catch (Exception)
            {
                // If manager isn't present yet, it will absorb PendingFollowingIds on Awake.
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NPCFollowSaver] ApplyData failed: " + e.Message);
        }
    }


    // Small helper that matches the wrapper used by NPCFollowManager (ids field).
    [Serializable]
    private class FollowingIdsWrapper
    {
        public string[] ids = Array.Empty<string>();
    }
}
