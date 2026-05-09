using UnityEngine;

public class SexSkill_RapidStroke : Skill_Base
{
    [Header("Rapid Stroke")]
    [SerializeField] private float pinkBarIncrease = 15f;

    [Header("Audio")]
    [SerializeField] private string rapidStrokeSfx = "SexRapidStroke";

    public override void TryUseSkill()
    {
        if (!CanUseSkillCheck(out string reason))
        {
            Debug.Log($"[RapidStroke] Blocked: {reason}");
            return;
        }

        if (!Unlocked(SkillUpgradeType.RapidStroke))
        {
            Debug.Log("[RapidStroke] Skill is locked.");
            return;
        }

        SexyTimeLogic logic = SexyTimeLogic.Current;

        if (logic == null || logic.UI == null)
        {
            Debug.LogWarning("[RapidStroke] SexyTimeLogic or UI not found.");
            return;
        }

        if (!logic.CanUseSexSkill())
        {
            Debug.Log("[RapidStroke] Blocked: sex skills are disabled.");
            return;
        }

        if (!CommitUse())
            return;

        float blueValue = logic.UI.PlayerBarValue;
        float blueMax = logic.UI.PlayerBarMax;

        float oldPinkValue = logic.UI.PartnerBarValue;
        float pinkMax = logic.UI.PartnerBarMax;

        float newPinkValue = Mathf.Clamp(
            oldPinkValue + pinkBarIncrease,
            0f,
            pinkMax
        );

        logic.UI.UpdateBars(
            blueValue,
            blueMax,
            newPinkValue,
            pinkMax
        );

        logic.CheckDialogueTriggersAfterBars(blueValue, newPinkValue);

        PlayRapidStrokeSfx();

        Debug.Log($"[RapidStroke] Pink bar increased: {oldPinkValue} -> {newPinkValue}");
    }

    private void PlayRapidStrokeSfx()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("[RapidStroke] AudioManager.instance is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(rapidStrokeSfx))
            return;

        AudioManager.instance.PlayGlobalSFX(rapidStrokeSfx);
    }
}