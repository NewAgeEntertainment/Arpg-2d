using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Refund All Skills", fileName = "Item effect data - Refund All Skills")]
public class ItemEffect_RefundAllSkills : ItemEffect_DataSO
{
    public override void ExecuteEffect(Player target)
    {
        if (target == null)
        {
            Debug.LogWarning("[ItemEffect_RefundAllSkills] Target player is null!");
            return;
        }

        var skillTree = target.GetComponent<UI_SkillTree>();
        if (skillTree != null)
        {
            skillTree.RefundAllSkills();
            Debug.Log($"[ItemEffect_RefundAllSkills] Refunded skills for {target.name}");
        }
        else
        {
            Debug.LogWarning($"[ItemEffect_RefundAllSkills] No SkillTreeUI found on {target.name}");
        }
    }
}
