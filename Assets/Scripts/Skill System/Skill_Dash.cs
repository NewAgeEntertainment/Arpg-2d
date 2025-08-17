using UnityEngine;

public class Skill_Dash : Skill_Base
{
    protected override void Awake()
    {
        base.Awake();
        // Optional: allow dash without the tree
        // ForceUnlock(true);
    }

    public void OnStartEffect()
    {
        if (Unlocked(SkillUpgradeType.Dash_CloneOnStart) || Unlocked(SkillUpgradeType.Dash_CloneOnStartAndArrival))
            CreateClone();

        if (Unlocked(SkillUpgradeType.Dash_ShardOnStart) || Unlocked(SkillUpgradeType.Dash_ShardOnStartAndArrival))
            CreateShard();
    }

    public void OnEndEffect()
    {
        if (Unlocked(SkillUpgradeType.Dash_CloneOnStartAndArrival))
            CreateClone();

        if (Unlocked(SkillUpgradeType.Dash_ShardOnStartAndArrival))
            CreateShard();
    }

    private void CreateShard()
    {
        if (skillManager?.shard != null)
            skillManager.shard.CreateRawShard();
    }

    private void CreateClone()
    {
        Debug.Log("Create Time echo");
        // TODO: spawn your time echo here
    }
}
