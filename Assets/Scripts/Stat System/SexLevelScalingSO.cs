using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Sex Level Scaling", fileName = "SexLevelScalingData")]
public class SexLevelScalingSO : ScriptableObject
{
    [Header("Total bonus at THIS Sex Level (not per-level increment)")]
    [Tooltip("Evaluate curves at SexLevel (1..∞). The value you return is the TOTAL bonus to apply at that level.")]
    public AnimationCurve maxArousalCurve = AnimationCurve.Linear(1, 0, 10, 50);
    public AnimationCurve sexualDamageCurve = AnimationCurve.Linear(1, 0, 10, 20);
    public AnimationCurve strokeCurve = AnimationCurve.Linear(1, 0, 10, 10);
    public AnimationCurve resilienceCurve = AnimationCurve.Linear(1, 0, 10, 5);
    public AnimationCurve sexualRestraintCurve = AnimationCurve.Linear(1, 0, 10, 5);

    /// <summary>
    /// Returns the TOTAL bonus to apply to this level (not the delta from the previous level).
    /// Clamps the input level to the last keyframe time so you never get 0 past the last key.
    /// </summary>
    public float Evaluate(AnimationCurve curve, int sexLevel)
    {
        if (curve == null || curve.length == 0) return 0f;

        float lastTime = curve.keys[curve.length - 1].time;
        float t = Mathf.Clamp(sexLevel, 1f, lastTime);
        return curve.Evaluate(t);
    }
}
