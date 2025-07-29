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
}
