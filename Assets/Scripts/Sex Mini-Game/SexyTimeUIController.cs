using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SexyTimeUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panel;

    [Header("Bars")]
    [SerializeField] private Slider playerBar;
    [SerializeField] private Slider partnerBar;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerBarFillText;
    [SerializeField] private TextMeshProUGUI partnerBarFillText;
    [SerializeField] private TextMeshProUGUI playerPowerText;
    [SerializeField] private TextMeshProUGUI partnerPowerText;
    [SerializeField] private TextMeshProUGUI npcCooldownText;
    [SerializeField] private TextMeshProUGUI critText;

    [Header("Sex Hotbar (optional)")]
    [SerializeField] private SexyTimeHotbar sexHotbar;

    [Header("Single DeepBreath Slot (optional)")]
    [SerializeField] private UI_SkillSlot deepBreathSlot;

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        HideCrit();
        RefreshAffordability(null); // clears fades until we get player mana
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ---------- Bars ----------
    public void InitBars(float playerMax, float partnerMax)
    {
        if (playerBar != null)
        {
            playerBar.maxValue = playerMax;
            playerBar.value = 0f;
            if (playerBarFillText != null)
                playerBarFillText.text = $"0 / {Mathf.FloorToInt(playerMax)}";
        }

        if (partnerBar != null)
        {
            partnerBar.maxValue = partnerMax;
            partnerBar.value = 0f;
            if (partnerBarFillText != null)
                partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerMax)}";
        }
    }

    public void UpdateBars(float playerVal, float playerMax, float partnerVal, float partnerMax)
    {
        if (playerBar != null) playerBar.value = playerVal;
        if (partnerBar != null) partnerBar.value = partnerVal;

        if (playerBarFillText != null)
            playerBarFillText.text = $"{Mathf.FloorToInt(playerVal)} / {Mathf.FloorToInt(playerMax)}";

        if (partnerBarFillText != null)
            partnerBarFillText.text = $"{Mathf.FloorToInt(partnerVal)} / {Mathf.FloorToInt(partnerMax)}";
    }

    public void UpdatePower(float playerStroke, float partnerStroke)
    {
        if (playerPowerText != null) playerPowerText.text = $"+{playerStroke:F0}";
        if (partnerPowerText != null) partnerPowerText.text = $"+{partnerStroke:F0}";
    }

    public void UpdateCooldown(float remaining)
    {
        if (npcCooldownText != null) npcCooldownText.text = remaining.ToString("F1");
    }

    public void ShowCrit()
    {
        if (critText == null) return;
        critText.gameObject.SetActive(true);
        critText.text = "💥 Critical Stroke!";
        CancelInvoke(nameof(HideCrit));
        Invoke(nameof(HideCrit), 1f);
    }

    private void HideCrit()
    {
        if (critText != null) critText.gameObject.SetActive(false);
    }

    // ---------- Sex skill presentation ----------
    /// Assigns a Sex-category skill to the sex hotbar (or the single slot fallback).
    public void ShowSexSkill(Skill_DataSO sexSkill)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return;

        bool assigned = false;

        if (sexHotbar != null)
            assigned = sexHotbar.TryAssign(sexSkill);

        if (!assigned && deepBreathSlot != null)
        {
            deepBreathSlot.gameObject.SetActive(true);
            deepBreathSlot.SetupSkillSlot(sexSkill);
        }
    }

    public void HideSexSkills()
    {
        if (sexHotbar != null) sexHotbar.ClearAll();
        if (deepBreathSlot != null) deepBreathSlot.gameObject.SetActive(false);
    }

    public void StartSexSkillCooldown(float cooldownSeconds)
    {
        if (sexHotbar != null && sexHotbar.Slots != null)
        {
            foreach (var s in sexHotbar.Slots)
                if (s != null && s.HasSkill) s.StartCooldown(cooldownSeconds);
        }
        if (deepBreathSlot != null && deepBreathSlot.gameObject.activeSelf)
            deepBreathSlot.StartCooldown(cooldownSeconds);
    }

    /// Call this whenever the player's mana changes so the hotbar reflects affordability.
    public void RefreshAffordability(Entity_Mana playerMana)
    {
        if (sexHotbar != null && sexHotbar.Slots != null)
        {
            foreach (var s in sexHotbar.Slots)
                if (s != null) s.UpdateAffordability(playerMana);
        }
        if (deepBreathSlot != null) deepBreathSlot.UpdateAffordability(playerMana);
    }

    public void ResetAll()
    {
        UpdateBars(0f, PlayerBarMax, 0f, PartnerBarMax);
        UpdatePower(0f, 0f);
        UpdateCooldown(0f);
        HideCrit();
    }

    // Small helpers so logic can read/write bar values via UI controller if desired
    public float PlayerBarValue => playerBar != null ? playerBar.value : 0f;
    public float PartnerBarValue => partnerBar != null ? partnerBar.value : 0f;
    public float PlayerBarMax => playerBar != null ? playerBar.maxValue : 100f;
    public float PartnerBarMax => partnerBar != null ? partnerBar.maxValue : 100f;

    // Allow logic to push directly
    public Slider PlayerBar => playerBar;
    public Slider PartnerBar => partnerBar;

    // ---------- Compatibility wrappers (so older scripts still compile) ----------
    public void StartDeepBreathCooldown(float cooldown) => StartSexSkillCooldown(cooldown);

    public void ShowDeepBreathSlot(Skill_DataSO skillData) => ShowSexSkill(skillData);
    public void HideDeepBreathSlot() => HideSexSkills();
}
