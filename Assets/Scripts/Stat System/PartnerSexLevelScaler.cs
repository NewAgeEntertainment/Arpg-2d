using UnityEngine;

[DisallowMultipleComponent]
public class PartnerSexLevelScaler : MonoBehaviour
{
    [SerializeField] private int level = 1;
    public int Level => Mathf.Max(1, level);

    [SerializeField] private PartnerLevelScalingSO scaling;

    private Entity_Stats stats;
    private const string TAG = "PartnerLevelScaling";

    private void Awake()
    {
        stats = GetComponent<Entity_Stats>();
        if (stats == null)
            stats = GetComponentInParent<Entity_Stats>(true);
    }

    public void ApplyScaling()
    {
        if (stats == null || scaling == null) return;

        // Clear old scaling first (prevents stacking)
        RemoveScaling();

        int lvl = Mathf.Max(1, level);
        int baseLvl = Mathf.Max(1, scaling.baseLevel);
        int delta = Mathf.Max(0, lvl - baseLvl);
        if (delta <= 0) return;

        float m = scaling.CurveMult(lvl);

        stats.sex.maxArousal.AddModifier(scaling.maxArousalPerLevel * delta * m, StatModType.Flat, TAG);
        stats.sex.sexualDamage.AddModifier(scaling.sexualDamagePerLevel * delta * m, StatModType.Flat, TAG);
        stats.sex.resilience.AddModifier(scaling.resiliencePerLevel * delta * m, StatModType.Flat, TAG);
        stats.sex.sexualRestraint.AddModifier(scaling.sexualRestraintPerLevel * delta * m, StatModType.Flat, TAG);
        stats.sex.stroke.AddModifier(scaling.strokePerLevel * delta * m, StatModType.Flat, TAG);
    }

    public void RemoveScaling()
    {
        if (stats == null) return;

        stats.sex.maxArousal.RemoveModifier(TAG);
        stats.sex.sexualDamage.RemoveModifier(TAG);
        stats.sex.resilience.RemoveModifier(TAG);
        stats.sex.sexualRestraint.RemoveModifier(TAG);
        stats.sex.stroke.RemoveModifier(TAG);
    }
}