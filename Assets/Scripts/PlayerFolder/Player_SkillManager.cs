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
    [SerializeField] private Player_Stats stats; // 🔹 Reference to player stats

    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>();
        thrust = GetComponentInChildren<Skill_Thrust>();
        shard = GetComponentInChildren<Skill_Shard>();
        swordSpin = GetComponentInChildren<Skill_Sword>();
        deepBreath = GetComponentInChildren<SexSkill_DeepBreath>();
    }

    private void Start()
    {
        if (deepBreathData != null)
        {
            deepBreath.SetSkillUpgrade(deepBreathData);
        }
        else
        {
            Debug.LogError("[SkillManager] Deep Breath Skill_DataSO not assigned.");
        }
    }

    public Skill_Base GetSkillByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Thrust:
                return thrust;
            case SkillType.Dash:
                return dash;
            case SkillType.TimeShard:
                return shard;
            case SkillType.SwordSpin:
                return swordSpin;
            case SkillType.DeepBreath:
                return deepBreath;
            default:
                Debug.LogError($"[SkillManager] Unknown skill type: {skillType}");
                return null;
        }
    }

    public float CalculateSkillDamage(Skill_DataSO skillData)
    {
        if (skillData == null || skillData.upgradeData == null)
        {
            Debug.LogWarning("[SkillManager] Skill data is null or missing upgrade data.");
            return 0f;
        }

        float basePower = skillData.upgradeData.damageScale.basePower;
        float scalingMultiplier = skillData.upgradeData.damageScale.scalingMultiplier;
        StatType scalingStat = skillData.upgradeData.damageScale.scalingStat;
        float statValue = stats.GetStatByType(scalingStat).GetValue();
        float levelBonus = 1 + (stats.CurrentLevel * 0.05f);

        float damage = (basePower + statValue * scalingMultiplier) * levelBonus;
        return Mathf.Floor(damage);
    }
}
