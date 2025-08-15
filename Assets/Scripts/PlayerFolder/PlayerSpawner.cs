using UnityEngine;
using System.Collections;

#if UNITY_CINEMACHINE
using Unity.Cinemachine; // Cinemachine v3
#endif

public class PlayerSpawner : MonoBehaviour
{
    [Header("Assign Player Prefab")]
    [SerializeField] private Player playerPrefab;

    [Header("Optional spawn point (leave empty to use this GameObject's position)")]
    [SerializeField] private Transform spawnPoint;

    [Header("Cinemachine v3 Auto-Bind")]
    [SerializeField] private bool bindCinemachine = true;
    [SerializeField] private bool alsoSetLookAt = true;
    [SerializeField, Min(0f)] private float cameraBindRetrySeconds = 2f;

    private Player _player;

    private void Awake()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No player prefab assigned!");
            return;
        }

        // Find existing (including DDOL) or spawn a new one
        _player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        Vector3 pos = (spawnPoint != null ? spawnPoint.position : transform.position);

        if (_player == null)
        {
            _player = Instantiate(playerPrefab, pos, Quaternion.identity);
            _player.name = playerPrefab.name; // clean name in hierarchy
        }
        else
        {
            _player.TeleportPlayer(pos);
        }

        // Let global systems know who the current player is
        GameManager.Instance?.RegisterPlayer(_player);
    }

    private void Start()
    {
        if (bindCinemachine)
            StartCoroutine(BindCameraRetryCo(cameraBindRetrySeconds));
    }

    private IEnumerator BindCameraRetryCo(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            // Player can be reassigned by other systems; re-check
            var p = _player != null ? _player : FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (TryBindCameraOnce(p))
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

#if UNITY_CINEMACHINE
        Debug.LogWarning("[PlayerSpawner] Timed out binding Cinemachine camera to Player.");
#endif
    }

    private bool TryBindCameraOnce(Player p)
    {
        if (p == null) return false;

#if UNITY_CINEMACHINE
        var vcam = PickBestVcam();
        if (vcam != null)
        {
            vcam.Follow = p.transform;
            if (alsoSetLookAt) vcam.LookAt = p.transform;
            // Debug.Log($"[PlayerSpawner] CinemachineCamera bound to {p.name} (prio {vcam.Priority}).");
            return true;
        }
        // If you want a log when no vcam is found, uncomment:
        // Debug.LogWarning("[PlayerSpawner] No CinemachineCamera found to bind.");
#endif
        return false; // no CM v3 available or no camera found yet
    }

#if UNITY_CINEMACHINE
    private CinemachineCamera PickBestVcam()
    {
        // Choose the highest-priority active camera; fall back to any if none active
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

        if (best == null)
            best = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

        return best;
    }
#endif
}
