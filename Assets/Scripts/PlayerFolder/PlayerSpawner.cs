using UnityEngine;
using System.Collections;
using UnityEngine.Playables;

#if UNITY_CINEMACHINE
using Unity.Cinemachine; // Cinemachine v3
#endif

public class PlayerSpawner : MonoBehaviour
{
    [Header("Assign Player Prefab")]
    [SerializeField] private Player playerPrefab;

    [Header("Optional spawn point (leave empty to use this GameObject's position)")]
    [SerializeField] private Transform spawnPoint;

    [Header("Spawn Policy")]
    [Tooltip("If true, ALWAYS spawn the prefab even if a Player exists (replaces existing).")]
    [SerializeField] private bool preferPrefabOverExisting = true;

    [Tooltip("If replacing, spawn the prefab at the existing Player's pose.")]
    [SerializeField] private bool copyPoseFromExisting = true;

    [Tooltip("If replacing, destroy any existing Players after the prefab is spawned.")]
    [SerializeField] private bool destroyExistingOnReplace = true;

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

        var existingPlayers = FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var haveExisting = existingPlayers != null && existingPlayers.Length > 0;

        Vector3 targetPos = (spawnPoint ? spawnPoint.position : transform.position);
        Quaternion targetRot = Quaternion.identity;
        Vector3 targetScale = Vector3.one;

        if (preferPrefabOverExisting)
        {
            // Replace any existing Player(s)
            if (haveExisting && copyPoseFromExisting)
            {
                // Use the first one's pose as the spawn pose
                var first = existingPlayers[0].transform;
                targetPos = first.position;
                targetRot = first.rotation;
                targetScale = first.localScale;
            }

            _player = Instantiate(playerPrefab, targetPos, targetRot);
            _player.transform.localScale = targetScale;
            _player.name = playerPrefab.name;

            if (destroyExistingOnReplace && haveExisting)
            {
                foreach (var p in existingPlayers)
                    if (p && p.gameObject != _player.gameObject)
                        Destroy(p.gameObject);
            }
        }
        else
        {
            // Keep an existing Player if one is around; otherwise spawn new
            if (haveExisting)
            {
                _player = existingPlayers[0];
                _player.TeleportPlayer(targetPos);
            }
            else
            {
                _player = Instantiate(playerPrefab, targetPos, Quaternion.identity);
                _player.name = playerPrefab.name;
            }
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
            return true;
        }
#endif
        return false;
    }

#if UNITY_CINEMACHINE
    private CinemachineCamera PickBestVcam()
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

        if (best == null)
            best = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

        return best;
    }
#endif



}


