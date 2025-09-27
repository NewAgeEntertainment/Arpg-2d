using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_TreeNode : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerClickHandler
{
    private UI ui;
    private RectTransform rect;
    private UI_SkillTree skillTree;
    private UI_TreeConnectHandler connectHandler;

    [Header("Unlock details")]
    public UI_TreeNode[] neededNodes;
    public UI_TreeNode[] conflictNodes;
    public bool isUnlocked;
    public bool isLocked;

    [Header("Skill details")]
    public Skill_DataSO skillData;
    [SerializeField] private string skillName;
    [SerializeField] private Image skillIcon;
    [SerializeField] private int skillCost;

    [Header("Colors")]
    [SerializeField] private string lockedColorHex = "#9F9797";
    private Color lastColor;

    private void EnsureWired()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (skillTree == null) skillTree = GetComponentInParent<UI_SkillTree>(true);
        if (ui == null) ui = GetComponentInParent<UI>();
        if (connectHandler == null) connectHandler = GetComponent<UI_TreeConnectHandler>();
        // skillIcon is serialized
    }

    private Color LockedColor()
    {
        ColorUtility.TryParseHtmlString(lockedColorHex, out var c);
        return c;
    }

    private void Start()
    {
        // Start with locked visuals if not unlocked yet
        if (isUnlocked == false)
            UpdateIconColor(LockedColor());

        UnlockDefaultSkills();
    }

    public void UnlockDefaultSkills()
    {
        EnsureWired();

        if (skillData != null && skillData.unlockedByDefault && !isUnlocked)
        {
            // default unlock should not spend points
            isUnlocked = true;
            isLocked = false;
            UpdateIconColor(Color.white);
            connectHandler?.UnlockConnectionImage(true);

            // Apply the skill’s upgrade to gameplay/UI
            if (skillTree != null && skillTree.skillManager != null)
            {
                var skill = skillTree.skillManager.GetSkillByType(skillData.skillType);
                if (skill != null) skill.SetSkillUpgrade(skillData);
            }
        }
    }

    public void Refund()
    {
        EnsureWired();

        if (isUnlocked == false || (skillData != null && skillData.unlockedByDefault))
            return;

        isUnlocked = false;
        isLocked = false;

        UpdateIconColor(LockedColor());
        connectHandler?.UnlockConnectionImage(false);

        if (skillData != null)
        {
            if (skillData.category == SkillCategory.Combat)
                skillTree?.AddSkillPoints(skillData.cost);
            else
                skillTree?.AddSexSkillPoints(skillData.cost);
        }
        // If you need to remove effects, add here.
    }

    private bool CanBeUnlocked()
    {
        EnsureWired();

        if (isLocked || isUnlocked || skillData == null || skillTree == null)
            return false;

        bool enoughPoints = (skillData.category == SkillCategory.Combat)
            ? skillTree.EnoughSkillPoints(skillData.cost)
            : skillTree.EnoughSexSkillPoints(skillData.cost);

        if (!enoughPoints) return false;

        if (neededNodes != null)
        {
            foreach (var node in neededNodes)
            {
                if (node != null && node.isUnlocked == false)
                    return false;
            }
        }

        if (conflictNodes != null)
        {
            foreach (var node in conflictNodes)
            {
                if (node != null && node.isUnlocked)
                    return false;
            }
        }

        return true;
    }

    private void LockConflictNodes()
    {
        if (conflictNodes == null) return;

        foreach (var node in conflictNodes)
        {
            if (node == null) continue;
            node.ForceLock();
            node.LockChildNodes();
        }
    }

    public void LockChildNodes()
    {
        EnsureWired();

        isLocked = true;

        var children = connectHandler != null ? connectHandler.GetChildNodes() : null;
        if (children == null) return;

        foreach (var child in children)
        {
            if (child == null) continue;
            child.ForceLock();
            child.LockChildNodes();
        }
    }

    private void UpdateIconColor(Color color)
    {
        lastColor = color;
        if (skillIcon != null) skillIcon.color = color;
    }

    // LEFT CLICK = unlock flow only
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (CanBeUnlocked())
        {
            skillTree?.ShowSkillUnlockConfirmation(this);
        }
        else if (isLocked)
        {
            if (ui != null && ui.skillToolTip != null)
                ui.skillToolTip.LockedSkillEffect();
        }
    }

    // RIGHT CLICK = open "Assign to Slot" popup (only if unlocked)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;

        EnsureWired();

        if (skillData == null)
        {
            ui?.skillToolTip?.LockedSkillEffect();
            return;
        }

        if (!isUnlocked)
        {
            ui?.skillToolTip?.LockedSkillEffect();
            return;
        }

        skillTree?.ShowAssignToSlotOptions(this);
        eventData.Use();
    }

    public void ForceUnlock()
    {
        EnsureWired();

        if (skillData == null) return;
        if (isUnlocked) return;

        isUnlocked = true;
        isLocked = false;

        UpdateIconColor(Color.white);
        connectHandler?.UnlockConnectionImage(true);

        LockConflictNodes();

        if (skillTree != null && skillTree.skillManager != null)
        {
            var skill = skillTree.skillManager.GetSkillByType(skillData.skillType);
            if (skill != null)
            {
                skill.SetSkillUpgrade(skillData);
            }
        }
    }

    public void ForceLock()
    {
        EnsureWired();

        isUnlocked = false;
        isLocked = true;

        if (skillIcon != null) skillIcon.color = LockedColor();
        connectHandler?.UnlockConnectionImage(false);
    }

    /// Visual-only lock used during save-state baseline.
    public void SetLockedVisualOnly()
    {
        EnsureWired();
        if (skillIcon != null) skillIcon.color = LockedColor();
        connectHandler?.UnlockConnectionImage(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureWired();

        if (ui != null && ui.skillToolTip != null)
            ui.skillToolTip.ShowToolTip(true, rect, skillData, this);

        if (isUnlocked || isLocked) return;

        ToggleNodeHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EnsureWired();

        if (ui != null && ui.skillToolTip != null)
        {
            ui.skillToolTip.ShowToolTip(false, rect);
            ui.skillToolTip.StopLockedSkillEffect();
        }

        if (isUnlocked || isLocked) return;

        ToggleNodeHighlight(false);
    }

    private void ToggleNodeHighlight(bool highlight)
    {
        var highlightColor = Color.white * 0.9f;
        highlightColor.a = 1f;

        var colorToApply = highlight ? highlightColor : (skillIcon != null ? skillIcon.color : Color.white);
        UpdateIconColor(colorToApply);
    }

    private void OnDisable()
    {
        if (isLocked) UpdateIconColor(LockedColor());
        if (isUnlocked) UpdateIconColor(Color.white);
    }

    private void OnValidate()
    {
        if (skillData != null)
        {
            skillName = skillData.displayName;
            if (skillIcon != null) skillIcon.sprite = skillData.icon;
            skillCost = skillData.cost;
            gameObject.name = "UI_TreeNode - " + skillData.displayName;
        }
    }
}
