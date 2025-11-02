using UnityEngine;

public class MinimapRevealer : MonoBehaviour
{
    public MinimapSettings settings;
    [Tooltip("Reveal radius in world units. If <= 0 uses settings.defaultRevealRadius.")]
    public float revealRadius = 0f;
    [Tooltip("How often to stamp the fog (seconds).")]
    public float interval = 0.1f;

    private float _next;

    private void Update()
    {
        if (Time.time < _next) return;
        _next = Time.time + Mathf.Max(0.02f, interval);

        if (MinimapFog.Instance == null || MinimapFog.Instance.settings == null) return;
        var s = settings != null ? settings : MinimapFog.Instance.settings;
        float r = (revealRadius > 0f) ? revealRadius : s.defaultRevealRadius;
        MinimapFog.Instance.RevealCircle(transform.position, r);
    }
}
