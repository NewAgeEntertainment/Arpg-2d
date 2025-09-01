using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelCrushers; // Pixel Crushers Save System

/// <summary>
/// Saves & loads ConquestRosterManager's unlocked profiles and affection.
/// Attach this to a persistent object (e.g., Dialogue Manager). Ensure its "Key" is unique.
/// </summary>
[AddComponentMenu("Conquest/Conquest Roster Saver")]
public class ConquestRosterSaver : Saver
{
    [Header("Profile Lookup")]
    [Tooltip("Preferred: database of all CharacterProfileSO assets.")]
    [SerializeField] private CharacterProfilesDatabase database;

    [Tooltip("If no database, optionally load all CharacterProfileSO from this Resources path. Leave blank to scan all Resources.")]
    [SerializeField] private string profilesResourcesPath = "";

    [Header("Logging")]
    [SerializeField] private bool verbose = false;

    // ===== Data model we serialize =====
    [Serializable]
    public class RosterSaveData
    {
        public List<Item> items = new();
        [Serializable]
        public class Item
        {
            public string profileName;   // asset name fallback key
            public string displayName;   // human-readable fallback
            public int affection;        // stored affection
        }
    }

    // PixelCrushers.Saver -> called on Save
    public override string RecordData()
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null || mgr.Entries == null)
        {
            if (verbose) Debug.Log("[ConquestRosterSaver] No manager/entries to save.");
            return string.Empty;
        }

        var dataset = new RosterSaveData();

        foreach (var e in mgr.Entries)
        {
            if (e == null || e.profile == null) continue;
            dataset.items.Add(new RosterSaveData.Item
            {
                profileName = e.profile.name,                 // asset name
                displayName = e.profile.displayName,          // optional
                affection = e.affection
            });
        }

        var payload = SaveSystem.Serialize(dataset);
        if (verbose) Debug.Log($"[ConquestRosterSaver] Saved {dataset.items.Count} roster entries.");
        return payload;
    }

    // PixelCrushers.Saver -> called on Load
    public override void ApplyData(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            if (verbose) Debug.Log("[ConquestRosterSaver] No data to apply.");
            return;
        }

        var mgr = ConquestRosterManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[ConquestRosterSaver] ConquestRosterManager.Instance is missing; cannot restore roster.");
            return;
        }

        var dataset = SaveSystem.Deserialize<RosterSaveData>(data);
        if (dataset == null || dataset.items == null)
        {
            if (verbose) Debug.LogWarning("[ConquestRosterSaver] Malformed or empty dataset.");
            return;
        }

        // Build a lookup so we can resolve profiles even if they’re not in the scene.
        var lookup = BuildProfileLookup();
        int restored = 0;

        foreach (var item in dataset.items)
        {
            var profile = ResolveProfile(lookup, item.profileName, item.displayName);
            if (profile == null)
            {
                if (verbose) Debug.LogWarning($"[ConquestRosterSaver] Couldn’t resolve profile '{item.profileName}'/'{item.displayName}'.");
                continue;
            }

            if (!mgr.IsUnlocked(profile))
            {
                // Unlock with starting affection; stats can be found later if/when actor spawns
                mgr.Unlock(profile, s: null, startingAffection: item.affection);
            }
            else
            {
                mgr.SetAffection(profile, item.affection);
            }

            restored++;
        }

        if (verbose) Debug.Log($"[ConquestRosterSaver] Restored {restored}/{dataset.items.Count} entries.");
    }

    // ===== Helpers =====

    private Dictionary<string, CharacterProfileSO> BuildProfileLookup()
    {
        var dict = new Dictionary<string, CharacterProfileSO>(StringComparer.OrdinalIgnoreCase);

        if (database != null && database.allProfiles != null && database.allProfiles.Count > 0)
        {
            foreach (var p in database.allProfiles)
            {
                if (p != null && !dict.ContainsKey(p.name))
                    dict.Add(p.name, p);
            }
            if (verbose) Debug.Log($"[ConquestRosterSaver] Loaded {dict.Count} profiles from database.");
            return dict;
        }

        // Fallback: load from Resources (requires your profiles to live under a Resources folder)
        CharacterProfileSO[] resourcesProfiles;
        if (string.IsNullOrEmpty(profilesResourcesPath))
            resourcesProfiles = Resources.LoadAll<CharacterProfileSO>(string.Empty);
        else
            resourcesProfiles = Resources.LoadAll<CharacterProfileSO>(profilesResourcesPath);

        foreach (var p in resourcesProfiles)
        {
            if (p != null && !dict.ContainsKey(p.name))
                dict.Add(p.name, p);
        }
        if (verbose) Debug.Log($"[ConquestRosterSaver] Loaded {dict.Count} profiles from Resources ('{profilesResourcesPath}').");
        return dict;
    }

    private CharacterProfileSO ResolveProfile(Dictionary<string, CharacterProfileSO> lookup, string assetName, string displayName)
    {
        if (lookup == null) return null;

        // 1) Try asset name
        if (!string.IsNullOrEmpty(assetName) && lookup.TryGetValue(assetName, out var byAsset))
            return byAsset;

        // 2) Try display name
        if (!string.IsNullOrEmpty(displayName))
        {
            foreach (var p in lookup.Values)
            {
                if (!string.IsNullOrEmpty(p.displayName) &&
                    string.Equals(p.displayName, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
        }

        return null;
    }
}
