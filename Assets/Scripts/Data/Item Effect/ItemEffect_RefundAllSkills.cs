using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Refund All Skills", fileName = "Item effect data - Refund All Skills")]
public class ItemEffect_RefundAllSkills : ItemEffect_DataSO
{
    public override void ExecuteEffect()
    {
        UI ui = FindFirstObjectByType<UI>();
        ui.SkillTreeUI.RefundAllSkills();
    }
}
