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
        // 1) Basic null checks
        if (skillData == null)
        {
            Debug.LogError($"[{name}] SetSkillUpgrade received NULL skillData.");
            return;
        }

        if (skillData.skillType != skillType)
        {
            Debug.LogWarning(
                $"[{name}] Mismatched SkillType! This Skill_Base is [{skillType}] but the SO is [{skillData.skillType}]");
        }

        // 2) UpgradeData null check
        if (skillData.upgradeData == null)
        {
            Debug.LogError($"[{name}] skillData.upgradeData is NULL on {skillData.name}. Aborting SetSkillUpgrade.");
            return;
        }

        UpgradeData upgrade = skillData.upgradeData;

        // 3) Assign fields (guard against nulls if needed)
        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
        manaCost = upgrade.manaCost;
        damageScaleData = upgrade.damageScale != null ? upgrade.damageScale : damageScaleData; // don't overwrite with null

        // 4) UI binding can be optional at this point
        //    If you call SetSkillUpgrade very early (before UI has spawned), skip this gracefully.
        try
        {
            // Ensure we have a valid player reference if Skill_Base expects one
            if (player == null)
                player = FindFirstObjectByType<Player>();

            var ui = player?.ui?.inGameUI;
            if (ui == null)
            {
                // No UI yet -> just warn and safely continue.
                Debug.LogWarning($"[{name}] UI not ready while setting skill upgrade ({skillData.name}). " +
                                 $"Will skip slot setup.");
            }
            else
            {
                var slot = ui.GetSkillSlot(skillType);
                if (slot != null)
                {
                    slot.SetupSkillSlot(skillData);
                }
                else
                {
                    Debug.LogWarning($"[{name}] No UI skill slot found for {skillType} while setting {skillData.name}.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{name}] Exception while wiring UI in SetSkillUpgrade: {ex}");
        }

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
