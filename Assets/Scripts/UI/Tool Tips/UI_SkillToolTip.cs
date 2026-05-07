using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class UI_SkillToolTip : UI_ToolTip
{
    private UI ui;
    private UI_SkillTree skillTree;

    [Header("Tooltip Text")]
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillDescription;
    [SerializeField] private TextMeshProUGUI skillCooldown;
    [SerializeField] private TextMeshProUGUI skillRequirements;

    [Header("Fixed Tooltip Position")]
    [Tooltip("If true, the tooltip will always appear at fixedTooltipAnchor instead of near the hovered skill.")]
    [SerializeField] private bool useFixedPosition = true;

    [Tooltip("Place this RectTransform where you want the tooltip to appear.")]
    [SerializeField] private RectTransform fixedTooltipAnchor;

    [Tooltip("Optional fallback position if no fixedTooltipAnchor is assigned.")]
    [SerializeField] private Vector2 fallbackAnchoredPosition = new Vector2(450f, 0f);

    [Space]
    [SerializeField] private string metConditionHex;
    [SerializeField] private string notMetConditionHex;
    [SerializeField] private string importantInfoHex;
    [SerializeField] private Color exampleColor;
    [SerializeField] private string lockedSkillText = "You've taken a different path - this skill is now locked.";

    private Coroutine textEffectCo;

    protected override void Awake()
    {
        base.Awake();

        ui = GetComponentInParent<UI>();

        if (ui != null)
            skillTree = ui.GetComponentInChildren<UI_SkillTree>(true);
    }

    public override void ShowToolTip(bool show, RectTransform targetRect)
    {
        base.ShowToolTip(show, targetRect);

        if (show)
            ApplyFixedPosition();
    }

    public void ShowToolTip(bool show, RectTransform targetRect, Skill_DataSO skillData, UI_TreeNode node)
    {
        base.ShowToolTip(show, targetRect);

        if (!show)
            return;

        ApplyFixedPosition();

        if (skillData == null)
            return;

        if (skillName != null)
            skillName.text = skillData.displayName;

        if (skillDescription != null)
            skillDescription.text = skillData.description;

        if (skillCooldown != null)
        {
            float cooldown = 0f;

            if (skillData.upgradeData != null)
                cooldown = skillData.upgradeData.cooldown;

            skillCooldown.text = "Cooldown: " + cooldown + "s.";
        }

        if (skillRequirements == null)
            return;

        if (node == null)
        {
            skillRequirements.text = "";
            return;
        }

        string skillLockedText = GetColoredText(importantInfoHex, lockedSkillText);

        string requirements = node.isLocked
    ? skillLockedText
    : GetRequirements(node.skillData, node.neededNodes, node.conflictNodes);

        skillRequirements.text = requirements;
    }

    private void ApplyFixedPosition()
    {
        if (!useFixedPosition)
            return;

        RectTransform rect = transform as RectTransform;

        if (rect == null)
            return;

        if (fixedTooltipAnchor != null)
        {
            rect.position = fixedTooltipAnchor.position;
            rect.rotation = fixedTooltipAnchor.rotation;
            rect.localScale = fixedTooltipAnchor.localScale;
        }
        else
        {
            rect.anchoredPosition = fallbackAnchoredPosition;
        }
    }

    public void LockedSkillEffect()
    {
        StopLockedSkillEffect();
        textEffectCo = StartCoroutine(TextBlinkEffectCo(skillRequirements, .15f, 3));
    }

    public void StopLockedSkillEffect()
    {
        if (textEffectCo != null)
            StopCoroutine(textEffectCo);

        textEffectCo = StartCoroutine(TextBlinkEffectCo(skillRequirements, .15f, 0));
    }

    private IEnumerator TextBlinkEffectCo(TextMeshProUGUI text, float blinkInterval, int blinkCount)
    {
        if (text == null)
            yield break;

        for (int i = 0; i < blinkCount; i++)
        {
            text.text = GetColoredText(notMetConditionHex, lockedSkillText);
            yield return new WaitForSeconds(blinkInterval);

            text.text = GetColoredText(importantInfoHex, lockedSkillText);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private string GetRequirements(Skill_DataSO skillData, UI_TreeNode[] neededNodes, UI_TreeNode[] conflictNodes)
    {
        StringBuilder sb = new StringBuilder();

        if (skillData == null)
            return "";

        sb.AppendLine("Requirements:");

        // -------------------------------------------------------
        // LEVEL REQUIREMENT
        // -------------------------------------------------------
        Player player = FindFirstObjectByType<Player>();
        Player_Stats stats = player != null ? player.stats : null;

        int currentLevel = stats != null ? stats.CurrentLevel : 1;
        int requiredLevel = Mathf.Max(1, skillData.requiredLevel);

        bool levelMet = currentLevel >= requiredLevel;

        string levelColor = levelMet ? metConditionHex : notMetConditionHex;
        string levelText = $"- Level {requiredLevel}";

        if (!levelMet)
            levelText += $"  <size=80%>(Current: {currentLevel})</size>";

        sb.AppendLine(GetColoredText(levelColor, levelText));

        // -------------------------------------------------------
        // SKILL POINT REQUIREMENT
        // -------------------------------------------------------
        int skillCost = skillData.cost;

        bool enoughPoints = false;

        if (skillTree != null)
        {
            enoughPoints = skillData.category == SkillCategory.Combat
                ? skillTree.EnoughSkillPoints(skillCost)
                : skillTree.EnoughSexSkillPoints(skillCost);
        }

        string pointType = skillData.category == SkillCategory.Combat
            ? "combat skill point(s)"
            : "sex skill point(s)";

        string costColor = enoughPoints ? metConditionHex : notMetConditionHex;
        string costText = $"- {skillCost} {pointType}";

        sb.AppendLine(GetColoredText(costColor, costText));

        // -------------------------------------------------------
        // NEEDED SKILLS
        // -------------------------------------------------------
        if (neededNodes != null)
        {
            foreach (var node in neededNodes)
            {
                if (node == null || node.skillData == null)
                    continue;

                string nodeColor = node.isUnlocked ? metConditionHex : notMetConditionHex;
                string nodeText = $"- {node.skillData.displayName}";
                string finalNodeText = GetColoredText(nodeColor, nodeText);

                sb.AppendLine(finalNodeText);
            }
        }

        // -------------------------------------------------------
        // CONFLICT SKILLS
        // -------------------------------------------------------
        if (conflictNodes == null || conflictNodes.Length <= 0)
            return sb.ToString();

        sb.AppendLine();
        sb.AppendLine(GetColoredText(importantInfoHex, "Locks out: "));

        foreach (var node in conflictNodes)
        {
            if (node == null || node.skillData == null)
                continue;

            string nodeText = $"- {node.skillData.displayName}";
            string finalNodeText = GetColoredText(importantInfoHex, nodeText);

            sb.AppendLine(finalNodeText);
        }

        return sb.ToString();
    }

    private void OnDisable()
    {
        ShowToolTip(false, null);
    }
}