using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    public Player_SkillManager skillManager { get; private set; }
    public Player player { get; private set; }
    public Entity_Mana mana { get; private set; }

    public DamageScaleData damageScaleData { get; private set; }

    [Header("General details")]
    [SerializeField] protected SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] protected float cooldown;
    [SerializeField] protected float manaCost;

    private float lastTimeUsed;

    protected virtual void Awake()
    {
        mana = GetComponentInParent<Entity_Mana>();
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();

        lastTimeUsed = -cooldown; // allow immediate use
    }

    public virtual void TryUseSkill()
    {
        // Custom skill logic goes here
    }

    public virtual void SetSkillUpgrade(Skill_DataSO skillData)
    {
        if (skillData.skillType != skillType)
        {
            Debug.LogWarning($"Mismatched SkillType! This Skill_Base is [{skillType}] but the SO is [{skillData.skillType}]");
        }

        UpgradeData upgrade = skillData.upgradeData;
        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
        manaCost = upgrade.manaCost;
        damageScaleData = upgrade.damageScale;

        player.ui.inGameUI.GetSkillSlot(skillType).SetupSkillSlot(skillData);
        ResetCoolDown();
    }

    public bool CanUseSkill()
    {
        if (upgradeType == SkillUpgradeType.None)
            return false;

        if (OnCooldown())
        {
            Debug.Log("Skill is on cooldown.");
            return false;
        }

        if (mana == null || !mana.UseMana(manaCost))
        {
            Debug.Log("Not enough mana to use the skill.");
            return false;
        }

        SetSkillOnCooldown();
        return true;
    }

    public bool Unlocked(SkillUpgradeType upgradeToCheck) => upgradeType == upgradeToCheck;

    protected bool OnCooldown() => Time.time < lastTimeUsed + cooldown;

    public void SetSkillOnCooldown()
    {
        player.ui.inGameUI.GetSkillSlot(skillType).StartCooldown(cooldown);
        lastTimeUsed = Time.time;
    }

    public void ResetCoolDownBy(float cooldownReduction) => lastTimeUsed += cooldownReduction;

    public void ResetCoolDown() => lastTimeUsed = Time.time;

    public void ResetCooldown()
    {
        player.ui.inGameUI.GetSkillSlot(skillType).ResetCooldown();
        lastTimeUsed = Time.time - cooldown;
    }
}
