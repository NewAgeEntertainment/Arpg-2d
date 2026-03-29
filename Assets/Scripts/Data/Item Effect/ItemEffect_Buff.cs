using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Buff effect", fileName = "Item effect data - Buff")]
public class ItemEffect_Buff : ItemEffect_DataSO
{
    [SerializeField] private BuffEffectData[] buffsToApply;
    [SerializeField] private float duration;
    [SerializeField] private string source = "";

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(source))
            source = Guid.NewGuid().ToString();
    }

    public override bool CanBeUsed(Component target)
    {
        if (target == null) return false;

        Player player = target.GetComponent<Player>();
        if (player != null && player.stats != null)
            return player.stats.CanApplyBuffOf(source);

        Debug.LogWarning("[ItemEffect_Buff] Target does not support buff usage.");
        return false;
    }

    public override void ExecuteEffect(Component target)
    {
        if (target == null) return;

        base.ExecuteEffect(target);

        Player player = target.GetComponent<Player>();
        if (player != null && player.stats != null)
        {
            player.stats.ApplyBuff(buffsToApply, duration, source);
            return;
        }

        Debug.LogWarning("[ItemEffect_Buff] Target does not support buff application.");
    }
}