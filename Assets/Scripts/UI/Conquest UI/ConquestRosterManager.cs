using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConquestRosterManager : MonoBehaviour
{
    public static ConquestRosterManager Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        public CharacterProfileSO profile;
        public Entity_Stats stats;     // optional live ref
        public int affection; // stored affection
    }

    [Header("Affection Clamp")]
    [SerializeField] private int affectionMin = 0;
    [SerializeField] private int affectionMax = 100;

    [SerializeField] private List<Entry> unlocked = new List<Entry>();
    public IReadOnlyList<Entry> Entries => unlocked;

    public int AffectionMin => affectionMin;   // expose for UI slider sizing
    public int AffectionMax => affectionMax;

    public event Action OnRosterChanged;
    public event Action<CharacterProfileSO, int> OnAffectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------- Roster --------------------

    public bool IsUnlocked(CharacterProfileSO p) => p && unlocked.Any(e => e.profile == p);

    public void Unlock(CharacterProfileSO p, Entity_Stats s = null, int startingAffection = 0)
    {
        if (!p) return;

        var entry = unlocked.FirstOrDefault(e => e.profile == p);
        if (entry == null)
        {
            entry = new Entry
            {
                profile = p,
                stats = s,
                affection = Mathf.Clamp(startingAffection, affectionMin, affectionMax)
            };
            unlocked.Add(entry);
            OnRosterChanged?.Invoke();
            OnAffectionChanged?.Invoke(p, entry.affection);
        }
        else
        {
            if (s != null) entry.stats = s; // update live ref if provided
        }
    }

    public void Remove(CharacterProfileSO p)
    {
        if (!p) return;
        int removed = unlocked.RemoveAll(e => e.profile == p);
        if (removed > 0) OnRosterChanged?.Invoke();
    }

    // -------------------- Affection --------------------

    public int GetAffection(CharacterProfileSO p)
    {
        if (!p) return 0;
        var e = unlocked.FirstOrDefault(x => x.profile == p);
        return e != null ? e.affection : 0;
    }

    public void SetAffection(CharacterProfileSO p, int value)
    {
        if (!p) return;
        var e = unlocked.FirstOrDefault(x => x.profile == p);
        if (e == null) return;

        int clamped = Mathf.Clamp(value, affectionMin, affectionMax);
        if (e.affection == clamped) return;

        e.affection = clamped;
        OnAffectionChanged?.Invoke(p, e.affection);
    }

    public void AddAffection(CharacterProfileSO p, int delta)
    {
        if (!p || delta == 0) return;
        var e = unlocked.FirstOrDefault(x => x.profile == p);
        if (e == null) return;

        int newValue = Mathf.Clamp(e.affection + delta, affectionMin, affectionMax);
        if (newValue == e.affection) return;

        e.affection = newValue;
        OnAffectionChanged?.Invoke(p, e.affection);
    }

    public void SubtractAffection(CharacterProfileSO p, int delta)
    {
        AddAffection(p, -Mathf.Abs(delta));
    }

    // -------------------- Helpers used by UI/Lua --------------------

    /// <summary>Try to find the live Entity_Stats for a given profile in the current scene.</summary>
    public Entity_Stats FindStatsForProfile(CharacterProfileSO p)
    {
        if (!p) return null;

        // Find any CharacterProfileRef in the scene that points to this profile
        var refs = FindObjectsOfType<CharacterProfileRef>(true);
        foreach (var r in refs)
        {
            if (r && r.profile == p)
            {
                var s = r.GetComponent<Entity_Stats>()
                     ?? r.GetComponentInParent<Entity_Stats>(true)
                     ?? r.GetComponentInChildren<Entity_Stats>(true);
                if (s) return s;
            }
        }

        // Fallback: search all stats and check if any nearby object has this profile
        var allStats = FindObjectsOfType<Entity_Stats>(true);
        foreach (var s in allStats)
        {
            var r = s.GetComponent<CharacterProfileRef>()
                 ?? s.GetComponentInParent<CharacterProfileRef>(true)
                 ?? s.GetComponentInChildren<CharacterProfileRef>(true);
            if (r && r.profile == p) return s;
        }

        return null;
    }

    /// <summary>Best-effort scene lookup by profile displayName or asset name.</summary>
    public CharacterProfileSO FindProfileByName(string displayOrAssetName)
    {
        if (string.IsNullOrWhiteSpace(displayOrAssetName)) return null;

        string want = displayOrAssetName.Trim();
        var refs = FindObjectsOfType<CharacterProfileRef>(true);

        // Try displayName first, then asset name (case-insensitive)
        foreach (var r in refs)
        {
            if (r && r.profile)
            {
                if (!string.IsNullOrEmpty(r.profile.displayName) &&
                    string.Equals(r.profile.displayName, want, StringComparison.OrdinalIgnoreCase))
                    return r.profile;

                if (string.Equals(r.profile.name, want, StringComparison.OrdinalIgnoreCase))
                    return r.profile;
            }
        }

        return null;
    }
}
