using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillTree : UI_Panel
{
    [SerializeField] private int skillPoints;
    [SerializeField] private int sexSkillPoints;
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private TextMeshProUGUI sexSkillPointsText;
    [SerializeField] private UI_TreeConnectHandler[] parentNodes;

    public Player_SkillManager skillManager { get; private set; }

    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    

    private UI_TreeNode pendingSkillNode;

    private void Start()
    {
        UpdateAllConnections();
        UpdateSkillPointsUI();
        UpdateSexSkillPointsUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCancel();
        }
    }

    private void UpdateSkillPointsUI()
    {
        skillPointsText.text = skillPoints.ToString();
    }

    private void UpdateSexSkillPointsUI()
    {
        sexSkillPointsText.text = sexSkillPoints.ToString();
    }

    public void UnlockDefaultSkills()
    {
        var allTreeNodes = GetComponentsInChildren<UI_TreeNode>(true);
        skillManager = FindAnyObjectByType<Player_SkillManager>();

        foreach (var node in allTreeNodes)
        {
            node.UnlockDefaultSkills();
        }
    }

    [ContextMenu("Reset Skill Tree")]
    public void RefundAllSkills()
    {
        var skillNodes = GetComponentsInChildren<UI_TreeNode>();
        foreach (var node in skillNodes)
            node.Refund();
    }

    public bool EnoughSkillPoints(int cost) => skillPoints >= cost;

    public void RemoveSkillPoints(int cost)
    {
        skillPoints -= cost;
        UpdateSkillPointsUI();
    }

    public void AddSkillPoints(int points)
    {
        skillPoints += points;
        UpdateSkillPointsUI();
    }

    public bool EnoughSexSkillPoints(int cost) => sexSkillPoints >= cost;

    public void RemoveSexSkillPoints(int cost)
    {
        sexSkillPoints -= cost;
        UpdateSexSkillPointsUI();
    }

    public void AddSexSkillPoints(int points)
    {
        sexSkillPoints += points;
        UpdateSexSkillPointsUI();
    }

    [ContextMenu("Update All Connections")]
    public void UpdateAllConnections()
    {
        foreach (var node in parentNodes)
        {
            node.UpdateAllConnections();
        }
    }

    // ----- Confirmation Popup Logic -----
    public void ShowSkillUnlockConfirmation(UI_TreeNode node)
    {
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
        if (pendingSkillNode != null)
        {
            pendingSkillNode.ForceUnlock();
        }
        CloseConfirmationPopup();
    }

    private void CloseConfirmationPopup()
    {
        confirmationPopup?.SetActive(false);
        pendingSkillNode = null;
    }

    public override bool HandleCancel()
    {
        
        if (confirmationPopup != null && confirmationPopup.activeSelf)
        {
            CloseConfirmationPopup();
            return true;
        }

        var ui = FindObjectOfType<UI>();
        gameObject.SetActive(false);
        ui?.OpenMainMenuDirect();
        return true;
    }


}
