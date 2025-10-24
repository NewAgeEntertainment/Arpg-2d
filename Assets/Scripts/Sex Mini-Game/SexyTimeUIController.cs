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

    [Header("Bars Group (optional)")]
    [SerializeField] private GameObject barsGroup; // drag a parent that contains BOTH sliders + all bar texts

    // Internal restore state
    private bool _barsHiddenForAssign;
    private bool _prevBarsGroupActive;
    private bool _prevPlayerBarActive, _prevPartnerBarActive;
    private bool _prevPlayerFillTextActive, _prevPartnerFillTextActive;
    private bool _prevPlayerPowerTextActive, _prevPartnerPowerTextActive;
    private bool _prevNpcCooldownTextActive;

    [Header("Sex Hotbar (optional)")]
    [SerializeField] private SexyTimeHotbar sexHotbar;

    [Header("Single DeepBreath Slot (optional)")]
    [SerializeField] private UI_SkillSlot deepBreathSlot;

    // ===== Persistent sticky sex skills =====
    private static readonly System.Collections.Generic.List<Skill_DataSO> s_pendingSexSkills = new();
    private static readonly System.Collections.Generic.Dictionary<int, Skill_DataSO> s_indexedSexSkills = new();

    // ===== Quick state helpers =====
    public bool IsOpen => panel != null && panel.activeInHierarchy;
    public int SexSlotCount => sexHotbar != null ? sexHotbar.SlotCount : 0;

    // --- Assign-preview helpers ---
    private bool _openedByAssignPreview = false;

    // ======== FOLLOW TARGET SUPPORT (kept from earlier) ========
    [Header("Follow (optional)")]
    [Tooltip("If set, the UI will follow this Transform (e.g., SexyTimeLogic.transform or Player.transform).")]
    [SerializeField] private Transform followTarget;
    [Tooltip("If true, UI keeps following every frame while open. If false, only snaps once on AttachTo/SetFollowTarget.")]
    [SerializeField] private bool followWhileOpen = false;
    [Tooltip("World offset when using a World Space canvas.")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [Tooltip("Pixel offset when using a Screen Space canvas.")]
    [SerializeField] private Vector2 screenOffset = Vector2.zero;

    private Canvas _rootCanvas;
    private RectTransform _uiRoot;
    private Camera _uiCam;
    // ===========================================================

    // === NEW: tiny public helpers so logic can ask about the panel/canvas/camera ===
    public RectTransform PanelRect => panel != null ? panel.transform as RectTransform : null;

    public Canvas RootCanvas
    {
        get
        {
            if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>(true);
            return _rootCanvas;
        }
    }

    public Camera UICamera
    {
        get
        {
            if (RootCanvas != null && RootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                return _uiCam != null ? _uiCam : RootCanvas.worldCamera;
            return null; // null means Overlay (no UI camera)
        }
    }
    // ===============================================================================

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

        // cache canvas bits for follow support
        _rootCanvas = GetComponentInParent<Canvas>(true);
        _uiRoot = (panel != null ? panel.transform : transform) as RectTransform;

        if (_rootCanvas != null && _rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            _uiCam = _rootCanvas.worldCamera;

        if (_uiCam == null) _uiCam = Camera.main;
    }

    private void LateUpdate()
    {
        // Only run follow logic if enabled and visible.
        if (!followWhileOpen || !IsOpen) return;
        if (followTarget == null || RootCanvas == null || _uiRoot == null) return;

        SnapToTarget();
    }

    /// <summary>Attach this UI to a SexyTimeLogic so it follows its Transform.</summary>
    public void AttachTo(SexyTimeLogic logic)
    {
        if (logic == null) return;
        followTarget = logic.transform;
        followWhileOpen = true;  // safe default: stick while the panel is open
        SnapToTarget();          // initial placement
    }

    /// <summary>Attach to any Transform. (One-time snap unless followWhileOpen is true.)</summary>
    public void SetFollowTarget(Transform t, bool enableFollowWhileOpen = true)
    {
        followTarget = t;
        followWhileOpen = enableFollowWhileOpen;
        SnapToTarget();
    }

    /// <summary>Immediately re-position the UI to the current follow target.</summary>
    public void SnapToTarget()
    {
        if (followTarget == null || RootCanvas == null || _uiRoot == null) return;

        // WORLD SPACE CANVAS: move in world
        if (RootCanvas.renderMode == RenderMode.WorldSpace)
        {
            RootCanvas.transform.position = followTarget.position + worldOffset;
            return;
        }

        // SCREEN SPACE (Overlay or Camera): convert world → canvas local
        var cam = (RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (_uiCam != null ? _uiCam : Camera.main);
        Vector3 screenPos;

        if (Camera.main != null)
            screenPos = Camera.main.WorldToScreenPoint(followTarget.position);
        else
            screenPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

        RectTransform canvasRect = RootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out localPoint);
        _uiRoot.anchoredPosition = localPoint + screenOffset;
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        HideCrit();
        RefreshAffordability(null); // clears fades until we get player mana

        // Make sure persisteds are applied when showing
        ApplyPendingSexSkillsToHotbar();

        // If a follow target is already set and we just opened, do a one-time snap.
        if (followTarget != null) SnapToTarget();
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
        // Ensure panel is open while picking
        if (!IsOpen) { _openedByAssignPreview = true; Show(); }
        else { _openedByAssignPreview = false; }

        // Hide the pleasure bars during assignment to avoid clutter
        HideBarsForAssign();
    }

    public void HideAssignPreview()
    {
        // Restore bars visibility first
        RestoreBarsAfterAssign();

        // Only hide the whole panel if we opened it just for preview
        if (!_openedByAssignPreview) return;
        _openedByAssignPreview = false;
        Hide();
    }

    public void HideBarsForAssign()
    {
        if (_barsHiddenForAssign) return;
        _barsHiddenForAssign = true;

        if (barsGroup != null)
        {
            _prevBarsGroupActive = barsGroup.activeSelf;
            barsGroup.SetActive(false);
            return;
        }

        if (playerBar != null) { _prevPlayerBarActive = playerBar.gameObject.activeSelf; playerBar.gameObject.SetActive(false); }
        if (partnerBar != null) { _prevPartnerBarActive = partnerBar.gameObject.activeSelf; partnerBar.gameObject.SetActive(false); }
        if (playerBarFillText != null) { _prevPlayerFillTextActive = playerBarFillText.gameObject.activeSelf; playerBarFillText.gameObject.SetActive(false); }
        if (partnerBarFillText != null) { _prevPartnerFillTextActive = partnerBarFillText.gameObject.activeSelf; partnerBarFillText.gameObject.SetActive(false); }
        if (playerPowerText != null) { _prevPlayerPowerTextActive = playerPowerText.gameObject.activeSelf; playerPowerText.gameObject.SetActive(false); }
        if (partnerPowerText != null) { _prevPartnerPowerTextActive = partnerPowerText.gameObject.activeSelf; partnerPowerText.gameObject.SetActive(false); }
        if (npcCooldownText != null) { _prevNpcCooldownTextActive = npcCooldownText.gameObject.activeSelf; npcCooldownText.gameObject.SetActive(false); }
    }

    public void RestoreBarsAfterAssign()
    {
        if (!_barsHiddenForAssign) return;
        _barsHiddenForAssign = false;

        if (barsGroup != null)
        {
            barsGroup.SetActive(_prevBarsGroupActive);
            return;
        }

        if (playerBar != null) playerBar.gameObject.SetActive(_prevPlayerBarActive);
        if (partnerBar != null) partnerBar.gameObject.SetActive(_prevPartnerBarActive);
        if (playerBarFillText != null) playerBarFillText.gameObject.SetActive(_prevPlayerFillTextActive);
        if (partnerBarFillText != null) partnerBarFillText.gameObject.SetActive(_prevPartnerFillTextActive);
        if (playerPowerText != null) playerPowerText.gameObject.SetActive(_prevPlayerPowerTextActive);
        if (partnerPowerText != null) partnerPowerText.gameObject.SetActive(_prevPartnerPowerTextActive);
        if (npcCooldownText != null) npcCooldownText.gameObject.SetActive(_prevNpcCooldownTextActive);
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

    // ===== direct assignment to a specific sex slot index + persistence =====
    public bool AssignSexSkillToIndex(Skill_DataSO sexSkill, int index)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return false;
        if (sexHotbar == null) return false;

        bool ok = sexHotbar.TryAssignToIndex(index, sexSkill);
        if (ok) SetPersistentSexSkillAtIndex(index, sexSkill);
        return ok;
    }
}
