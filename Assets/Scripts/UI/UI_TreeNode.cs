using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_TreeNode : MonoBehaviour , IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private UI ui; // Reference to the UI manager
    private RectTransform rect; // Reference to the RectTransform for positioning

    [SerializeField] private Skill_DataSO skillData; // Reference to the skill data
    [SerializeField] private string skillName; // Name of the skill
    [SerializeField] private Image skillIcon;
    [SerializeField] private string lockedColorHex = "#9D9797";
    private Color lastColor;
    public bool isUnlocked;
    public bool isLocked;

    

    private void Awake()
    {
        ui = GetComponentInParent<UI>(); // Get the UI manager from the parent
        rect = GetComponent<RectTransform>(); // Get the RectTransform component

        UpdateIconColor(GetColorByHex(lockedColorHex));
    }

    private void Unlock()
    {
        isUnlocked = true;
        UpdateIconColor(Color.white);
    
    }

    private bool CanBeUnlocked()
    {
        if (isLocked || isUnlocked)
        {
            return false;
        }

        return true;
    }

    private void UpdateIconColor(Color color)
    {
        if (skillIcon == null)
            return; // Ensure skillIcon is assigned

        lastColor = skillIcon.color; // Store the last color for potential future use
        skillIcon.color = color; // Update the icon color

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanBeUnlocked())
            Unlock();
        else
            Debug.Log("Cannot Be Unlocked!");

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.skilltoolTip.ShowToolTip(true, rect, skillData); // Show the tooltip when hovered

        if (isUnlocked == false)
            UpdateIconColor(Color.white * .9f); // Highlight the icon when hovered
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.skilltoolTip.ShowToolTip(false, rect); // Hide the tooltip when not hovered

        if (isUnlocked == false)
            UpdateIconColor(lastColor); // Revert to the last color when not hovered
    }

    private Color GetColorByHex(string hexNumber)
    {
        ColorUtility.TryParseHtmlString(hexNumber, out Color color);
        
        return color;
    }

    private void OnValidate()
    {
        if (skillData == null)
            return; // Ensure skillData is assigned

        skillName = skillData.displayName; // Get the skill name from the skill data
        skillIcon.sprite = skillData.icon; // Get the skill icon from the skill data
        gameObject.name = "UI_TreeNode - " + skillData.displayName;
    }

}
