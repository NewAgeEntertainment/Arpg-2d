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
        // store a copy so external references can’t mutate later
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
        UpdateAllConnections();    // wires lines/positions
        UpdateSkillPointsUI();

        // if saver queued a state before we were alive, apply it now (after one frame)
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

    /// <summary>
    /// Apply saved state (unlocked/locked nodes + point pools).
    /// Safe to call at runtime or one frame after Start.
    /// </summary>
    public void ApplySaveState(SkillTreeState state)
    {
        if (state == null) return;

        // Ensure manager exists (so nodes can SetSkillUpgrade)
        if (skillManager == null)
            skillManager = FindAnyObjectByType<Player_SkillManager>();

        var nodes = GetComponentsInChildren<UI_TreeNode>(true);
        if (nodes == null) nodes = new UI_TreeNode[0];

        // Build quick lookup by SO name
        var byName = new Dictionary<string, UI_TreeNode>();
        foreach (var n in nodes)
        {
            if (n == null || n.skillData == null) continue;
            var name = n.skillData.name;
            if (!byName.ContainsKey(name)) byName.Add(name, n);
        }

        // 0) Baseline: set visual locked and clear flags (no traversal)
        foreach (var n in nodes)
        {
            if (n == null) continue;
            n.isUnlocked = false;
            n.isLocked = false;
            n.SetLockedVisualOnly();
        }

        // 1) Apply unlocked list first
        if (state.unlockedSkillNames != null)
        {
            foreach (var name in state.unlockedSkillNames)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (!byName.TryGetValue(name, out var node) || node == null) continue;
                node.ForceUnlock(); // handles visuals + SetSkillUpgrade + conflict locks
            }
        }

        // 2) Apply explicit locked list to enforce locks (optional)
        if (state.lockedSkillNames != null)
        {
            foreach (var name in state.lockedSkillNames)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (!byName.TryGetValue(name, out var node) || node == null) continue;
                if (!node.isUnlocked) node.ForceLock(); // don’t relock unlocked
            }
        }

        // 3) Restore point pools
        SetCombatSkillPoints(state.combatSkillPoints);
        SetSexSkillPoints(state.sexSkillPoints);

        // 4) Connections redraw
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

            // Spend from the correct pool first
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
