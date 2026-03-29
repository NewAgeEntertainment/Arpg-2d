using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Grant skill point", fileName = "Item effect data - Grant skill point")]
public class ItemEffect_GrantSkillPoint : ItemEffect_DataSO
{
    [SerializeField] private int pointsToAdd;

    public override void ExecuteEffect(Component target)
    {
        base.ExecuteEffect(target);

        UI ui = Object.FindFirstObjectByType<UI>();
        if (ui != null && ui.SkillTreeUI != null)
        {
            ui.SkillTreeUI.AddSkillPoints(pointsToAdd);
            Debug.Log($"[ItemEffect_GrantSkillPoint] Added {pointsToAdd} skill points!");
        }
    }
}