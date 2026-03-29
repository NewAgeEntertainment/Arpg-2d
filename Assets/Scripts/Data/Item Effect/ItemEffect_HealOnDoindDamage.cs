using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Heal on doing damage", fileName = "Item effect data - Heal on doing Phys Damage")]
public class ItemEffect_HealOnDoindDamage : ItemEffect_DataSO
{
    [SerializeField] private float percentHealerOnAttack = 0.2f;

    private Player subscribedPlayer;
    private Companion subscribedCompanion;

    public override void Subscribe(Component target)
    {
        base.Subscribe(target);

        Unsubscribe();

        if (target == null)
            return;

        subscribedPlayer = target.GetComponent<Player>();
        if (subscribedPlayer != null && subscribedPlayer.combat != null)
        {
            subscribedPlayer.combat.OnDoingPhysicalDamage += HealOnDoingDamage;
            return;
        }

        subscribedCompanion = target.GetComponent<Companion>();
        if (subscribedCompanion != null && subscribedCompanion.combat != null)
        {
            subscribedCompanion.combat.OnDoingPhysicalDamage += HealOnDoingDamage;
        }
    }

    public override void Unsubscribe()
    {
        if (subscribedPlayer != null && subscribedPlayer.combat != null)
            subscribedPlayer.combat.OnDoingPhysicalDamage -= HealOnDoingDamage;

        if (subscribedCompanion != null && subscribedCompanion.combat != null)
            subscribedCompanion.combat.OnDoingPhysicalDamage -= HealOnDoingDamage;

        subscribedPlayer = null;
        subscribedCompanion = null;
        targetCharacter = null;
    }

    public override void ExecuteEffect(Component target)
    {
        if (target == null)
        {
            Debug.LogWarning("[ItemEffect_HealOnDoindDamage] Target is null.");
            return;
        }

        base.ExecuteEffect(target);
        Subscribe(target);

        Debug.Log($"[ItemEffect_HealOnDoindDamage] Subscribed to {target.name}");
    }

    private void HealOnDoingDamage(float damage)
    {
        if (targetCharacter == null)
            return;

        Entity_Health health = targetCharacter.GetComponent<Entity_Health>();
        if (health == null)
            return;

        float healAmount = damage * percentHealerOnAttack;
        health.IncreaseHealth(healAmount);

        Debug.Log($"[HealOnDoindDamage] Healed {targetCharacter.name} for {healAmount} from physical damage.");
    }
}