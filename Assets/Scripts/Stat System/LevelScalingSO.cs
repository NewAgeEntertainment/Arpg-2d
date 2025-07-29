using UnityEngine;

[CreateAssetMenu(fileName = "LevelScalingSO", menuName = "Stats/Level Scaling Data")]
public class LevelScalingSO : ScriptableObject
{
    public AnimationCurve healthCurve = AnimationCurve.Linear(1, 10, 99, 1000);
    public AnimationCurve manaCurve = AnimationCurve.Linear(1, 5, 99, 500);
    public AnimationCurve strengthCurve = AnimationCurve.Linear(1, 2, 99, 100);
    public AnimationCurve defenseCurve = AnimationCurve.Linear(1, 2, 99, 100);
    public AnimationCurve intelligenceCurve = AnimationCurve.Linear(1, 1.5f, 99, 75);
    public AnimationCurve luckCurve = AnimationCurve.Linear(1, 1, 99, 50);
    public AnimationCurve vitalityCurve = AnimationCurve.Linear(1, 2.5f, 99, 150);

    public float Evaluate(AnimationCurve curve, int level)
    {
        return curve.Evaluate(level);
    }
}
