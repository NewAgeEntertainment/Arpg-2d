using System.Collections.Generic;
using UnityEngine;

public class CompanionPartyManager : MonoBehaviour
{
    public static CompanionPartyManager Instance { get; private set; }

    [Header("Player / Roots")]
    public Transform player;                  // drag your Player root here
    public Transform partyRoot;               // empty object to parent companions under

    [System.Serializable]
    public class RosterEntry
    {
        public string id;
        public GameObject prefab;             // prefab to instantiate if no scene instance
        public Transform sceneInstance;       // optional existing instance in scene
        public bool startInParty = false;
    }

    [Header("Roster")]
    public List<RosterEntry> roster = new List<RosterEntry>();

    [Header("Spawn")]
    public float spawnOffset = 0.8f;          // radial distance from player
    public float ringSpacing = 0.6f;          // extra distance per companion to spread them

    [Header("Behavior")]
    public bool forceFollowOnRecruit = true;  // jump to follow state on recruit
    public bool reactivateIfAlreadyInScene = true;

    private readonly Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Auto-find player if not set
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Ensure party root
        if (partyRoot == null)
        {
            var go = new GameObject("PartyRoot");
            partyRoot = go.transform;
        }

        // Start-in-party
        foreach (var e in roster)
        {
            if (e.startInParty) Recruit(e.id, silent: true);
        }
    }

    // ----------------------------------------------------
    // Public API
    // ----------------------------------------------------

    public IEnumerable<string> ActiveIds()
    {
        foreach (var kv in _active) yield return kv.Key;
    }

    public GameObject FindActiveInstance(string id)
    {
        if (!string.IsNullOrEmpty(id) && _active.TryGetValue(id, out var go)) return go;
        return null;
    }

    public GameObject Recruit(string id, bool silent = false)
    {
        var entry = GetEntry(id);
        if (entry == null) { Debug.LogWarning($"[CompanionPartyManager] No roster entry id '{id}'"); return null; }

        // Already active?
        if (_active.TryGetValue(id, out var existing) && existing)
        {
            if (reactivateIfAlreadyInScene && !existing.activeSelf) existing.SetActive(true);
            FlagInParty(existing, true);
            return existing;
        }

        GameObject instance = null;

        // Prefer a scene instance if assigned
        if (entry.sceneInstance != null)
        {
            instance = entry.sceneInstance.gameObject;
            if (reactivateIfAlreadyInScene && !instance.activeSelf) instance.SetActive(true);
        }
        else if (entry.prefab != null)
        {
            instance = Instantiate(entry.prefab, partyRoot);
            instance.transform.position = GetSpawnPosition();
        }
        else
        {
            Debug.LogWarning($"[CompanionPartyManager] Roster '{id}' has no Prefab or Scene Instance.");
            return null;
        }

        SetupCompanion(instance);
        if (forceFollowOnRecruit) ForceFollow(instance);
        FlagInParty(instance, true);
        _active[id] = instance;

        if (!silent) Debug.Log($"[CompanionPartyManager] Recruited '{id}'");
        return instance;
    }

    public void Dismiss(string id, bool destroy = false)
    {
        var go = FindActiveInstance(id);
        if (go == null) return;

        FlagInParty(go, false);

        if (destroy) Destroy(go);
        else go.SetActive(false);

        _active.Remove(id);
        Debug.Log($"[CompanionPartyManager] Dismissed '{id}'");
    }

    public void DismissAll(bool destroy = false)
    {
        var list = new List<string>(_active.Keys);
        foreach (var id in list) Dismiss(id, destroy);
    }

    // ----------------------------------------------------
    // Internals
    // ----------------------------------------------------

    private RosterEntry GetEntry(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < roster.Count; i++)
            if (roster[i] != null && roster[i].id == id) return roster[i];
        return null;
    }

    private void SetupCompanion(GameObject go)
    {
        // Parent under PartyRoot (keeps hierarchy neat)
        if (partyRoot != null) go.transform.SetParent(partyRoot, worldPositionStays: true);

        // Point to player
        var comp = go.GetComponent<Companion>();
        if (comp != null && player != null) comp.playerTarget = player;
    }

    private void ForceFollow(GameObject go)
    {
        var comp = go.GetComponent<Companion>();
        if (comp != null)
        {
            // Nudge the state machine to a following stance (SetInParty also does this)
            comp.SetInParty(true);
        }
    }

    private void FlagInParty(GameObject go, bool value)
    {
        var comp = go.GetComponent<Companion>();
        if (comp != null) comp.SetInParty(value);
    }

    private Vector3 GetSpawnPosition()
    {
        if (player == null) return Vector3.zero;

        // Simple radial placement around player based on active count
        int n = _active.Count + 1; // about to add one
        float radius = spawnOffset + (n * ringSpacing * 0.25f);
        float angle = n * 110f * Mathf.Deg2Rad; // spread a bit
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        return player.position + offset;
    }
}
