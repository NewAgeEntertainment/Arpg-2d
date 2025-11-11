using UnityEngine;

[DisallowMultipleComponent]
public class BossMarker : MonoBehaviour
{
    [Tooltip("Shown on the boss HP bar. Leave blank to just show 'BOSS'.")]
    public string bossDisplayName = "BOSS";

    private Entity_Health health;

    private void OnEnable()
    {
        health = GetComponent<Entity_Health>();
        if (!health) { Debug.LogWarning($"[BossMarker] No Entity_Health on {name}."); return; }

        if (BossHealthBarUI.Instance != null)
            BossHealthBarUI.Instance.ShowFor(health, bossDisplayName);

        // If the boss gets revived later, re-show automatically
        health.OnRevived += HandleRevived;
    }

    private void OnDisable()
    {
        if (health != null) health.OnRevived -= HandleRevived;

        // If this was the currently bound boss, hide UI
        if (BossHealthBarUI.Instance != null)
            BossHealthBarUI.Instance.Hide();
    }

    private void HandleRevived()
    {
        if (BossHealthBarUI.Instance != null)
            BossHealthBarUI.Instance.ShowFor(health, bossDisplayName);
    }
}
