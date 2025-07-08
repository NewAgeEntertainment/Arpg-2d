using TMPro;
using UnityEngine;

public class UI_SkillTree : MonoBehaviour
{
    [SerializeField] private int skillPoints;
    [SerializeField] private int sexSkillPoints;
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private TextMeshProUGUI sexSkillPointsText;
    [SerializeField] private UI_TreeConnectHandler[] parentNodes;
    private UI_TreeNode[] allTreeNodes;
    public Player_SkillManager skillManager { get; private set; }
    

    private void Start()
    {
        UpdateAllConnections();
        UpdateSkillPointsUI();
        UpdateSexSkillPointsUI();
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
        allTreeNodes = GetComponentsInChildren<UI_TreeNode>(true);
        skillManager = FindAnyObjectByType<Player_SkillManager>();

        foreach (var node in allTreeNodes)
        {
            node.UnlockDefaultSkills();
        }
    }

    [ContextMenu("Reset Skill Tree")]
    public void RefundAllSkills()
    {
        UI_TreeNode[] skillNodes = GetComponentsInChildren<UI_TreeNode>();

        foreach (var node in skillNodes)
            node.Refund();
    }

    public bool EnoughSkillPoints(int cost) => skillPoints >= cost;
    public void RemoveSkillPoints(int cost)
    {
        skillPoints = skillPoints - cost;
        UpdateSkillPointsUI();
    }

    public void AddSkillPoints(int points) 
    { 
        skillPoints = skillPoints + points;
        UpdateSkillPointsUI();
    }
    public bool EnoughSexSkillPoints(int cost) => sexSkillPoints >= cost;

    public void RemoveSexSkillPoints(int cost)
    {
        sexSkillPoints = sexSkillPoints - cost;
        UpdateSexSkillPointsUI();
    }
    public void AddSexSkillPoints(int points) 
    { 
        sexSkillPoints = sexSkillPoints + points;
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
}