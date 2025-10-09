using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillTree : UI_Panel
{
    [Header("Point Pools")]
    [SerializeField] private int skillPoints;         // Combat points
    [SerializeField] private int sexSkillPoints;      // Sex points

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private TextMeshProUGUI sexSkillPointsText;

    [Header("Graph Roots (connection updaters)")]
    [SerializeField] private UI_TreeConnectHandler[] parentNodes;

    public Player_SkillManager skillManager { get; private set; }

    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private UI_TreeNode pendingSkillNode;

    // ===== Assign-to-slot popup ===== (kept for back-compat, not used in pick mode)
    [Header("Assign To Slot Popup (legacy)")]
    [SerializeField] private GameObject assignPopup;
    [SerializeField] private TextMeshProUGUI assignTitleText;
    [SerializeField] private Button assignSlotAButton;
    [SerializeField] private Button assignSlotBButton;
    [SerializeField] private Button assignSlotCButton;
    [SerializeField] private Button assignSlotDButton;
    [SerializeField] private Button assignCancelButton;
    private UI_TreeNode _pendingAssignNode;

    // ===== NEW: Click-to-assign pick mode =====
    private static UI_SkillTree _pickOwner;
    private static UI_TreeNode _pickNode;
    private static Skill_DataSO _pickSkill;

    public static bool IsPicking => _pickOwner != null && _pickNode != null && _pickSkill != null;
    public static Skill_DataSO PendingPickSkill => _pickSkill;

    [Header("Assign Pick Visuals")]
    [SerializeField] private Color nodePickColor = new Color(1f, 0.9f, 0.4f, 1f);

    #region State Type (matches GameDataSaver.SkillTreeState)
    [System.Serializable]
    public class SkillTreeState
    {
        public List<string> unlockedSkillNames = new();
        public List<string> lockedSkillNames = new();
        public int combatSkillPoints;
        public int sexSkillPoints;
    }
    #endregion

    #region Pending State (used when saver applies before tree exists)
    private static SkillTreeState _pendingState;
    public static void SetPendingState(SkillTreeState s)
    {
        if (s == null) { _pendingState = null; return; }
        _pendingState = new SkillTreeState
        {
            unlockedSkillNames = new List<string>(s.unlockedSkillNames ?? new List<string>()),
            lockedSkillNames = new List<string>(s.lockedSkillNames ?? new List<string>()),
            combatSkillPoints = s.combatSkillPoints,
            sexSkillPoints = s.sexSkillPoints
        };
    }
    #endregion

    private void Awake()
    {
        skillManager = FindAnyObjectByType<Player_SkillManager>();
    }

    private void Start()
    {
        UnlockDefaultSkills();     // default nodes
        UpdateAllConnections();
        UpdateSkillPointsUI();

        if (_pendingState != null)
            StartCoroutine(ApplyPendingAfterFrame());
    }

    private IEnumerator ApplyPendingAfterFrame()
    {
        yield return null;
        ApplySaveState(_pendingState);
        UpdateAllConnections();
        _pendingState = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If we're in the click-to-assign flow, exit THAT first and do NOT fall through
            var ui = UI.Instance;
            if (ui != null && ui.IsAssignPreviewActive)
            {
                ui.RequestExitAssignPreview();
                return; // <- prevents HandleCancel() from sending you to main menu
            }

            HandleCancel();
        }
    }

    public bool IsOpen => gameObject.activeInHierarchy;


    #region Public API used by Saver/UI

    public int GetCombatSkillPoints() => skillPoints;
    public int GetSexSkillPoints() => sexSkillPoints;

    public void SetCombatSkillPoints(int value)
    {
        skillPoints = Mathf.Max(0, value);
        UpdateSkillPointsUI();
    }

    public void SetSexSkillPoints(int value)
    {
        sexSkillPoints = Mathf.Max(0, value);
        UpdateSexSkillPointsUI();
    }

    public void AddSkillPoints(int points)
    {
        skillPoints += Mathf.Max(0, points);
        UpdateSkillPointsUI();
    }

    public void AddSexSkillPoints(int points)
    {
        sexSkillPoints += Mathf.Max(0, points);
        UpdateSexSkillPointsUI();
    }

    public bool EnoughSkillPoints(int cost) => skillPoints >= cost;
    public bool EnoughSexSkillPoints(int cost) => sexSkillPoints >= cost;

    public void RemoveSkillPoints(int cost)
    {
        skillPoints = Mathf.Max(0, skillPoints - Mathf.Max(0, cost));
        UpdateSkillPointsUI();
    }

    public void RemoveSexSkillPoints(int cost)
    {
        sexSkillPoints = Mathf.Max(0, sexSkillPoints - Mathf.Max(0, cost));
        UpdateSexSkillPointsUI();
    }

    public void UpdateAllConnections()
    {
        if (parentNodes == null) return;
        foreach (var n in parentNodes)
            n?.UpdateAllConnections();
    }

    /// <summary>Create a serializable snapshot of the tree for saving.</summary>
    public SkillTreeState CreateSaveState()
    {
        var state = new SkillTreeState
        {
            combatSkillPoints = GetCombatSkillPoints(),
            sexSkillPoints = GetSexSkillPoints()
        };

        var nodes = GetComponentsInChildren<UI_TreeNode>(true) ?? new UI_TreeNode[0];
        foreach (var n in nodes)
        {
            if (n == null || n.skillData == null) continue;
            var name = n.skillData.name;

            if (n.isUnlocked) state.unlockedSkillNames.Add(name);
            else if (n.isLocked) state.lockedSkillNames.Add(name);
        }

        return state;
    }

    /// <summary>Apply saved state (unlocked/locked nodes + point pools).</summary>
    public void ApplySaveState(SkillTreeState state)
    {
        if (state == null) return;

        if (skillManager == null)
            skillManager = FindAnyObjectByType<Player_SkillManager>();

        var nodes = GetComponentsInChildren<UI_TreeNode>(true) ?? new UI_TreeNode[0];

        var byName = new Dictionary<string, UI_TreeNode>();
        foreach (var n in nodes)
        {
            if (n == null || n.skillData == null) continue;
            var name = n.skillData.name;
            if (!byName.ContainsKey(name)) byName.Add(name, n);
        }

        // Baseline visuals/flags
        foreach (var n in nodes)
        {
            if (n == null) continue;
            n.isUnlocked = false;
            n.isLocked = false;
            n.SetLockedVisualOnly();
        }

        // Unlocked first (triggers SetSkillUpgrade)
        if (state.unlockedSkillNames != null)
        {
            foreach (var name in state.unlockedSkillNames)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (byName.TryGetValue(name, out var node) && node != null)
                    node.ForceUnlock();
            }
        }

        // Explicit locks next
        if (state.lockedSkillNames != null)
        {
            foreach (var name in state.lockedSkillNames)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (byName.TryGetValue(name, out var node) && node != null && !node.isUnlocked)
                    node.ForceLock();
            }
        }

        // Points
        SetCombatSkillPoints(state.combatSkillPoints);
        SetSexSkillPoints(state.sexSkillPoints);

        UpdateAllConnections();
    }

    #endregion

    #region Default Unlocks & Refund

    public void UnlockDefaultSkills()
    {
        var allTreeNodes = GetComponentsInChildren<UI_TreeNode>(true);
        foreach (var node in allTreeNodes)
            node?.UnlockDefaultSkills();

        UpdateSkillPointsUI();
        UpdateSexSkillPointsUI();
    }

    [ContextMenu("Reset Skill Tree")]
    public void RefundAllSkills()
    {
        var skillNodes = GetComponentsInChildren<UI_TreeNode>(true);
        foreach (var node in skillNodes)
            node?.Refund();

        UpdateAllConnections();
    }

    #endregion

    #region Confirmation Popup Flow (Unlock)

    public void ShowSkillUnlockConfirmation(UI_TreeNode node)
    {
        if (node == null || node.skillData == null) return;
        if (confirmationPopup == null || confirmationText == null) return;

        pendingSkillNode = node;
        confirmationText.text = $"Unlock <b>{node.skillData.displayName}</b>?";
        confirmationPopup.SetActive(true);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(ConfirmSkillUnlock);
        noButton.onClick.AddListener(CloseConfirmationPopup);
    }

    private void ConfirmSkillUnlock()
    {
        if (pendingSkillNode != null && pendingSkillNode.skillData != null)
        {
            var cost = pendingSkillNode.skillData.cost;
            var cat = pendingSkillNode.skillData.category;

            if (cat == SkillCategory.Combat)
            {
                if (EnoughSkillPoints(cost))
                {
                    RemoveSkillPoints(cost);
                    pendingSkillNode.ForceUnlock();
                }
            }
            else
            {
                if (EnoughSexSkillPoints(cost))
                {
                    RemoveSexSkillPoints(cost);
                    pendingSkillNode.ForceUnlock();
                }
            }

            UpdateAllConnections();
        }

        CloseConfirmationPopup();
    }

    private void CloseConfirmationPopup()
    {
        if (confirmationPopup != null)
            confirmationPopup.SetActive(false);

        pendingSkillNode = null;
    }

    #endregion

    #region Assign-to-Slot CLICK MODE (Right click)

    /// <summary>Compatibility alias; we now start pick mode instead of legacy popup.</summary>
    public void ShowAssignToSlot(UI_TreeNode node) => ShowAssignToSlotOptions(node);

    /// <summary>Begin pick mode: highlight node, show relevant hotbar, wait for slot click.</summary>
    public void ShowAssignToSlotOptions(UI_TreeNode node)
    {
        if (node == null || node.skillData == null) return;
        if (!node.isUnlocked) return;

        // End any existing pick first
        EndPickMode(false);

        _pickOwner = this;
        _pickNode = node;
        _pickSkill = node.skillData;

        // Visual highlight on node
        _pickNode.SetAssignHighlight(true);

        // Show the relevant hotbar for visual placement
        var ui = FindFirstObjectByType<UI>();
        ui?.ShowHotbarAssignPreview(_pickSkill.category);
    }

    /// <summary>Called by UI_SkillSlot when a slot is clicked.</summary>
    public static bool TryCompleteSlotPick(UI_SkillSlot clickedSlot)
    {
        if (!IsPicking || clickedSlot == null) return false;

        // Wrong bar type? refuse but stay in pick mode
        if (!clickedSlot.Accepts(_pickSkill))
        {
            clickedSlot.PulseConflict(0.2f);
            return false;
        }

        var ui = Object.FindFirstObjectByType<UI>();
        if (ui == null) return false;

        bool success = false;

        if (clickedSlot.slotCategory == UISkillCategory.Combat)
        {
            ui.inGameUI?.AssignSkillToSlot(_pickSkill, clickedSlot.slotId);
            success = true;
        }
        else
        {
            var sexUI = Object.FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
            var hotbar = sexUI != null ? sexUI.Hotbar : null;
            if (hotbar != null)
            {
                int idx = hotbar.IndexOf(clickedSlot);
                if (idx >= 0) success = hotbar.TryAssignToIndex(idx, _pickSkill);
            }
        }

        if (success)
        {
            // ✅ Stay in pick mode. Do NOT close the preview here.
            // Clear the pending skill so further clicks do nothing until the player exits or re-selects.
            _pickSkill = null;

            // (Optional) keep the highlight to show which node was assigned.
            // If you prefer to remove highlight after assignment, uncomment:
            // _pickNode?.SetAssignHighlight(false);
        }
        else
        {
            clickedSlot.PulseConflict(0.2f);
        }
        return success;
    }

    // Public wrapper (UI can call this to exit on Esc)
    public static void CancelPickModeFromUI()
    {
        EndPickMode(false);
    }

    private static void EndPickMode(bool fromAssignment)
    {
        if (!IsPicking && _pickNode == null && _pickOwner == null)
        {
            // also handle the case where _pickSkill was cleared after assignment
            var uiA = Object.FindFirstObjectByType<UI>();
            uiA?.HideHotbarAssignPreview();
            return;
        }

        // Clear node highlight
        _pickNode?.SetAssignHighlight(false);

        // Hide preview hotbar
        var ui = Object.FindFirstObjectByType<UI>();
        ui?.HideHotbarAssignPreview();

        _pickOwner = null;
        _pickNode = null;
        _pickSkill = null;
    }

    private void CloseAssignPopup() // legacy path – also end pick mode if it was active
    {
        var ui = FindFirstObjectByType<UI>();
        ui?.HideHotbarAssignPreview();
        _pendingAssignNode = null;
        EndPickMode(false);
    }

    #endregion

    #region UI Helpers

    private void UpdateSkillPointsUI()
    {
        if (skillPointsText != null)
            skillPointsText.text = skillPoints.ToString();
    }

    private void UpdateSexSkillPointsUI()
    {
        if (sexSkillPointsText != null)
            sexSkillPointsText.text = sexSkillPoints.ToString();
    }

    #endregion

    #region Panel Cancel

    public override bool HandleCancel()
    {
        // If we are in pick mode, cancel that first
        if (IsPicking || _pickNode != null || _pickOwner != null)
        {
            EndPickMode(false);
            return true;
        }

        // Close assign (legacy) popup first if open
        if (assignPopup != null && assignPopup.activeSelf)
        {
            CloseAssignPopup();
            return true;
        }

        // Then close unlock confirmation if open
        if (confirmationPopup != null && confirmationPopup.activeSelf)
        {
            CloseConfirmationPopup();
            return true;
        }

        var ui = FindFirstObjectByType<UI>();
        gameObject.SetActive(false);
        ui?.OpenMainMenuDirect();
        return true;
    }

    #endregion
}
