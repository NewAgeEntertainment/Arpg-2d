using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Conquest : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject rosterRoot;
    [SerializeField] private GameObject detailsRoot;

    [Header("Roster")]
    [SerializeField] private RectTransform rosterListRoot;   // ScrollView/Viewport/Content
    [SerializeField] private Button rosterItemPrefab;        // Button (can have UI_ConquestRosterItem)
    [SerializeField] private TMP_Text rosterHeaderText;

    [Tooltip("Optional: drag scene objects with CharacterProfileRef here")]
    [SerializeField] private List<CharacterProfileRef> manualRoster = new();

    [Header("Identity (Details)")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text raceText;
    [SerializeField] private TMP_Text genderText;
    [SerializeField] private TMP_Text masteryText;           // "Sex Mastery: ..."

    [Header("Affection (Details)")]
    [SerializeField] private Slider affectionSlider;
    [SerializeField] private TMP_Text affectionValueText;

    [Header("Arousal / Pleasure (optional)")]
    [SerializeField] private Slider arousalSlider;
    [SerializeField] private TMP_Text arousalPointsText;     // "0/100"
    [SerializeField] private TMP_Text sexualDamageText;
    [SerializeField] private TMP_Text sexualRestraintText;

    [Header("Description (optional)")]
    [SerializeField] private TMP_Text shortDescriptionText;

    [Header("Optional defaults (used only for Open/Direct select)")]
    [SerializeField] private CharacterProfileSO profile;     // if you want to jump straight to details
    [SerializeField] private Entity_Stats entityStats;

    // runtime
    private CharacterProfileSO currentProfile;
    private Entity_Stats currentStats;
    private readonly List<GameObject> _spawnedRosterButtons = new();

    // -------------------- Unity --------------------

    private void OnEnable()
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr != null)
        {
            mgr.OnAffectionChanged += HandleAffectionChanged;
            mgr.OnRosterChanged += HandleRosterChanged;
        }
    }

    private void OnDisable()
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr != null)
        {
            mgr.OnAffectionChanged -= HandleAffectionChanged;
            mgr.OnRosterChanged -= HandleRosterChanged;
        }
    }

    // -------------------- Open/Close --------------------

    public bool IsOpen => gameObject.activeInHierarchy;


    public void Open()
    {
        gameObject.SetActive(true);
        ShowRosterFirst();
    }

    // Called by UI.cs
    public void OpenRosterFirst()
    {
        gameObject.SetActive(true);
        ShowRosterFirst();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public bool HandleCancel()
    {
        // If details open → go back to roster
        if (detailsRoot != null && detailsRoot.activeSelf)
        {
            ShowRosterFirst();
            return true;
        }

        // Otherwise just close conquest panel.
        // ✅ Do NOT open main menu here (UI.cs will do it with book close).
        Close();
        return true;
    }


    // -------------------- Roster --------------------

    private void ShowRosterFirst()
    {
        if (rosterHeaderText) rosterHeaderText.text = "Roster";
        if (rosterRoot) rosterRoot.SetActive(true);
        if (detailsRoot) detailsRoot.SetActive(false);
        BuildRoster();
    }

    private void HandleRosterChanged()
    {
        if (rosterRoot != null && rosterRoot.activeSelf)
            BuildRoster();
    }

    private void BuildRoster()
    {
        // Get entries first (so we can log count even if fields are missing)
        var entries = GetRosterEntries();
        Debug.Log($"[Conquest/UI] BuildRoster: entries={entries.Count}");

        // Clear old items
        foreach (var go in _spawnedRosterButtons) if (go) Destroy(go);
        _spawnedRosterButtons.Clear();

        if (rosterListRoot == null || rosterItemPrefab == null)
        {
            Debug.LogWarning("[UI_Conquest] Roster List Root or Roster Item Prefab is not assigned.");
            return;
        }

        if (entries.Count == 0)
        {
            var placeholder = Instantiate(rosterItemPrefab, rosterListRoot);
            var label = placeholder.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "No characters with profiles found";
            placeholder.interactable = false;
            _spawnedRosterButtons.Add(placeholder.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rosterListRoot);
            return;
        }

        foreach (var (p, s) in entries)
        {
            var btn = Instantiate(rosterItemPrefab, rosterListRoot);

            // If your prefab has a custom item component, use it
            var item = btn.GetComponent<UI_ConquestRosterItem>();
            if (item != null)
            {
                item.Init(this, p, s);
            }
            else
            {
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label) label.text = string.IsNullOrEmpty(p.displayName) ? p.name : p.displayName;

                var localP = p; var localS = s;
                btn.onClick.AddListener(() => ShowDetails(localP, localS));
            }

            _spawnedRosterButtons.Add(btn.gameObject);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rosterListRoot);
    }

    /// <summary>
    /// Prefer the manual roster automatically if it has any valid entries;
    /// otherwise fall back to the manager's unlocked list.
    /// </summary>
    private List<(CharacterProfileSO profile, Entity_Stats stats)> GetRosterEntries()
    {
        var result = new List<(CharacterProfileSO, Entity_Stats)>();

        // Prefer manual roster automatically if it contains entries.
        bool manualHasEntries = manualRoster != null && manualRoster.Any(r => r && r.profile);
        if (manualHasEntries)
        {
            foreach (var r in manualRoster)
            {
                if (!r || !r.profile) continue;
                var s = r.GetComponent<Entity_Stats>()
                     ?? r.GetComponentInParent<Entity_Stats>(true)
                     ?? r.GetComponentInChildren<Entity_Stats>(true);
                result.Add((r.profile, s));
            }
            return result;
        }

        // Otherwise, use the manager’s unlocked list.
        var mgr = ConquestRosterManager.Instance;
        if (mgr != null && mgr.Entries != null)
        {
            foreach (var e in mgr.Entries)
            {
                if (e?.profile == null) continue;
                var s = e.stats ? e.stats : mgr.FindStatsForProfile(e.profile);
                result.Add((e.profile, s));
            }
        }

        return result;
    }

    // -------------------- Details --------------------

    // Called by roster items OR externally
    public void OnRosterItemSelected(UI_ConquestRosterItem item)
    {
        if (item == null) return;
        ShowDetails(item.Profile, item.Stats);
    }

    public void ShowDetails(CharacterProfileSO p, Entity_Stats s = null)
    {
        currentProfile = p;
        currentStats = s;

        if (currentProfile == null)
        {
            Debug.LogWarning("[UI_Conquest] ShowDetails called with null profile.");
            return;
        }

        if (detailsRoot) detailsRoot.SetActive(true);
        if (rosterRoot) rosterRoot.SetActive(false);

        // Identity
        if (portraitImage) portraitImage.sprite = currentProfile.portrait;
        if (nameText) nameText.text = string.IsNullOrEmpty(currentProfile.displayName) ? currentProfile.name : currentProfile.displayName;
        if (raceText) raceText.text = $"Race: {SafeString(currentProfile.race)}";
        if (genderText) genderText.text = $"Gender: {SafeString(currentProfile.gender)}";
        if (masteryText) masteryText.text = $"Sex Mastery: {ResolveSexMasteryText(currentProfile, currentStats)}";

        // Description
        if (shortDescriptionText) shortDescriptionText.text = SafeString(currentProfile.shortDescription);

        // Affection
        RefreshAffectionUI(currentProfile);

        // Stats-ish
        FillStatsSection(currentStats);
    }

    private static string SafeString(object v) => v == null ? "-" : v.ToString();

    /// <summary>
    /// Get a readable "sex mastery" without hard-referencing a field that may not exist.
    /// </summary>
    private string ResolveSexMasteryText(CharacterProfileSO p, Entity_Stats s)
    {
        if (p != null)
        {
            var t = p.GetType();
            // Try fields
            var f = t.GetField("sexMastery") ?? t.GetField("SexMastery") ??
                    t.GetField("mastery") ?? t.GetField("Mastery") ??
                    t.GetField("masteryLevel") ?? t.GetField("MasteryLevel");
            if (f != null)
            {
                var val = f.GetValue(p);
                if (val != null) return val.ToString();
            }
            // Try properties
            var prop = t.GetProperty("sexMastery") ?? t.GetProperty("SexMastery") ??
                       t.GetProperty("mastery") ?? t.GetProperty("Mastery") ??
                       t.GetProperty("masteryLevel") ?? t.GetProperty("MasteryLevel");
            if (prop != null)
            {
                var val = prop.GetValue(p, null);
                if (val != null) return val.ToString();
            }
        }

        // Fallback: derive a bucket from stats if available
        if (s != null && s.sex != null)
        {
            float dmg = s.sex.sexualDamage.GetValue();
            if (dmg >= 80) return "Master";
            if (dmg >= 60) return "Expert";
            if (dmg >= 40) return "Advanced";
            if (dmg >= 20) return "Intermediate";
            return "Beginner";
        }

        return "-";
    }

    private void FillStatsSection(Entity_Stats s)
    {
        // Arousal points (0 / Max)
        if (arousalSlider || arousalPointsText)
        {
            float maxArousal = 100f;
            if (s != null && s.sex != null)
                maxArousal = s.sex.maxArousal.GetValue();

            if (arousalSlider)
            {
                arousalSlider.minValue = 0f;
                arousalSlider.maxValue = maxArousal;
                arousalSlider.value = 0f; // bind to a live value if you have it
            }

            if (arousalPointsText)
                arousalPointsText.text = $"0/{maxArousal:0}";
        }

        // Sexual damage / restraint
        if (s != null && s.sex != null)
        {
            if (sexualDamageText) sexualDamageText.text = $"Sexual Damage: {s.sex.sexualDamage.GetValue():0}";
            if (sexualRestraintText) sexualRestraintText.text = $"Sexual Restraint: {s.sex.sexualRestraint.GetValue():0}";
        }
        else
        {
            if (sexualDamageText) sexualDamageText.text = "Sexual Damage: -";
            if (sexualRestraintText) sexualRestraintText.text = "Sexual Restraint: -";
        }
    }

    // -------------------- Affection live updates --------------------

    private void RefreshAffectionUI(CharacterProfileSO profileToShow)
    {
        var mgr = ConquestRosterManager.Instance;

        int value = 0;
        int max = 100;
        int min = 0;

        if (mgr != null)
        {
            value = mgr.GetAffection(profileToShow);
            max = mgr.AffectionMax;
            min = mgr.AffectionMin;
        }

        if (affectionSlider)
        {
            affectionSlider.minValue = min;
            affectionSlider.maxValue = max;
            affectionSlider.value = value;
        }

        if (affectionValueText)
            affectionValueText.text = value.ToString();
    }

    private void HandleAffectionChanged(CharacterProfileSO profile, int newValue)
    {
        if (detailsRoot != null && detailsRoot.activeSelf && profile == currentProfile)
            RefreshAffectionUI(profile);
    }

    // -------------------- Buttons --------------------

    public void OnBackFromDetails() => ShowRosterFirst();

    public void ForceRosterRefresh() => BuildRoster();

    // -------------------- Debug helpers (Inspector context menu) --------------------

    [ContextMenu("Debug/Validate Bindings")]
    private void DebugValidateBindings()
    {
        Debug.Log($"[Conquest/UI] rosterRoot={(rosterRoot ? rosterRoot.name : "NULL")}");
        Debug.Log($"[Conquest/UI] rosterListRoot={(rosterListRoot ? rosterListRoot.name : "NULL")}  // should be ScrollView/Viewport/Content");
        Debug.Log($"[Conquest/UI] rosterItemPrefab={(rosterItemPrefab ? rosterItemPrefab.name : "NULL")}");
        if (!rosterListRoot) Debug.LogError("[Conquest/UI] Roster List Root is NOT assigned.");
        if (!rosterItemPrefab) Debug.LogError("[Conquest/UI] Roster Item Prefab is NOT assigned.");
    }

    [ContextMenu("Debug/Build Roster Now")]
    private void DebugBuildRosterNow() => BuildRoster();
}
