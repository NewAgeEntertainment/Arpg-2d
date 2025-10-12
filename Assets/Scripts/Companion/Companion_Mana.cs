using UnityEngine;

public class Companion_Mana : Entity_Mana
{
    [Header("Mana rewards")]
    [SerializeField] private float hitRestorePercent = 0.02f;  // 2% of max per hit
    [SerializeField] private float killRestorePercent = 0.10f; // 10% of max per kill
    [SerializeField] private float minPerHit = 1f;
    [SerializeField] private float minPerKill = 3f;

    /// <summary>
    /// Call this when the companion successfully lands a hit.
    /// Level is optional; pass 1 if you don’t have one.
    /// </summary>
    public void RestoreManaOnHitWithScaling(int level = 1)
    {
        float max = Mathf.Max(1f, GetMaxMana());
        float amount = Mathf.Max(minPerHit, max * hitRestorePercent + 0.1f * Mathf.Max(0, level - 1));
        IncreaseMana(amount); // this will raise OnManaUpdate inside Entity_Mana
    }

    /// <summary>
    /// Call this when the companion gets the killing blow.
    /// </summary>
    public void RestoreManaOnKill()
    {
        float max = Mathf.Max(1f, GetMaxMana());
        float amount = Mathf.Max(minPerKill, max * killRestorePercent);
        IncreaseMana(amount); // this will raise OnManaUpdate inside Entity_Mana
    }
}
