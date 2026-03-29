using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item effect/Heal effect", fileName = "Item effect data - heal")]
public class ItemEffect_Heal : ItemEffect_DataSO
{
    [SerializeField] private float healPercent = 0.10f; // 10%

    public override void ExecuteEffect(Component target)
    {
        if (target == null)
        {
            Debug.LogError("[HealEffect] target is null");
            return;
        }

        base.ExecuteEffect(target);

        Entity_Stats stats = target.GetComponent<Entity_Stats>();
        Entity_Health health = target.GetComponent<Entity_Health>();

        if (stats == null)
        {
            Debug.LogError("[HealEffect] target Entity_Stats is null");
            return;
        }

        if (health == null)
        {
            Debug.LogError("[HealEffect] target Entity_Health is null");
            return;
        }

        float healAmount = Mathf.Max(0f, stats.GetMaxHealth() * healPercent);
        health.IncreaseHealth(healAmount);

        // Player-only UI refresh if the target is the main player
        Player player = target.GetComponent<Player>();
        if (player != null)
        {
            player.ui?.playerHealthBar?.UpdateHealth(health.GetCurrentHealth(), stats.GetMaxHealth());
            player.ui?.inGameUI?.ForceRefreshFromCurrentState();
        }

        Debug.Log($"[HealEffect] Healed {target.name} for {healAmount}");
    }
}