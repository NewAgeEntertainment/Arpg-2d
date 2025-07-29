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
        if (!CanUseSkill())
            return;

        // ✅ Skill logic should be added in child class override
        Debug.Log($"[Skill_Base] {name} skill used. Mana spent: {manaCost}");

        // Child classes will do the actual effect (e.g. damage, VFX, etc.)
    }

    public virtual void SetSkillUpgrade(Skill_DataSO skillData)
    {
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

        if (skillData.upgradeData == null)
        {
            Debug.LogError($"[{name}] skillData.upgradeData is NULL on {skillData.name}. Aborting SetSkillUpgrade.");
            return;
        }

        UpgradeData upgrade = skillData.upgradeData;

        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
        manaCost = upgrade.manaCost;
        damageScaleData = upgrade.damageScale != null ? upgrade.damageScale : damageScaleData;

        try
        {
            if (player == null)
                player = FindFirstObjectByType<Player>();

            var ui = player?.ui?.inGameUI;
            if (ui == null)
            {
                Debug.LogWarning($"[{name}] UI not ready while setting skill upgrade ({skillData.name}). Skipping UI binding.");
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
            Debug.Log($"[Skill_Base] {name} is on cooldown.");
            return false;
        }

        if (mana == null || !mana.UseMana(manaCost))
        {
            Debug.Log($"[Skill_Base] Not enough mana to use {name}. Required: {manaCost}");
            return false;
        }

        SetSkillOnCooldown();
        return true;
    }

    public bool Unlocked(SkillUpgradeType upgradeToCheck) => upgradeType == upgradeToCheck;

    protected bool OnCooldown() => Time.time < lastTimeUsed + cooldown;

    public void SetSkillOnCooldown()
    {
        if (player?.ui?.inGameUI != null)
        {
            var slot = player.ui.inGameUI.GetSkillSlot(skillType);
            slot?.StartCooldown(cooldown);
        }

        lastTimeUsed = Time.time;
    }

    public void ResetCoolDownBy(float cooldownReduction) => lastTimeUsed += cooldownReduction;

    public void ResetCoolDown() => lastTimeUsed = Time.time;

    public void ResetCooldown()
    {
        if (player?.ui?.inGameUI != null)
        {
            var slot = player.ui.inGameUI.GetSkillSlot(skillType);
            slot?.ResetCooldown();
        }

        lastTimeUsed = Time.time - cooldown;
    }
}
