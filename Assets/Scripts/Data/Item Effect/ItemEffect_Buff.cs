using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Buff effect", fileName = "Item effect data - Buff")]

public class ItemEffect_Buff : ItemEffect_DataSO
{
    [SerializeField] private BuffEffectData[] buffsToApply;
    [SerializeField] private float duration;
    [SerializeField] private string source = Guid.NewGuid().ToString();



    public override bool CanBeUsed(Player player)
    {
        return player.stats.CanApplyBuffOf(source);
    }

    public override void ExecuteEffect(Player target)
    {
        if (target == null) return;

        target.stats.ApplyBuff(buffsToApply, duration, source);
    }

}
