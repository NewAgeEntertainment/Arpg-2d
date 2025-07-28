using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SexyTimeUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panel;   // SexMiniGamePanel

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

    [SerializeField] private UI_SkillSlot deepBreathSlot;

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        HideCrit();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

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

    // Small helpers so logic can read/write bar values via UI controller if desired
    public float PlayerBarValue => playerBar != null ? playerBar.value : 0f;
    public float PartnerBarValue => partnerBar != null ? partnerBar.value : 0f;
    public float PlayerBarMax => playerBar != null ? playerBar.maxValue : 100f;
    public float PartnerBarMax => partnerBar != null ? partnerBar.maxValue : 100f;

    // Allow logic to push directly
    public Slider PlayerBar => playerBar;
    public Slider PartnerBar => partnerBar;

    public void ShowDeepBreathSlot(Skill_DataSO skillData)
    {
        if (deepBreathSlot != null)
        {
            deepBreathSlot.gameObject.SetActive(true);
            deepBreathSlot.SetupSkillSlot(skillData);
        }
    }

    public void HideDeepBreathSlot()
    {
        if (deepBreathSlot != null)
            deepBreathSlot.gameObject.SetActive(false);
    }

    public void StartDeepBreathCooldown(float cooldown)
    {
        if (deepBreathSlot != null)
            deepBreathSlot.StartCooldown(cooldown);
    }
}
