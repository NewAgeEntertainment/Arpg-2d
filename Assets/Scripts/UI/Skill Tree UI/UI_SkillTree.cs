using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SkillTreePanelType
{
    Combat,
    Sex
}

public class UI_SkillTree : UI_Panel
{
    [Header("Point Pools")]
    [SerializeField] private int skillPoints;         // Combat points
    [SerializeField] private int sexSkillPoints;      // Sex points

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private TextMeshProUGUI sexSkillPointsText;

    [Header("Skill Tree Panels")]
    [SerializeField] private GameObject combatSkillPanel;
    [SerializeField] private GameObject sexSkillPanel;

    [SerializeField] private Button combatTabButton;
    [SerializeField] private Button sexTabButton;

    [SerializeField] private SkillTreePanelType startingPanel = SkillTreePanelType.Combat;
    private SkillTreePanelType currentPanel;

    [Header("Graph Roots - Combat")]
    [SerializeField] private UI_TreeConnectHandler[] combatParentNodes;

    [Header("Graph Roots - Sex")]
    [SerializeField] private UI_TreeConnectHandler[] sexParentNodes;

    [Header("Graph Roots - Legacy / Optional")]
    [Tooltip("Optional old connection roots. You can leave this empty if using Combat/Sex roots above.")]
    [SerializeField] private UI_TreeConnectHandler[] parentNodes;

    public Player_SkillManager skillManager { get; private set; }

    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [SerializeField] private RectTransform fixedTooltipAnchor;
    [SerializeField] private bool useFixedPosition = true;

    private UI_TreeNode pendingSkillNode;

    // ===== Assign-to-slot popup =====
    [Header("Assign To Slot Popup (legacy)")]
    [SerializeField] private GameObject assignPopup;
    [SerializeField] private TextMeshProUGUI assignTitleText;
    [SerializeField] private Button assignSlotAButton;
    [SerializeField] private Button assignSlotBButton;
    [SerializeField] private Button assignSlotCButton;
    [SerializeField] private Button assignSlotDButton;
    [SerializeField] private Button assignCancelButton;

    private UI_TreeNode _pendingAssignNode;

    // ===== Click-to-assign pick mode =====
    private static UI_SkillTree _pickOwner;
    private static UI_TreeNode _pickNode;
    private static Skill_DataSO _pickSkill;

    public static bool IsPicking => _pickOwner != null && _pickNode != null && _pickSkill != null;
    public static Skill_DataSO PendingPickSkill => _pickSkill;

    [Header("Assign Pick Visuals")]
    [SerializeField] private Color nodePickColor = new Color(1f, 0.9f, 0.4f, 1f);

    #region State Type

    [System.Serializable]
    public class SkillTreeState
    {
        public List<string> unlockedSkillNames = new();
        public List<string> lockedSkillNames = new();
        public int combatSkillPoints;
        public int sexSkillPoints;
    }

    #endregion

    #region Pending State

    private static SkillTreeState _pendingState;

    public static void SetPendingState(SkillTreeState s)
    {
        if (s == null)
        {
            _pendingState = null;
            return;
        }

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
        UnlockDefaultSkills();

        ShowPanel(startingPanel);

        UpdateAllConnections();
        UpdateSkillPointsUI();
        UpdateSexSkillPointsUI();

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
            var ui = UI.Instance;

            if (ui != null && ui.IsAssignPreviewActive)
            {
                ui.RequestExitAssignPreview();
                return;
            }

            HandleCancel();
        }
    }

    public bool IsOpen => gameObject.activeInHierarchy;

    #region Panel Switching

    public void ShowPanel(SkillTreePanelType panelType)
    {
        currentPanel = panelType;

        bool showCombat = panelType == SkillTreePanelType.Combat;
        bool showSex = panelType == SkillTreePanelType.Sex;

        if (combatSkillPanel != null)
            combatSkillPanel.SetActive(showCombat);

        if (sexSkillPanel != null)
            sexSkillPanel.SetActive(showSex);

        if (combatTabButton != null)
            combatTabButton.interactable = !showCombat;

        if (sexTabButton != null)
            sexTabButton.interactable = !showSex;

        UpdateSkillPointsUI();
        UpdateSexSkillPointsUI();
        UpdateAllConnections();
    }

    public void ShowCombatSkillPanel()
    {
        ShowPanel(SkillTreePanelType.Combat);
    }

    public void ShowSexSkillPanel()
    {
        ShowPanel(SkillTreePanelType.Sex);
    }

    public SkillTreePanelType GetCurrentPanel()
    {
        return currentPanel;
    }

    #endregion

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
        if (combatParentNodes != null)
        {
            foreach (var n in combatParentNodes)
                n?.UpdateAllConnections();
        }

        if (sexParentNodes != null)
        {
            foreach (var n in sexParentNodes)
                n?.UpdateAllConnections();
        }

        if (parentNodes != null)
        {
            foreach (var n in parentNodes)
                n?.UpdateAllConnections();
        }
    }

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
            if (n == null || n.skillData == null)
                continue;

            string skillName = n.skillData.name;

            if (n.isUnlocked)
                state.unlockedSkillNames.Add(skillName);
            else if (n.isLocked)
                state.lockedSkillNames.Add(skillName);
        }

        return state;
    }

    public void ApplySaveState(SkillTreeState state)
    {
        if (state == null)
            return;

        if (skillManager == null)
            skillManager = FindAnyObjectByType<Player_SkillManager>();

        var nodes = GetComponentsInChildren<UI_TreeNode>(true) ?? new UI_TreeNode[0];

        var byName = new Dictionary<string, UI_TreeNode>();

        foreach (var n in nodes)
        {
            if (n == null || n.skillData == null)
                continue;

            string skillName = n.skillData.name;

            if (!byName.ContainsKey(skillName))
                byName.Add(skillName, n);
        }

        foreach (var n in nodes)
        {
            if (n == null)
                continue;

            n.isUnlocked = false;
            n.isLocked = false;
            n.SetLockedVisualOnly();
        }

        if (state.unlockedSkillNames != null)
        {
            foreach (string skillName in state.unlockedSkillNames)
            {
                if (string.IsNullOrEmpty(skillName))
                    continue;

                if (byName.TryGetValue(skillName, out var node) && node != null)
                    node.ForceUnlock();
            }
        }

        if (state.lockedSkillNames != null)
        {
            foreach (string skillName in state.lockedSkillNames)
            {
                if (string.IsNullOrEmpty(skillName))
                    continue;

                if (byName.TryGetValue(skillName, out var node) && node != null && !node.isUnlocked)
                    node.ForceLock();
            }
        }

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

    #region Confirmation Popup Flow

    public void ShowSkillUnlockConfirmation(UI_TreeNode node)
    {
        if (node == null || node.skillData == null)
            return;

        if (confirmationPopup == null || confirmationText == null)
            return;

        pendingSkillNode = node;

        Skill_DataSO skillData = node.skillData;

        Player player = FindFirstObjectByType<Player>();
        Player_Stats stats = player != null ? player.stats : null;

        string levelText = "";

        if (stats != null && stats.CurrentLevel < skillData.requiredLevel)
        {
            levelText =
                $"\n<color=#ff7070>Requires Level {skillData.requiredLevel}</color>" +
                $"\n<color=#ff7070>Your Level: {stats.CurrentLevel}</color>";
        }

        confirmationText.text =
            $"Unlock <b>{skillData.displayName}</b>?" +
            $"\nCost: {skillData.cost}" +
            levelText;

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
            Skill_DataSO skillData = pendingSkillNode.skillData;

            Player player = FindFirstObjectByType<Player>();
            Player_Stats stats = player != null ? player.stats : null;

            if (stats == null)
            {
                Debug.LogWarning("[SkillTree] Cannot unlock skill. Player_Stats not found.");
                CloseConfirmationPopup();
                return;
            }

            if (stats.CurrentLevel < skillData.requiredLevel)
            {
                Debug.Log(
                    $"[SkillTree] Cannot unlock {skillData.displayName}. " +
                    $"Requires Level {skillData.requiredLevel}. Current Level: {stats.CurrentLevel}."
                );

                CloseConfirmationPopup();
                return;
            }

            int cost = skillData.cost;
            SkillCategory cat = skillData.category;

            if (cat == SkillCategory.Combat)
            {
                if (EnoughSkillPoints(cost))
                {
                    RemoveSkillPoints(cost);
                    pendingSkillNode.ForceUnlock();
                }
                else
                {
                    Debug.Log($"[SkillTree] Not enough combat skill points to unlock {skillData.displayName}.");
                }
            }
            else
            {
                if (EnoughSexSkillPoints(cost))
                {
                    RemoveSexSkillPoints(cost);
                    pendingSkillNode.ForceUnlock();
                }
                else
                {
                    Debug.Log($"[SkillTree] Not enough sex skill points to unlock {skillData.displayName}.");
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

    #region Assign-to-Slot CLICK MODE

    public void ShowAssignToSlot(UI_TreeNode node)
    {
        ShowAssignToSlotOptions(node);
    }

    public void ShowAssignToSlotOptions(UI_TreeNode node)
    {
        if (node == null || node.skillData == null)
            return;

        if (!node.isUnlocked)
            return;

        EndPickMode(false);

        _pickOwner = this;
        _pickNode = node;
        _pickSkill = node.skillData;

        _pickNode.SetAssignHighlight(true);

        var ui = FindFirstObjectByType<UI>();
        ui?.ShowHotbarAssignPreview(_pickSkill.category);
    }

    public static bool TryCompleteSlotPick(UI_SkillSlot clickedSlot)
    {
        if (!IsPicking || clickedSlot == null)
            return false;

        if (!clickedSlot.Accepts(_pickSkill))
        {
            clickedSlot.PulseConflict(0.2f);
            return false;
        }

        var ui = Object.FindFirstObjectByType<UI>();

        if (ui == null)
            return false;

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

                if (idx >= 0)
                    success = hotbar.TryAssignToIndex(idx, _pickSkill);
            }
        }

        if (success)
        {
            _pickSkill = null;
        }
        else
        {
            clickedSlot.PulseConflict(0.2f);
        }

        return success;
    }

    public static void CancelPickModeFromUI()
    {
        EndPickMode(false);
    }

    private static void EndPickMode(bool fromAssignment)
    {
        if (!IsPicking && _pickNode == null && _pickOwner == null)
        {
            var uiA = Object.FindFirstObjectByType<UI>();
            uiA?.HideHotbarAssignPreview();
            return;
        }

        _pickNode?.SetAssignHighlight(false);

        var ui = Object.FindFirstObjectByType<UI>();
        ui?.HideHotbarAssignPreview();

        _pickOwner = null;
        _pickNode = null;
        _pickSkill = null;
    }

    private void CloseAssignPopup()
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
        if (IsPicking || _pickNode != null || _pickOwner != null)
        {
            EndPickMode(false);
            return true;
        }

        if (assignPopup != null && assignPopup.activeSelf)
        {
            CloseAssignPopup();
            return true;
        }

        if (confirmationPopup != null && confirmationPopup.activeSelf)
        {
            CloseConfirmationPopup();
            return true;
        }

        if (UI.Instance != null)
            UI.Instance.CloseSkillTree();
        else
            gameObject.SetActive(false);

        return true;
    }

    #endregion
}