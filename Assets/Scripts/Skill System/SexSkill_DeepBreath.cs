using UnityEngine;

public class SexSkill_DeepBreath : Skill_Base
{
    [SerializeField] private SexyTimeLogic sexyTimeLogic;
    [SerializeField] private bool unlocked = false;

    protected override void Awake()
    {
        base.Awake();

        if (sexyTimeLogic == null)
            sexyTimeLogic = FindFirstObjectByType<SexyTimeLogic>(FindObjectsInactive.Include);
    }

    private bool ResolveSexyTimeLogic()
    {
        if (sexyTimeLogic != null)
            return true;

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

        if (!SexyTimeLogic.isSexyTimeGoingOn)
        {
            Debug.Log("[DeepBreath] Blocked: SexyTime is not active.");
            return;
        }

        if (!unlocked && !Unlocked(SkillUpgradeType.DeepBreath))
        {
            Debug.Log("[DeepBreath] Blocked: skill is locked.");
            return;
        }

        if (Time.time < sexyTimeLogic.deepBreatheTimestamp)
        {
            Debug.Log("[DeepBreath] Blocked: on SexyTime cooldown.");
            return;
        }

        var ui = sexyTimeLogic.UI;

        if (ui == null)
        {
            Debug.LogWarning("[DeepBreath] Blocked: SexyTime UI missing.");
            return;
        }

        float oldPlayerValue = ui.PlayerBarValue;

        float newPlayerValue = Mathf.Max(
            0f,
            ui.PlayerBarValue - sexyTimeLogic.DeepBreathDepleteAmount
        );

        ui.UpdateBars(
            newPlayerValue,
            ui.PlayerBarMax,
            ui.PartnerBarValue,
            ui.PartnerBarMax
        );

        sexyTimeLogic.deepBreatheTimestamp = Time.time + sexyTimeLogic.deepBreatheCooldown;
        ui.StartDeepBreathCooldown(sexyTimeLogic.deepBreatheCooldown);

        Debug.Log($"[DeepBreath] Used. Blue bar: {oldPlayerValue} -> {newPlayerValue}");
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
            Unlock();
            Debug.Log("[DeepBreath] Unlocked via upgrade data.");
        }
        else
        {
            Debug.LogWarning("[DeepBreath] upgradeData is null or not DeepBreath.");
        }
    }

    public void Unlock()
    {
        unlocked = true;

        // Important:
        // Make the base Skill_Base upgrade check pass too.
        upgradeType = SkillUpgradeType.DeepBreath;

        SaveRuntimeSnapshot();

        Debug.Log("✅ SexSkill_DeepBreath unlocked.");
    }
}