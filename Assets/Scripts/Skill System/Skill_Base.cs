using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    public Player_SkillManager skillManager { get; private set; }
    public Player player { get; private set; }
    public Entity_Mana mana { get; private set; }

    public float ManaCost => manaCost;
    public float CurrentManaCost => manaCost;
    public float Cooldown => cooldown;

    private string PrefKey(string field) => $"Skill_{skillType}_{field}";

    public DamageScaleData damageScaleData { get; private set; }

    [Header("General details")]
    [SerializeField] protected SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType = SkillUpgradeType.None;
    [SerializeField] protected float cooldown = 0f;
    [SerializeField] protected float manaCost = 0f;

    [Header("Unlocking")]
    [Tooltip("If true, this skill is usable even without a Skill_DataSO upgrade. Good for Dash/Thrust/base skills.")]
    [SerializeField] private bool unlockedByDefault = false;

    private float lastTimeUsed;

    protected virtual void Awake()
    {
        mana = GetComponentInParent<Entity_Mana>();
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();

        // Allow immediate use at startup.
        lastTimeUsed = -cooldown;

        LoadRuntimeSnapshotIfAny();
    }

    /// <summary>
    /// Pure check. Does not spend mana or start cooldown.
    /// </summary>
    public bool CanUseSkillCheck(out string reason)
    {
        reason = "";

        if (!IsUnlocked())
        {
            reason = "locked";
            return false;
        }

        if (OnCooldown())
        {
            reason = "on cooldown";
            return false;
        }

        if (mana != null && manaCost > 0f && mana.GetCurrentMana() < manaCost)
        {
            reason = "not enough mana";
            return false;
        }

        return true;
    }

    public bool CanUseSkill() => CanUseSkillCheck(out _);

    /// <summary>
    /// Commits skill use: spends mana and starts cooldown.
    /// </summary>
    public bool CommitUse()
    {
        if (mana != null && manaCost > 0f)
        {
            if (!mana.UseMana(manaCost))
                return false;
        }

        SetSkillOnCooldown();
        return true;
    }

    /// <summary>
    /// Default skill use. Child classes can override this.
    /// </summary>
    public virtual void TryUseSkill()
    {
        if (!CanUseSkillCheck(out var reason))
        {
            Debug.Log($"[Skill_Base] {name} blocked: {reason}");
            return;
        }

        if (!CommitUse())
            return;

        Debug.Log($"[Skill_Base] {name} used. Mana spent: {manaCost}");
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
            Debug.LogWarning($"[{name}] Mismatched SkillType! This Skill_Base is [{skillType}] but SO is [{skillData.skillType}]");
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

        if (upgrade.damageScale != null)
            damageScaleData = upgrade.damageScale;

        Debug.Log($"[{name}] Upgrade applied. New upgradeType = {upgradeType}");

        TryWireSkillSlot(skillData);

        // Make skill ready immediately after unlock/upgrade.
        ResetCooldown();

        player?.ui?.inGameUI?.NotifySkillUnlocked(skillType, skillData);

        SaveRuntimeSnapshot();
    }

    private void TryWireSkillSlot(Skill_DataSO skillData)
    {
        try
        {
            if (player == null)
                player = FindFirstObjectByType<Player>();

            var ui = player?.ui?.inGameUI;

            if (ui != null)
            {
                var slot = ui.GetSkillSlot(skillType);

                if (slot != null)
                    slot.SetupSkillSlot(skillData);
                else
                    Debug.LogWarning($"[{name}] No UI skill slot found for {skillType} while setting {skillData.name}.");
            }
            else
            {
                Debug.LogWarning($"[{name}] UI not ready while setting skill upgrade ({skillData.name}). Skipping UI binding.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{name}] Exception while wiring UI in SetSkillUpgrade: {ex}");
        }
    }

    public void SaveRuntimeSnapshot()
    {
        PlayerPrefs.SetInt(PrefKey("unlocked"), IsUnlocked() ? 1 : 0);
        PlayerPrefs.SetInt(PrefKey("upgradeType"), (int)upgradeType);
        PlayerPrefs.SetFloat(PrefKey("manaCost"), manaCost);
        PlayerPrefs.SetFloat(PrefKey("cooldown"), cooldown);
        PlayerPrefs.Save();

        Debug.Log($"[{name}] Saved skill snapshot. upgradeType = {upgradeType}");
    }

    public void LoadRuntimeSnapshotIfAny()
    {
        if (!PlayerPrefs.HasKey(PrefKey("unlocked")))
            return;

        if (PlayerPrefs.GetInt(PrefKey("unlocked"), 0) == 1)
            ForceUnlock(true);

        if (PlayerPrefs.HasKey(PrefKey("upgradeType")))
            upgradeType = (SkillUpgradeType)PlayerPrefs.GetInt(PrefKey("upgradeType"));

        manaCost = PlayerPrefs.GetFloat(PrefKey("manaCost"), manaCost);
        cooldown = PlayerPrefs.GetFloat(PrefKey("cooldown"), cooldown);

        ResetCooldown();

        Debug.Log($"[{name}] Loaded skill snapshot. upgradeType = {upgradeType}");
    }

    [ContextMenu("Clear This Skill Save")]
    private void ClearThisSkillSave()
    {
        PlayerPrefs.DeleteKey(PrefKey("unlocked"));
        PlayerPrefs.DeleteKey(PrefKey("upgradeType"));
        PlayerPrefs.DeleteKey(PrefKey("manaCost"));
        PlayerPrefs.DeleteKey(PrefKey("cooldown"));
        PlayerPrefs.Save();

        Debug.Log($"[{name}] Cleared saved data for {skillType}.");
    }

    public bool IsUnlocked()
    {
        return unlockedByDefault || upgradeType != SkillUpgradeType.None;
    }

    public bool Unlocked(SkillUpgradeType upgradeToCheck)
    {
        return upgradeType == upgradeToCheck;
    }

    protected bool OnCooldown()
    {
        return Time.time < lastTimeUsed + cooldown;
    }

    public void SetSkillOnCooldown()
    {
        if (player?.ui?.inGameUI != null)
        {
            var slot = player.ui.inGameUI.GetSkillSlot(skillType);
            slot?.StartCooldown(cooldown);
        }

        lastTimeUsed = Time.time;
    }

    public void ResetCoolDownBy(float cooldownReduction)
    {
        lastTimeUsed += cooldownReduction;
    }

    /// <summary>
    /// Makes the skill ready immediately.
    /// </summary>
    public void ResetCooldown()
    {
        if (player?.ui?.inGameUI != null)
        {
            var slot = player.ui.inGameUI.GetSkillSlot(skillType);
            slot?.ResetCooldown();
        }

        lastTimeUsed = Time.time - cooldown;
    }

    // Backwards-compatible alias.
    public void ResetCoolDown()
    {
        ResetCooldown();
    }

    public void ForceUnlock(bool value = true)
    {
        unlockedByDefault = value;
    }
}