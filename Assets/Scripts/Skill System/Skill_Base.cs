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
    [SerializeField] protected float manaCost; // Placeholder for mana cost, can be set in individual skills if needed
    // [SerializeField] protected float manaCost; // Placeholder for mana cost, can be set in individual skills if needed
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        mana = GetComponentInParent<Entity_Mana>();
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();
        // Initialize lastTimeUsed to a negative value to allow immediate use of the skill
        lastTimeUsed = lastTimeUsed - cooldown;
    }

    public virtual void TryUseSkill() // this is the method that will be called to use the skill
    {

    }

    // this connects the skill Manager
    public void SetSkillUpgrade(UpgradeData upgrade)
    {
        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
        manaCost = upgrade.manaCost;
        damageScaleData = upgrade.damageScale;
    }

    public bool CanUseSkill()
    {
        if (upgradeType == SkillUpgradeType.None)
            return false; // if skill is not Unlocked, it cannot be used  

        if (OnCooldown())
        {
            Debug.Log("Skill is on cooldown.");
            return false;
        }

        //Corrected mana check
        if (mana == null || !mana.UseMana(manaCost))
        {
            Debug.Log("Not enough mana to use the skill.");
            return false;
        }

        SetSkillOnCooldown();
        return true;
    }

    protected bool Unlocked(SkillUpgradeType upgradeToCheck) => upgradeType == upgradeToCheck;


    protected bool OnCooldown() => Time.time < lastTimeUsed + cooldown;
    public void SetSkillOnCooldown() => lastTimeUsed = Time.time;
    public void ResetCoolDownBy(float cooldownReduction) => lastTimeUsed = lastTimeUsed + cooldownReduction;
    public void ResetCoolDown() => lastTimeUsed = Time.time;
}
