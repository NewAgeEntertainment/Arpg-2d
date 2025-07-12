using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Heal on doing damage", fileName = "Item effect data - Heal on doing Phys Damage")]
public class ItemEffect_HealOnDoindDamage : ItemEffect_DataSO
{
    [SerializeField] private float percentHealerOnAttack = 0.2f;

    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        player.combat.OnDoingPhysicalDamage += HealOnDoingDamage;
    }

    public override void Unsubscribe()
    {
        if (player != null)
        {
            player.combat.OnDoingPhysicalDamage -= HealOnDoingDamage;
            player = null;
        }
    }

    public override void ExecuteEffect(Player target)
    {
        // This type of effect doesn't "activate" on use, but it still must be valid.
        Debug.Log($"[ItemEffect_HealOnDoindDamage] Subscribed to {target.name}");
        Subscribe(target);
    }

    private void HealOnDoingDamage(float damage)
    {
        if (player != null)
        {
            float healAmount = damage * percentHealerOnAttack;
            player.health.IncreaseHealth(healAmount);
            Debug.Log($"[HealOnDoindDamage] Healed {player.name} for {healAmount} from physical damage.");
        }
    }
}
