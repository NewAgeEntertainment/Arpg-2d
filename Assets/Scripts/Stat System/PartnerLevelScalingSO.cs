using UnityEngine;

[CreateAssetMenu(fileName = "PartnerLevelScaling", menuName = "Game/Stats/Partner Level Scaling", order = 10)]
public class PartnerLevelScalingSO : ScriptableObject
{
    [Header("Baseline Level (no bonus at this level)")]
    [Min(1)] public int baseLevel = 1;

    [Header("Per-level gains (Flat)")]
    public float maxArousalPerLevel = 5f;
    public float sexualDamagePerLevel = 1f;
    public float resiliencePerLevel = 1f;
    public float sexualRestraintPerLevel = 0.05f;
    public float strokePerLevel = 0.5f;

    [Header("Optional Curve Multiplier")]
    [Tooltip("If empty, multiplier = 1.\nIf set, evaluates by normalized level progress (0..1).")]
    public AnimationCurve curve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Tooltip("Level value that maps to curve time = 1.0")]
    [Min(1)] public int curveMaxLevel = 50;

    /// <summary>
    /// Returns a multiplier for per-level gains at a given level.
    /// </summary>
    public float CurveMult(int level)
    {
        if (curve == null || curve.length == 0) return 1f;

        int lvl = Mathf.Max(1, level);
        float t = (curveMaxLevel <= 1) ? 1f : Mathf.InverseLerp(1f, curveMaxLevel, lvl);
        return Mathf.Max(0f, curve.Evaluate(t));
    }
}