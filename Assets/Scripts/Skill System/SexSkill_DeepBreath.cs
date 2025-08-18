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
        if (sexyTimeLogic != null) return true;

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
        // ❌ Do NOT call CanUseSkill() here — it causes a startup delay tied to scene time.
        if (!Unlocked(SkillUpgradeType.DeepBreath)) return;

        if (!ResolveSexyTimeLogic()) return;
        if (!SexyTimeLogic.isSexyTimeGoingOn) return;

        // Mini-game–scoped cooldown only
        if (Time.time < sexyTimeLogic.deepBreatheTimestamp) return;

        var ui = sexyTimeLogic.UI;
        if (ui == null) return;

        // Apply bar drain now
        float newPlayerVal = Mathf.Max(0f, ui.PlayerBarValue - sexyTimeLogic.DeepBreathDepleteAmount);
        ui.UpdateBars(newPlayerVal, ui.PlayerBarMax, ui.PartnerBarValue, ui.PartnerBarMax);

        // Start ONLY the mini-game cooldown
        sexyTimeLogic.deepBreatheTimestamp = Time.time + sexyTimeLogic.deepBreatheCooldown;
        ui.StartDeepBreathCooldown(sexyTimeLogic.deepBreatheCooldown);
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
        Debug.Log("✅ SexSkill_DeepBreath unlocked.");
    }
}
