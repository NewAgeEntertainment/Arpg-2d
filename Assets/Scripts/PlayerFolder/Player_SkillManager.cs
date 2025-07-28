using UnityEngine;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Thrust thrust { get; private set; }
    public SexSkill_DeepBreath deepBreath { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_Sword swordSpin { get; private set; }

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
        // You should have a way to load Skill_DataSO for DeepBreath
        Skill_DataSO deepBreathData = LoadDeepBreathSkill();

        if (deepBreathData != null)
        {
            deepBreath.SetSkillUpgrade(deepBreathData);
        }
        else
        {
            Debug.LogError("[SkillManager] Deep Breath Skill_DataSO not found.");
        }
    }

    private Skill_DataSO LoadDeepBreathSkill()
    {
        return Resources.Load<Skill_DataSO>("Skill Data/Sex Skills/Skill data - Deep Breath");
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
