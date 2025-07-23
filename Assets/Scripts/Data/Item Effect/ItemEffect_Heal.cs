using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Heal effect", fileName = "Item effect data - heal")]
public class ItemEffect_Heal : ItemEffect_DataSO
{

    [SerializeField] private float healPercent = .1f;

    public override void ExecuteEffect(Player target)
    {
        if (target == null) return;

        float healAmount = target.stats.GetMaxHealth() * healPercent;
        target.health.IncreaseHealth(healAmount);

        Debug.Log($"[HealEffect] Healed {target.name} for {healAmount}");
    }
}
