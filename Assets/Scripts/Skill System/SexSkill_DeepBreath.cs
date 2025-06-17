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
            sexyTimeLogic.blueBar.value -= sexyTimeLogic.blueBarValueDeplete;
            sexyTimeLogic.deepBreatheTimestamp = Time.time + sexyTimeLogic.deepBreatheCooldown;

            Debug.Log("Deep Breath used. Blue bar reduced.");
        }
        else
        {
            Debug.Log("Deep Breath is on cooldown.");
        }
    }


    public override void SetSkillUpgrade(UpgradeData data)
    {
        base.SetSkillUpgrade(data);

        if (data.upgradeType == SkillUpgradeType.DeepBreath)
    {
        //Unlock; // or apply cooldown/manaCost here too
        Debug.Log("Deep Breath skill unlocked via upgrade.");
    }
}

    public void Unlock()
    {
        unlocked = true;
        Debug.Log("✅ SexSkill_DeepBreath -> Unlock() called. Skill is now unlocked.");
    }
}

