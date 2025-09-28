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

    // ===== Assign-to-slot popup =====
    [Header("Assign To Slot Popup")]
    [SerializeField] private GameObject assignPopup;
    [SerializeField] private TextMeshProUGUI assignTitleText;
    [SerializeField] private Button assignSlotAButton;
    [SerializeField] private Button assignSlotBButton;
    [SerializeField] private Button assignSlotCButton;
    [SerializeField] private Button assignSlotDButton;
    [SerializeField] private Button assignCancelButton;
    private UI_TreeNode _pendingAssignNode;

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
            HandleCancel();
    }

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

    #region Assign-to-Slot Popup (Right click)

    /// <summary>Compatibility alias if something calls the old name.</summary>
    public void ShowAssignToSlot(UI_TreeNode node) => ShowAssignToSlotOptions(node);

    /// <summary>Open the "Assign to Slot" popup for an unlocked node.</summary>
    public void ShowAssignToSlotOptions(UI_TreeNode node)
    {
        if (node == null || node.skillData == null) return;
        if (!node.isUnlocked) return;
        if (assignPopup == null) return;

        _pendingAssignNode = node;

        if (assignTitleText != null)
            assignTitleText.text = node.skillData.category == SkillCategory.Sex
                ? $"Assign <b>{node.skillData.displayName}</b> to <b>Sex</b> slot:"
                : $"Assign <b>{node.skillData.displayName}</b> to slot:";

        // wire buttons
        assignSlotAButton?.onClick.RemoveAllListeners();
        assignSlotBButton?.onClick.RemoveAllListeners();
        assignSlotCButton?.onClick.RemoveAllListeners();
        assignSlotDButton?.onClick.RemoveAllListeners();
        assignCancelButton?.onClick.RemoveAllListeners();

        if (node.skillData.category == SkillCategory.Sex)
        {
            // Map A/B/C/D to Sex indices 0/1/2/3
            if (assignSlotAButton != null) assignSlotAButton.onClick.AddListener(() => AssignSelectedSexSkillToSexIndex(0));
            if (assignSlotBButton != null) assignSlotBButton.onClick.AddListener(() => AssignSelectedSexSkillToSexIndex(1));
            if (assignSlotCButton != null) assignSlotCButton.onClick.AddListener(() => AssignSelectedSexSkillToSexIndex(2));
            if (assignSlotDButton != null) assignSlotDButton.onClick.AddListener(() => AssignSelectedSexSkillToSexIndex(3));
        }
        else
        {
            if (assignSlotAButton != null) assignSlotAButton.onClick.AddListener(() => AssignSelectedSkillToSlot(UISkillSlotId.SlotA));
            if (assignSlotBButton != null) assignSlotBButton.onClick.AddListener(() => AssignSelectedSkillToSlot(UISkillSlotId.SlotB));
            if (assignSlotCButton != null) assignSlotCButton.onClick.AddListener(() => AssignSelectedSkillToSlot(UISkillSlotId.SlotC));
            if (assignSlotDButton != null) assignSlotDButton.onClick.AddListener(() => AssignSelectedSkillToSlot(UISkillSlotId.SlotD));
        }

        if (assignCancelButton != null) assignCancelButton.onClick.AddListener(CloseAssignPopup);

        assignPopup.SetActive(true);
    }

    private void AssignSelectedSkillToSlot(UISkillSlotId slot)
    {
        if (_pendingAssignNode == null || _pendingAssignNode.skillData == null)
        {
            CloseAssignPopup();
            return;
        }

        var ui = FindFirstObjectByType<UI>();
        ui?.inGameUI?.AssignSkillToSlot(_pendingAssignNode.skillData, slot);
        CloseAssignPopup();
    }

    // NEW: Sex skill assignment to a chosen Sex hotbar index
    private void AssignSelectedSexSkillToSexIndex(int index)
    {
        if (_pendingAssignNode == null || _pendingAssignNode.skillData == null)
        {
            CloseAssignPopup();
            return;
        }

        var sexSkill = _pendingAssignNode.skillData;
        if (sexSkill.category != SkillCategory.Sex)
        {
            CloseAssignPopup();
            return;
        }

        // Try to assign immediately if SexyTime UI is around; otherwise persist for later
        var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
        bool ok = false;

        if (sexUI != null)
        {
            ok = sexUI.AssignSexSkillToIndex(sexSkill, index);
            if (!ok) Debug.LogWarning($"[SkillTree] Failed to assign '{sexSkill.displayName}' to Sex slot {index}.");
        }
        else
        {
            SexyTimeUIController.SetPersistentSexSkillAtIndex(index, sexSkill);
            ok = true;
        }

        CloseAssignPopup();
    }

    private void CloseAssignPopup()
    {
        if (assignPopup != null) assignPopup.SetActive(false);
        _pendingAssignNode = null;
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
        // Close assign popup first if open
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
