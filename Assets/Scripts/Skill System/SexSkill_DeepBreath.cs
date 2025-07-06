using UnityEngine;


public class SexSkill_DeepBreath : Skill_Base
{
    [SerializeField] private SexyTimeLogic sexyTimeLogic;
    [SerializeField] private bool unlocked = false;

    protected override void Awake()
    {
        base.Awake();
        if (sexyTimeLogic == null)
            sexyTimeLogic = FindObjectOfType<SexyTimeLogic>();
    }

    public override void TryUseSkill()
    {
        if (!CanUseSkill())
            return;

        if (sexyTimeLogic == null)
        {
            Debug.LogWarning("Deep Breath skill failed: SexyTimeLogic is not assigned.");
            return;
        }

        if (!Unlocked(SkillUpgradeType.DeepBreath))
            return;

        if (Time.time >= sexyTimeLogic.deepBreatheTimestamp)
        {
            // ✅ Blue bar logic directly here
            sexyTimeLogic.playerBar.value -= sexyTimeLogic.playerBarValueDeplete;
            sexyTimeLogic.deepBreatheTimestamp = Time.time + sexyTimeLogic.deepBreatheCooldown;

            Debug.Log("Deep Breath used. Blue bar reduced.");
        }
        else
        {
            Debug.Log("Deep Breath is on cooldown.");
        }
    }


    public override void SetSkillUpgrade(Skill_DataSO skillData)
    {
        base.SetSkillUpgrade(skillData);

        if (skillData.upgradeData != null && skillData.upgradeData.upgradeType == SkillUpgradeType.DeepBreath)
        {
            Unlock(); // Unlock the skill  
            Debug.Log("Deep Breath skill unlocked via upgrade.");
        }
    }

    public void Unlock()
    {
        unlocked = true;
        Debug.Log("✅ SexSkill_DeepBreath -> Unlock() called. Skill is now unlocked.");
    }
}

