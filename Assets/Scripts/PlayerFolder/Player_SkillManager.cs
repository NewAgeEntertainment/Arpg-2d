using UnityEngine;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Thrust thrust { get; private set; }
    public SexSkill_DeepBreath deepBreath { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_Sword swordSpin { get; private set; } // Assuming you have a SkillObject_Sword class similar to SkillObject_Shard

    public float manaCost { get; set; } // Placeholder for mana cost, can be set in individual skills if needed


    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>();
        thrust = GetComponentInChildren<Skill_Thrust>();
        shard = GetComponentInChildren<Skill_Shard>();
        deepBreath = GetComponentInChildren<SexSkill_DeepBreath>();
    }

    public Skill_Base GetSkillByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Thrust:
                return thrust;
            case SkillType.Dash:
                return dash;
            case SkillType.TimeShard: return shard;
            case SkillType.SwordSpin: return swordSpin;
            case SkillType.DeepBreath:
                // Assuming you have a Skill_DeepBreath class
                return deepBreath;
            default:
                Debug.LogError("Skill type not found: " + skillType);
                return null;
        }
    }
}
