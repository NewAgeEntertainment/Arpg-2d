using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
[DefaultExecutionOrder(100)]
public class CinemachineV3AutoFollow : MonoBehaviour
{
    [SerializeField] private bool alsoSetLookAt = true;
    [SerializeField, Min(0f)] private float retryWindowSeconds = 2f;
    [SerializeField] private string fallbackPlayerTag = "Player";

    private CinemachineCamera vcam;
    private Coroutine bindCo;

    private void Awake() => vcam = GetComponent<CinemachineCamera>();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBindNow();

        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerRegistered += OnPlayerRegistered;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerRegistered -= OnPlayerRegistered;

        if (bindCo != null) { StopCoroutine(bindCo); bindCo = null; }
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (bindCo != null) StopCoroutine(bindCo);
        bindCo = StartCoroutine(RetryBindForSeconds(retryWindowSeconds));
    }

    private void OnPlayerRegistered(Player p)
    {
        if (p != null) BindTo(p.transform);
    }

    private System.Collections.IEnumerator RetryBindForSeconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (TryBindNow()) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.LogWarning("[CinemachineV3AutoFollow] Timed out binding to Player.");
    }

    private bool TryBindNow()
    {
        var player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null) { BindTo(player.transform); return true; }

        if (!string.IsNullOrEmpty(fallbackPlayerTag))
        {
            var go = GameObject.FindWithTag(fallbackPlayerTag);
            if (go != null) { BindTo(go.transform); return true; }
        }
        return false;
    }

    private void BindTo(Transform target)
    {
        if (vcam == null || target == null) return;
        vcam.Target.TrackingTarget = target;
        if (alsoSetLookAt) vcam.Target.LookAtTarget = target;
        // Debug.Log($"[CinemachineV3AutoFollow] Tracking '{target.name}'");
    }
}
