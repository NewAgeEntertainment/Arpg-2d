using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SexyTimeUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panel;

    [Header("Bars")]
    [SerializeField] private Slider playerBar;
    [SerializeField] private Slider partnerBar;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerBarFillText;
    [SerializeField] private TextMeshProUGUI partnerBarFillText;
    [SerializeField] private TextMeshProUGUI playerPowerText;
    [SerializeField] private TextMeshProUGUI partnerPowerText;
    [SerializeField] private TextMeshProUGUI npcCooldownText;
    [SerializeField] private TextMeshProUGUI critText;

    [Header("Sex Hotbar (optional)")]
    [SerializeField] private SexyTimeHotbar sexHotbar;

    [Header("Single DeepBreath Slot (optional)")]
    [SerializeField] private UI_SkillSlot deepBreathSlot;

    // ===== NEW: Persistent sticky sex skills =====
    private static readonly System.Collections.Generic.List<Skill_DataSO> s_pendingSexSkills = new();
    private static readonly System.Collections.Generic.Dictionary<int, Skill_DataSO> s_indexedSexSkills = new();

    // ===== NEW: Quick state helpers =====
    public bool IsOpen => panel != null && panel.activeInHierarchy;
    public int SexSlotCount => sexHotbar != null ? sexHotbar.SlotCount : 0;

    // --- Assign-preview helpers ---
    private bool _openedByAssignPreview = false;

 

    public UI_SkillSlot GetSexSlotByIndex(int index)
    {
        if (sexHotbar == null || sexHotbar.Slots == null) return null;
        if (index < 0 || index >= sexHotbar.Slots.Length) return null;
        return sexHotbar.Slots[index];
    }

    private static bool SameSkill(Skill_DataSO a, Skill_DataSO b)
    {
        if (a == null || b == null) return false;
        if (!string.IsNullOrEmpty(a.id) && !string.IsNullOrEmpty(b.id)) return a.id == b.id;
        return ReferenceEquals(a, b);
    }

    public static void AddPersistentSexSkill(Skill_DataSO sexSkill)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return;

        // de-dupe by id if set, else by ref
        if (!string.IsNullOrEmpty(sexSkill.id))
        {
            for (int i = 0; i < s_pendingSexSkills.Count; i++)
            {
                var s = s_pendingSexSkills[i];
                if (s != null && s.id == sexSkill.id)
                {
                    s_pendingSexSkills[i] = sexSkill;
                    return;
                }
            }
        }
        else
        {
            if (s_pendingSexSkills.Contains(sexSkill)) return;
        }

        s_pendingSexSkills.Add(sexSkill);
    }

    public static void SetPersistentSexSkillAtIndex(int index, Skill_DataSO sexSkill)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return;
        s_indexedSexSkills[index] = sexSkill;
        AddPersistentSexSkill(sexSkill);
    }

    public static void ClearPersistentSexSkills()
    {
        s_pendingSexSkills.Clear();
        s_indexedSexSkills.Clear();
    }

    private void ApplyPendingSexSkillsToHotbar()
    {
        if (sexHotbar == null || sexHotbar.Slots == null) return;

        // 1) Place explicit indexed skills first
        foreach (var kvp in s_indexedSexSkills)
        {
            int idx = kvp.Key;
            var skill = kvp.Value;
            if (skill != null) sexHotbar.TryAssignToIndex(idx, skill);
        }

        // 2) Fill remaining empty slots with any leftover pending skills
        foreach (var skill in s_pendingSexSkills)
        {
            if (skill == null) continue;

            bool already = false;
            foreach (var s in sexHotbar.Slots)
            {
                if (s != null && s.HasSkill && SameSkill(s.Data, skill))
                {
                    already = true;
                    break;
                }
            }
            if (!already) sexHotbar.TryAssign(skill);
        }
    }

    private void Awake()
    {
        // Ensure stickies are applied even if this UI spawns after assignments were chosen.
        ApplyPendingSexSkillsToHotbar();
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        HideCrit();
        RefreshAffordability(null); // clears fades until we get player mana

        // Make sure persisteds are applied when showing
        ApplyPendingSexSkillsToHotbar();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ---------- Bars ----------
    public void InitBars(float playerMax, float partnerMax)
    {
        if (playerBar != null)
        {
            playerBar.maxValue = playerMax;
            playerBar.value = 0f;
            if (playerBarFillText != null)
                playerBarFillText.text = $"0 / {Mathf.FloorToInt(playerMax)}";
        }

        if (partnerBar != null)
        {
            partnerBar.maxValue = partnerMax;
            partnerBar.value = 0f;
            if (partnerBarFillText != null)
                partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerMax)}";
        }
    }

    public void UpdateBars(float playerVal, float playerMax, float partnerVal, float partnerMax)
    {
        if (playerBar != null) playerBar.value = playerVal;
        if (partnerBar != null) partnerBar.value = partnerVal;

        if (playerBarFillText != null)
            playerBarFillText.text = $"{Mathf.FloorToInt(playerVal)} / {Mathf.FloorToInt(playerMax)}";

        if (partnerBarFillText != null)
            partnerBarFillText.text = $"{Mathf.FloorToInt(partnerVal)} / {Mathf.FloorToInt(partnerMax)}";
    }

    public void UpdatePower(float playerStroke, float partnerStroke)
    {
        if (playerPowerText != null) playerPowerText.text = $"+{playerStroke:F0}";
        if (partnerPowerText != null) partnerPowerText.text = $"+{partnerStroke:F0}";
    }

    public void UpdateCooldown(float remaining)
    {
        if (npcCooldownText != null) npcCooldownText.text = remaining.ToString("F1");
    }

    public void ShowCrit()
    {
        if (critText == null) return;
        critText.gameObject.SetActive(true);
        critText.text = "💥 Critical Stroke!";
        CancelInvoke(nameof(HideCrit));
        Invoke(nameof(HideCrit), 1f);
    }

    private void HideCrit()
    {
        if (critText != null) critText.gameObject.SetActive(false);
    }

  

    public void ShowAssignPreview()
    {
        if (IsOpen) return;
        _openedByAssignPreview = true;
        Show();     // your existing method that enables the panel
    }

    public void HideAssignPreview()
    {
        if (!_openedByAssignPreview) return;
        _openedByAssignPreview = false;
        Hide();     // your existing method that disables the panel
    }


    // ---------- Sex skill presentation ----------
    /// Assigns a Sex-category skill to the sex hotbar (or the single slot fallback).
    public void ShowSexSkill(Skill_DataSO sexSkill)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return;

        // Keep persistent record up to date
        AddPersistentSexSkill(sexSkill);

        bool assigned = false;

        if (sexHotbar != null)
            assigned = sexHotbar.TryAssign(sexSkill);

        if (!assigned && deepBreathSlot != null)
        {
            deepBreathSlot.gameObject.SetActive(true);
            deepBreathSlot.SetupSkillSlot(sexSkill);
        }
    }

    public void HideSexSkills()
    {
        if (sexHotbar != null) sexHotbar.ClearAll();
        if (deepBreathSlot != null) deepBreathSlot.gameObject.SetActive(false);
    }

    public void StartSexSkillCooldown(float cooldownSeconds)
    {
        if (sexHotbar != null && sexHotbar.Slots != null)
        {
            foreach (var s in sexHotbar.Slots)
                if (s != null && s.HasSkill) s.StartCooldown(cooldownSeconds);
        }
        if (deepBreathSlot != null && deepBreathSlot.gameObject.activeSelf)
            deepBreathSlot.StartCooldown(cooldownSeconds);
    }

    /// Call this whenever the player's mana changes so the hotbar reflects affordability.
    public void RefreshAffordability(Entity_Mana playerMana)
    {
        if (sexHotbar != null && sexHotbar.Slots != null)
        {
            foreach (var s in sexHotbar.Slots)
                if (s != null) s.UpdateAffordability(playerMana);
        }
        if (deepBreathSlot != null) deepBreathSlot.UpdateAffordability(playerMana);
    }

    public void ResetAll()
    {
        UpdateBars(0f, PlayerBarMax, 0f, PartnerBarMax);
        UpdatePower(0f, 0f);
        UpdateCooldown(0f);
        HideCrit();
    }

    public bool IsVisible => IsOpen;

    // Expose the hotbar so code like sexUI.Hotbar.Slots works:
    public SexyTimeHotbar Hotbar => sexHotbar;

    // Optional aliases if any code expects these:
    public int SlotCount => SexSlotCount;
    public UI_SkillSlot GetSlotByIndex(int index) => GetSexSlotByIndex(index);
    public UI_SkillSlot GetSlot(int index) => GetSexSlotByIndex(index);

    // Small helpers so logic can read/write bar values via UI controller if desired
    public float PlayerBarValue => playerBar != null ? playerBar.value : 0f;
    public float PartnerBarValue => partnerBar != null ? partnerBar.value : 0f;
    public float PlayerBarMax => playerBar != null ? playerBar.maxValue : 100f;
    public float PartnerBarMax => partnerBar != null ? partnerBar.maxValue : 100f;

    // Allow logic to push directly
    public Slider PlayerBar => playerBar;
    public Slider PartnerBar => partnerBar;

    // ---------- Compatibility wrappers (so older scripts still compile) ----------
    public void StartDeepBreathCooldown(float cooldown) => StartSexSkillCooldown(cooldown);

    public void ShowDeepBreathSlot(Skill_DataSO skillData) => ShowSexSkill(skillData);
    public void HideDeepBreathSlot() => HideSexSkills();

    // ===== NEW: direct assignment to a specific sex slot index + persistence =====
    public bool AssignSexSkillToIndex(Skill_DataSO sexSkill, int index)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return false;
        if (sexHotbar == null) return false;

        bool ok = sexHotbar.TryAssignToIndex(index, sexSkill);
        if (ok) SetPersistentSexSkillAtIndex(index, sexSkill);
        return ok;
    }
}
