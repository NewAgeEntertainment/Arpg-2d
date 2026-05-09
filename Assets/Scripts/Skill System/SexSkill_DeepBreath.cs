using UnityEngine;

public class SexSkill_DeepBreath : Skill_Base
{
    [SerializeField] private SexyTimeLogic sexyTimeLogic;

    [Header("Deep Breath")]
    [SerializeField] private float blueBarDecreaseAmount = 10f;

    [Header("Audio")]
    [SerializeField] private string deepBreathSfx = "SexDeepBreath";

    protected override void Awake()
    {
        base.Awake();

        if (sexyTimeLogic == null)
            sexyTimeLogic = FindFirstObjectByType<SexyTimeLogic>(FindObjectsInactive.Include);
    }

    private bool ResolveSexyTimeLogic()
    {
        if (SexyTimeLogic.Current != null)
        {
            sexyTimeLogic = SexyTimeLogic.Current;
            return true;
        }

        sexyTimeLogic = FindFirstObjectByType<SexyTimeLogic>(FindObjectsInactive.Include);

        return sexyTimeLogic != null;
    }

    public override void TryUseSkill()
    {
        if (!ResolveSexyTimeLogic())
        {
            Debug.LogWarning("[DeepBreath] Blocked: SexyTimeLogic not found.");
            return;
        }

        if (!sexyTimeLogic.CanUseSexSkill())
            return;

        if (!IsUnlocked() && !Unlocked(SkillUpgradeType.DeepBreath))
            return;

        SexyTimeUIController ui = sexyTimeLogic.UI;

        if (ui == null)
        {
            Debug.LogWarning("[DeepBreath] Blocked: SexyTime UI missing.");
            return;
        }

        float oldBlue = ui.PlayerBarValue;
        float oldPink = ui.PartnerBarValue;

        float newBlue = Mathf.Max(
            0f,
            oldBlue - blueBarDecreaseAmount
        );

        ui.UpdateBars(
            newBlue,
            ui.PlayerBarMax,
            oldPink,
            ui.PartnerBarMax
        );

        sexyTimeLogic.CheckDialogueTriggersAfterBars(newBlue, oldPink);

        PlayDeepBreathSfx();

        SetSkillOnCooldown();

        Debug.Log($"[DeepBreath] Used. Blue {oldBlue} -> {newBlue}");
    }

    private void PlayDeepBreathSfx()
    {
        if (AudioManager.instance == null)
            return;

        if (string.IsNullOrWhiteSpace(deepBreathSfx))
            return;

        AudioManager.instance.PlayGlobalSFX(deepBreathSfx);
    }

    public override void SetSkillUpgrade(Skill_DataSO skillData)
    {
        if (skillData == null)
        {
            Debug.LogError("[DeepBreath] SetSkillUpgrade called with NULL skillData.");
            return;
        }

        base.SetSkillUpgrade(skillData);

        if (skillData.upgradeData != null &&
            skillData.upgradeData.upgradeType == SkillUpgradeType.DeepBreath)
        {
            Debug.Log("[DeepBreath] Unlocked via upgrade data.");
        }
        else
        {
            Debug.LogWarning("[DeepBreath] upgradeData is null or not DeepBreath.");
        }
    }

    public void Unlock()
    {
        ForceUnlock(true);
        upgradeType = SkillUpgradeType.DeepBreath;
        SaveRuntimeSnapshot();

        Debug.Log("✅ SexSkill_DeepBreath force-unlocked as DeepBreath.");
    }
}