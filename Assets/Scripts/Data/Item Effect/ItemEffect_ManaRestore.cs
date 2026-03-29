using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/ManaHeal effect", fileName = "Item effect data - manaheal")]
public class ItemEffect_ManaRestore : ItemEffect_DataSO
{
    [SerializeField] private float restorePercent = 0.1f;

    public override void ExecuteEffect(Component target)
    {
        if (target == null)
        {
            Debug.LogWarning("[ItemEffect_ManaRestore] Target is null.");
            return;
        }

        base.ExecuteEffect(target);

        Entity_Stats stats = target.GetComponent<Entity_Stats>();
        Entity_Mana mana = target.GetComponent<Entity_Mana>();

        if (stats == null)
        {
            Debug.LogError("[ItemEffect_ManaRestore] Target Entity_Stats is null.");
            return;
        }

        if (mana == null)
        {
            Debug.LogError("[ItemEffect_ManaRestore] Target Entity_Mana is null.");
            return;
        }

        float restoreAmount = stats.GetMaxMana() * restorePercent;
        mana.IncreaseMana(restoreAmount);

        Debug.Log($"[ItemEffect_ManaRestore] Restored {restoreAmount} mana to {target.name}");
    }
}