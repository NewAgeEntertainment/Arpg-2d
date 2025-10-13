using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-companion saver. Attach to each Companion (prefab and/or scene instance).
/// Requires a CompanionIdentity (string id) on the same GameObject.
/// If a companion isn't present when loading, CompanionPartySaver caches the snapshot
/// and this script will auto-apply the first time the companion is spawned.
/// </summary>
[RequireComponent(typeof(CompanionIdentity))]
public class CompanionStatsSaver : Saver
{
    [Serializable]
    public class StatData
    {
        public float baseValue;
        public List<StatModifier> modifiers = new();
    }

    [Serializable]
    public class SaveData
    {
        public string id;                 // roster/identity id (key)
        public int normalLevel;
        public float normalEXP;

        public float currentHealth;
        public float currentMana;

        // Minimal essentials (you can add more if you like)
        public StatData baseMaxHealth;
        public StatData baseMaxMana;
    }

    // Static cache used when ApplyData for this companion arrives before the instance exists.
    // CompanionPartySaver fills this on load; CompanionStatsSaver consumes it on Awake/OnEnable.
    public static readonly Dictionary<string, SaveData> PendingSnapshots = new();

    private CompanionIdentity identity;
    private Companion_Stats cStats;
    private Entity_Health health;
    private Entity_Mana mana;

    public override void Awake()
    {
        base.Awake();
        identity = GetComponent<CompanionIdentity>();
        cStats = GetComponent<Companion_Stats>();
        health = GetComponent<Entity_Health>();
        mana = GetComponent<Entity_Mana>();
    }

    private void OnEnable()
    {
        // If a snapshot for this id was loaded before this object appeared, apply it now.
        TryApplyPending();
    }

    private void TryApplyPending()
    {
        if (identity == null || string.IsNullOrEmpty(identity.id)) return;

        if (PendingSnapshots.TryGetValue(identity.id, out var snap) && snap != null)
        {
            ApplySnapshot(snap);
            PendingSnapshots.Remove(identity.id);
        }
    }

    public override string RecordData()
    {
        var id = identity != null ? identity.id : null;
        if (string.IsNullOrEmpty(id) || cStats == null)
        {
            Debug.LogWarning($"[CompanionStatsSaver] Missing identity or stats on {name}.");
            return string.Empty;
        }

        var data = BuildSnapshot();
        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        var data = SaveSystem.Deserialize<SaveData>(s);
        if (data == null) return;

        // If this instance is active, apply immediately; otherwise cache for later.
        if (identity != null && !string.IsNullOrEmpty(identity.id) && identity.id == data.id)
        {
            ApplySnapshot(data);
        }
        else
        {
            // Identity mismatch or instance not ready; stash it.
            if (!string.IsNullOrEmpty(data.id))
                PendingSnapshots[data.id] = data;
        }
    }

    /// <summary>Build a snapshot of this companion's current state.</summary>
    public SaveData BuildSnapshot()
    {
        var id = identity != null ? identity.id : null;
        var hp = health ? health.GetCurrentHealth() : 0f;
        var mp = mana ? mana.GetCurrentMana() : 0f;

        var stats = cStats != null ? cStats : GetComponent<Companion_Stats>();
        var s = stats;

        var result = new SaveData
        {
            id = id ?? string.Empty,
            normalLevel = s != null ? s.CurrentLevel : 1,
            normalEXP = s != null ? s.CurrentEXP : 0f,

            currentHealth = hp,
            currentMana = mp,

            baseMaxHealth = new StatData
            {
                baseValue = s != null ? s.resources.maxHealth.BaseValue : 0f,
                modifiers = s != null ? new List<StatModifier>(s.resources.maxHealth.Modifiers) : new List<StatModifier>()
            },

            baseMaxMana = new StatData
            {
                baseValue = s != null ? s.resources.maxMana.BaseValue : 0f,
                modifiers = s != null ? new List<StatModifier>(s.resources.maxMana.Modifiers) : new List<StatModifier>()
            }
        };

        return result;
    }

    /// <summary>Apply the given snapshot to this companion instance.</summary>
    public void ApplySnapshot(SaveData data)
    {
        if (data == null) return;

        // Stats
        if (cStats != null)
        {
            cStats.SetLevelAndExp(Mathf.Max(1, data.normalLevel), Mathf.Max(0f, data.normalEXP));

            // Restore base values + modifiers for HP/MP caps
            cStats.resources.maxHealth.SetBaseValue(data.baseMaxHealth != null ? data.baseMaxHealth.baseValue : 0f);
            cStats.resources.maxHealth.ClearAllModifiers();
            if (data.baseMaxHealth != null && data.baseMaxHealth.modifiers != null)
                cStats.resources.maxHealth.AddModifiers(data.baseMaxHealth.modifiers);

            cStats.resources.maxMana.SetBaseValue(data.baseMaxMana != null ? data.baseMaxMana.baseValue : 0f);
            cStats.resources.maxMana.ClearAllModifiers();
            if (data.baseMaxMana != null && data.baseMaxMana.modifiers != null)
                cStats.resources.maxMana.AddModifiers(data.baseMaxMana.modifiers);
        }

        // Current HP/MP
        if (health != null) health.SetCurrentHealth(data.currentHealth);
        if (mana != null) mana.SetCurrentMana(data.currentMana);
    }
}
