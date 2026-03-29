using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Refund All Skills", fileName = "Item effect data - Refund All Skills")]
public class ItemEffect_RefundAllSkills : ItemEffect_DataSO
{
    public override void ExecuteEffect(Component target)
    {
        if (target == null)
        {
            Debug.LogWarning("[ItemEffect_RefundAllSkills] Target is null!");
            return;
        }

        base.ExecuteEffect(target);

        Player player = target.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogWarning("[ItemEffect_RefundAllSkills] Target is not a Player.");
            return;
        }

        UI ui = Object.FindFirstObjectByType<UI>();
        if (ui != null && ui.SkillTreeUI != null)
        {
            ui.SkillTreeUI.RefundAllSkills();
            Debug.Log($"[ItemEffect_RefundAllSkills] Refunded skills for {target.name}");
        }
        else
        {
            Debug.LogWarning("[ItemEffect_RefundAllSkills] No UI SkillTree found.");
        }
    }
}