using System.Collections;
using UnityEngine;

public class MinimapRevealer : MonoBehaviour
{
    [Header("Reveal")]
    public float revealRadius = 4f;
    [Min(0.01f)] public float interval = 0.1f;

    [Header("Follow target (optional)")]
    public bool autoBindPlayerLocator = true;  // uses PlayerLocator.Current
    public Transform followOverride;           // manually assign if needed

    Transform _target;
    Coroutine _loop;

    void OnEnable()
    {
        // Try to find our follow target
        _target = followOverride != null
            ? followOverride
            : (autoBindPlayerLocator ? PlayerLocator.Current : transform);

        if (_loop == null)
            _loop = StartCoroutine(RevealLoop());

        // re-bind if player respawns
        PlayerLocator.OnChanged += OnPlayerChanged;
    }

    void OnDisable()
    {
        PlayerLocator.OnChanged -= OnPlayerChanged;
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
    }

    void OnPlayerChanged(Transform t)
    {
        if (autoBindPlayerLocator && t != null)
            _target = t;
    }

    IEnumerator RevealLoop()
    {
        yield return null; // wait one frame for singletons to init

        while (true)
        {
            var fog = MinimapFog.Instance;
            if (fog != null && _target != null)
                fog.RevealCircle(_target.position, revealRadius);

            yield return new WaitForSeconds(interval);
        }
    }
}
