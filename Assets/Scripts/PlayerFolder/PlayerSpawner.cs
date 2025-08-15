using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Assign Player Prefab")]
    [SerializeField] private Player playerPrefab;

    [Header("Optional spawn point (leave empty to use this GameObject's position)")]
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        // Ensure we have a prefab assigned
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No player prefab assigned!");
            return;
        }

        // Find an existing player in the scene (or in DontDestroyOnLoad)
        Player existingPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (existingPlayer == null)
        {
            // Spawn a new one at the desired position
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Player newPlayer = Instantiate(playerPrefab, pos, Quaternion.identity);
            newPlayer.name = playerPrefab.name; // clean name in hierarchy
        }
        else
        {
            // Move existing player to this spawn point
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            existingPlayer.TeleportPlayer(pos);
        }
    }
}
