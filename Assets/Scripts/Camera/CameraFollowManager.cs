// Attach this to any always-active object (e.g., GameManager or SaveSystem)
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_CINEMACHINE
using Unity.Cinemachine; // Cinemachine v3
#endif

public class CameraFollowManager : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float assignTimeoutSeconds = 2f;   // how long we keep trying after a scene loads

    private Coroutine _assignCo;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartAssignRoutine(); // also try on enable
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_assignCo != null) { StopCoroutine(_assignCo); _assignCo = null; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartAssignRoutine();
    }

    private void StartAssignRoutine()
    {
        if (_assignCo != null) StopCoroutine(_assignCo);
        _assignCo = StartCoroutine(AssignCameraCo());
    }

    private System.Collections.IEnumerator AssignCameraCo()
    {
        float t = 0f;
        while (t < assignTimeoutSeconds)
        {
            if (TryAssignOnce())
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[CameraFollowManager] Timed out trying to assign camera follow.");
    }

    private bool TryAssignOnce()
    {
        var player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player == null) return false;

#if UNITY_CINEMACHINE
        var vcam = PickBestCinemachineCamera();
        if (vcam != null)
        {
            vcam.Follow = player.transform;
            vcam.LookAt  = player.transform;
            Debug.Log($"[CameraFollowManager] Assigned Player to CinemachineCamera (prio {vcam.Priority}).");
            return true;
        }
        Debug.LogWarning("[CameraFollowManager] No CinemachineCamera found in scene.");
#else
        Debug.LogWarning("[CameraFollowManager] UNITY_CINEMACHINE not defined or Cinemachine not installed.");
#endif
        return false;
    }

#if UNITY_CINEMACHINE
    private CinemachineCamera PickBestCinemachineCamera()
    {
        var cams = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CinemachineCamera best = null;
        int bestPriority = int.MinValue;

        foreach (var cam in cams)
        {
            if (cam == null || !cam.isActiveAndEnabled) continue;
            if (cam.Priority > bestPriority)
            {
                bestPriority = cam.Priority;
                best = cam;
            }
        }

        // If none active, return any
        if (best == null)
            best = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

        return best;
    }
#endif
}
