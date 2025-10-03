using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    public Player_SkillManager skillManager { get; private set; }
    public Player player { get; private set; }
    public Entity_Mana mana { get; private set; }
    
    public float ManaCost => manaCost;   // <— now UI can read the live cost
    public float CurrentManaCost => manaCost;
    public float Cooldown => cooldown;   // (optional but handy)

    private string PrefKey(string field) => $"Skill_{skillType}_{field}";

    public DamageScaleData damageScaleData { get; private set; }

    [Header("General details")]
    [SerializeField] protected SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType; // None => locked unless default-unlocked
    [SerializeField] protected float cooldown = 0f;
    [SerializeField] protected float manaCost = 0f;

    [Header("Unlocking")]
    [Tooltip("If true, this skill is usable even without a Skill_DataSO upgrade (good for Dash/Thrust).")]
    [SerializeField] private bool unlockedByDefault = false;

    private float lastTimeUsed;

    protected virtual void Awake()
    {
        mana = GetComponentInParent<Entity_Mana>();
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();

        // Allow immediate use at startup
        lastTimeUsed = -cooldown;

        // ⬇️ restore per-skill snapshot if present
        LoadRuntimeSnapshotIfAny();
    }

    /// <summary>Pure check: does NOT spend mana or start cooldown.</summary>
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

    /// <summary>Convenience pure check (no side effects).</summary>
    public bool CanUseSkill() => CanUseSkillCheck(out _);

    /// <summary>Commit the use: spend mana (if any) and start cooldown.</summary>
    public bool CommitUse()
    {
        // Spend mana
        if (mana != null && manaCost > 0f)
        {
            if (!mana.UseMana(manaCost))
                return false;
        }

        SetSkillOnCooldown();
        return true;
    }

    /// <summary>Default TryUse for one-shot skills (e.g., Shard). Child overrides can call base.TryUseSkill().</summary>
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
        // Child classes should actually perform the effect (projectile, AoE, etc).
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
        damageScaleData = upgrade.damageScale != null ? upgrade.damageScale : damageScaleData;

        // Wire HUD slot
        try
        {
            if (player == null) player = FindFirstObjectByType<Player>();
            var ui = player?.ui?.inGameUI;
            if (ui != null)
            {
                var slot = ui.GetSkillSlot(skillType);
                if (slot != null) slot.SetupSkillSlot(skillData);
                else Debug.LogWarning($"[{name}] No UI skill slot found for {skillType} while setting {skillData.name}.");
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

        // ⬇️ Important: make the skill READY immediately after unlock
        ResetCooldown();           // <-- use the READY version, not ResetCoolDown()

        // ✅ NEW: tell the HUD a skill was (just) unlocked/ready
        player?.ui?.inGameUI?.NotifySkillUnlocked(skillType, skillData);

        // ⬇️ persist new live values so the next scene picks them up
        SaveRuntimeSnapshot();
    }

    public void SaveRuntimeSnapshot()
    {
        PlayerPrefs.SetInt(PrefKey("unlocked"), IsUnlocked() ? 1 : 0);
        PlayerPrefs.SetFloat(PrefKey("manaCost"), manaCost);
        PlayerPrefs.SetFloat(PrefKey("cooldown"), cooldown);
        PlayerPrefs.Save();
    }

    public void LoadRuntimeSnapshotIfAny()
    {
        if (!PlayerPrefs.HasKey(PrefKey("unlocked"))) return;

        if (PlayerPrefs.GetInt(PrefKey("unlocked"), 0) == 1)
            ForceUnlock(true);  // sets unlockedByDefault = true (your IsUnlocked() already checks this)


        manaCost = PlayerPrefs.GetFloat(PrefKey("manaCost"), manaCost);
        cooldown = PlayerPrefs.GetFloat(PrefKey("cooldown"), cooldown);

        // Ensure UI cooldown ring & slot are consistent
        ResetCooldown();
    }

    private void RestoreBaseUpgradeForThisSkill()
    {
        switch (skillType)
        {
            case SkillType.Dash: upgradeType = SkillUpgradeType.Dash; break;
            case SkillType.Thrust: upgradeType = SkillUpgradeType.Thrust; break;
            case SkillType.Shard: upgradeType = SkillUpgradeType.Shard; break;
            case SkillType.SwordSpin: upgradeType = SkillUpgradeType.SwordSpin; break;
            case SkillType.DeepBreath: upgradeType = SkillUpgradeType.DeepBreath; break;
            default: ForceUnlock(true); break; // fallback if you add new skills later
        }
    }

    /// <summary>Core unlock predicate.</summary>
    public bool IsUnlocked() => unlockedByDefault || upgradeType != SkillUpgradeType.None;

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

    /// <summary>READY now (cooldown cleared).</summary>
    public void ResetCooldown()
    {
        if (player?.ui?.inGameUI != null)
        {
            var slot = player.ui.inGameUI.GetSkillSlot(skillType);
            slot?.ResetCooldown();
        }

        lastTimeUsed = Time.time - cooldown;
    }

    // Back-compat alias (your old code called this but it actually put the skill ON cooldown).
    // Keep it, but make it call the READY version to avoid confusion/bugs.
    public void ResetCoolDown() => ResetCooldown();

    /// <summary>Optional: call at runtime to allow Dash/Thrust without tree.</summary>
    public void ForceUnlock(bool value = true) => unlockedByDefault = value;
}
