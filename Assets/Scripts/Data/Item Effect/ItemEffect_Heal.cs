// Assets/Scripts/Data/Item Effect/ItemEffect_Heal.cs
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Heal effect", fileName = "Item effect data - heal")]
public class ItemEffect_Heal : ItemEffect_DataSO
{
    [SerializeField] private float healPercent = 0.10f; // 10%

    public override void ExecuteEffect(Player target)
    {
        if (target == null) { Debug.LogError("[HealEffect] target is null"); return; }

        var stats = target.stats ?? target.GetComponent<Player_Stats>();
        var health = target.health ?? target.GetComponent<Entity_Health>();

        if (stats == null) { Debug.LogError("[HealEffect] target.stats is null"); return; }
        if (health == null) { Debug.LogError("[HealEffect] target.health is null"); return; }

        float healAmount = Mathf.Max(0f, stats.GetMaxHealth() * healPercent);
        health.IncreaseHealth(healAmount);

        // Nudge HUDs just in case your health script doesn’t raise an event:
        target.ui?.playerHealthBar?.UpdateHealth(health.GetCurrentHealth(), stats.GetMaxHealth());
        target.ui?.inGameUI?.ForceRefreshFromCurrentState();

        Debug.Log($"[HealEffect] Healed {target.name} for {healAmount}");
    }
}
