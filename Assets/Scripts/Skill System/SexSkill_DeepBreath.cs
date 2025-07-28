using UnityEngine;

public class SexSkill_DeepBreath : Skill_Base
{
    [SerializeField] private SexyTimeLogic sexyTimeLogic;
    [SerializeField] private bool unlocked = false;

    protected override void Awake()
    {
        base.Awake();
        if (sexyTimeLogic == null)
            sexyTimeLogic = FindFirstObjectByType<SexyTimeLogic>();
    }

    public override void TryUseSkill()
    {
        if (!CanUseSkill())
            return;

        if (!Unlocked(SkillUpgradeType.DeepBreath))
            return;

        if (sexyTimeLogic == null || !SexyTimeLogic.isSexyTimeGoingOn)
            return;

        var ui = sexyTimeLogic.UI;
        if (ui == null)
            return;

        if (Time.time < sexyTimeLogic.deepBreatheTimestamp)
            return;

        // Apply bar drain
        float newPlayerVal = Mathf.Max(
            0f,
            ui.PlayerBarValue - sexyTimeLogic.DeepBreathDepleteAmount
        );
        ui.UpdateBars(newPlayerVal, ui.PlayerBarMax, ui.PartnerBarValue, ui.PartnerBarMax);

        sexyTimeLogic.deepBreatheTimestamp = Time.time + sexyTimeLogic.deepBreatheCooldown;
        ui.StartDeepBreathCooldown(sexyTimeLogic.deepBreatheCooldown);  // ✅ Start mini-game cooldown visual
    }


    public override void SetSkillUpgrade(Skill_DataSO skillData)
    {
        if (skillData == null)
        {
            Debug.LogError("[DeepBreath] SetSkillUpgrade called with NULL skillData.");
            return;
        }

        base.SetSkillUpgrade(skillData);

        if (skillData.upgradeData != null && skillData.upgradeData.upgradeType == SkillUpgradeType.DeepBreath)
        {
            Unlock();
            Debug.Log("Deep Breath skill unlocked via upgrade.");
        }
        else
        {
            Debug.LogWarning("[DeepBreath] upgradeData is null or not DeepBreath.");
        }
    }

    public void Unlock()
    {
        unlocked = true;
        Debug.Log("✅ SexSkill_DeepBreath unlocked.");
    }
}
