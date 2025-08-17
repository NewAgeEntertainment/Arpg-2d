using UnityEngine;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Thrust thrust { get; private set; }
    public SexSkill_DeepBreath deepBreath { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_Sword swordSpin { get; private set; }

    [Header("Skill Data References")]
    [SerializeField] private Skill_DataSO deepBreathData;

    [Header("Refs")]
    [SerializeField] private Player_Stats stats;

    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>(true);
        thrust = GetComponentInChildren<Skill_Thrust>(true);
        shard = GetComponentInChildren<Skill_Shard>(true);
        swordSpin = GetComponentInChildren<Skill_Sword>(true);
        deepBreath = GetComponentInChildren<SexSkill_DeepBreath>(true);

        if (stats == null) stats = GetComponentInParent<Player_Stats>();
    }

    private void Start()
    {
        if (deepBreath != null && deepBreathData != null)
        {
            deepBreath.SetSkillUpgrade(deepBreathData);
        }
        else
        {
            Debug.LogWarning("[SkillManager] Deep Breath skill or data not assigned.");
        }

        // Optional sanity logs so you know components were found on the live prefab
        if (dash == null) Debug.LogWarning("[SkillManager] Skill_Dash missing");
        if (thrust == null) Debug.LogWarning("[SkillManager] Skill_Thrust missing");
        if (shard == null) Debug.LogWarning("[SkillManager] Skill_Shard missing");
        if (swordSpin == null) Debug.LogWarning("[SkillManager] Skill_Sword missing");
        if (deepBreath == null) Debug.LogWarning("[SkillManager] SexSkill_DeepBreath missing");
    }

    public Skill_Base GetSkillByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Thrust: return thrust;
            case SkillType.Dash: return dash;
            case SkillType.TimeShard: return shard;
            case SkillType.SwordSpin: return swordSpin;
            case SkillType.DeepBreath: return deepBreath;
            default:
                Debug.LogError($"[SkillManager] Unknown skill type: {skillType}");
                return null;
        }
    }

    public float CalculateSkillDamage(Skill_DataSO skillData)
    {
        if (skillData == null || skillData.upgradeData == null || stats == null)
        {
            Debug.LogWarning("[SkillManager] Missing skill or stats for damage calc.");
            return 0f;
        }

        float basePower = skillData.upgradeData.damageScale.basePower;
        float scalingMultiplier = skillData.upgradeData.damageScale.scalingMultiplier;
        StatType scalingStat = skillData.upgradeData.damageScale.scalingStat;
        float statValue = stats.GetStatByType(scalingStat).GetValue();
        float levelBonus = 1f + (stats.CurrentLevel * 0.05f);

        float damage = (basePower + statValue * scalingMultiplier) * levelBonus;
        return Mathf.Floor(damage);
    }
}
