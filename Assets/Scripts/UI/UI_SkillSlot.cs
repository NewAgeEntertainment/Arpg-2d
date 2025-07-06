using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UI_SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // things this game object needs to function
    private UI ui;
    private Image skillIcon;
    private RectTransform rect;
    private Button button;

    private Skill_DataSO skillData;

    public SkillType skillType;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private string inputKeyName;
    [SerializeField] private TextMeshProUGUI inputKeyText;
    [SerializeField] private GameObject conflictSlot;

    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        skillIcon = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }

    private void OnValidate()
    {
        gameObject.name = "UI_SkillSlot - " + skillType.ToString(); // Set the name of the GameObject based on the input key name
    }

    public void SetupSkillSlot(Skill_DataSO selectedSkill)
    {
        this.skillData = selectedSkill;

        Color color = Color.black; color.a = .6f;
        cooldownImage.color = color; // Set the cooldown image color to black with 60% opacity

        inputKeyText.text = inputKeyName; // Set the input key text
        skillIcon.sprite = selectedSkill.icon;

        if(conflictSlot != null)
            conflictSlot.SetActive(false); // Hide the conflict slot if it exists

    }

    public void StartCooldown(float cooldown)
    {
        cooldownImage.fillAmount = 1;
        StartCoroutine(CooldownCo(cooldown)); // Start the cooldown coroutine
    }

    public void ResetCooldown() => cooldownImage.fillAmount = 0f; // Reset the cooldown image fill amount to 0

    private IEnumerator CooldownCo(float duration)
    {
        float timePassed = 0f;

        while (timePassed < duration)
        {
            timePassed = timePassed + Time.deltaTime;
            cooldownImage.fillAmount = 1 - (timePassed / duration); // Update the cooldown image fill amount
            yield return null; // Wait for the next frame
        }

        cooldownImage.fillAmount = 0f; // Reset the cooldown image fill amount
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skillData == null)
            return;

        ui.skillToolTip.ShowToolTip(true, rect, skillData, null); // Show the skill tooltip with the skill data
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.skillToolTip.ShowToolTip(false, null);
    }
}
