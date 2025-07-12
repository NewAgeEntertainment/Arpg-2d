using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/ManaHeal effect", fileName = "Item effect data - manaheal")]
public class ItemEffect_ManaRestore : ItemEffect_DataSO
{
    [SerializeField] private float restorePercent = .1f;

    public override void ExecuteEffect(Player target)
    {
        if (target == null) return;

        float restoreAmount = target.stats.GetMaxMana() * restorePercent;
        target.mana.IncreaseMana(restoreAmount);

        Debug.Log($"[ManaRestore] Restored {restoreAmount} mana to {target.name}");
    }
}
