using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UI_SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UI ui;
    private Image skillIcon;
    private RectTransform rect;
    private Button button;

    private Skill_DataSO skillData;

    [Header("Slot Setup")]
    public SkillType skillType;

    [Header("UI")]
    [SerializeField] private Image cooldownImage;       // radial image (Filled, 0..1)
    [SerializeField] private string inputKeyName;       // e.g., "Q"
    [SerializeField] private TextMeshProUGUI inputKeyText;
    [SerializeField] private GameObject conflictSlot;   // flashes when unusable / empty

    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        skillIcon = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }

    private void OnValidate()
    {
        gameObject.name = "UI_SkillSlot - " + skillType.ToString();
    }

    public void SetupSkillSlot(Skill_DataSO selectedSkill)
    {
        if (ui == null) ui = GetComponentInParent<UI>();
        if (skillIcon == null) skillIcon = GetComponent<Image>();
        if (rect == null) rect = GetComponent<RectTransform>();
        if (button == null) button = GetComponent<Button>();

        skillData = selectedSkill;

        if (cooldownImage != null)
        {
            var c = Color.black; c.a = 0.6f;
            cooldownImage.color = c;
            cooldownImage.fillAmount = 0f; // ready
        }

        if (inputKeyText != null) inputKeyText.text = inputKeyName;

        if (skillIcon != null && selectedSkill != null)
            skillIcon.sprite = selectedSkill.icon;

        if (conflictSlot != null)
            conflictSlot.SetActive(false);
    }

    public Skill_DataSO Data => skillData;
    public bool HasSkill => skillData != null;
    public bool IsReady => cooldownImage == null || cooldownImage.fillAmount <= 0.001f;

    public float CooldownSeconds
        => (skillData != null && skillData.upgradeData != null)
            ? Mathf.Max(0f, skillData.upgradeData.cooldown)
            : 5f;

    public float ManaCost
        => (skillData != null && skillData.upgradeData != null)
            ? Mathf.Max(0f, skillData.upgradeData.manaCost)
            : 0f;

    public void SetKeyLabel(string label)
    {
        inputKeyName = label;
        if (inputKeyText != null) inputKeyText.text = label;
    }

    public void StartCooldown(float cooldownSeconds)
    {
        if (cooldownImage == null) return;
        StopAllCoroutines();
        cooldownImage.fillAmount = 1f;
        StartCoroutine(CooldownCo(cooldownSeconds));
    }

    public void ResetCooldown()
    {
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
    }

    public void PulseConflict(float seconds = 0.2f)
    {
        if (conflictSlot == null) return;
        StopCoroutine(nameof(PulseCo));
        StartCoroutine(PulseCo(seconds));
    }

    private IEnumerator CooldownCo(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (cooldownImage != null)
                cooldownImage.fillAmount = 1f - (t / duration);
            yield return null;
        }
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
    }

    private IEnumerator PulseCo(float seconds)
    {
        conflictSlot.SetActive(true);
        yield return new WaitForSeconds(seconds);
        conflictSlot.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skillData == null) return;
        ui.skillToolTip.ShowToolTip(true, rect, skillData, null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.skillToolTip.ShowToolTip(false, null);
    }
}
