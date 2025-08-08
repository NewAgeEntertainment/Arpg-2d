using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Scene Tracking")]
    [SerializeField] private string initialSceneName = "StartScene";
    [SerializeField] private string playerSpawnPointName = "PlayerSpawn";
    private Vector3 lastSpawnPosition;
    private bool hasSceneLoaded = false;

    [Header("Player")]
    public Player player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(initialSceneName)) return;

        if (!SceneManager.GetSceneByName(initialSceneName).isLoaded)
        {
            LoadScene(initialSceneName, Vector3.zero);
        }
    }

    public void LoadScene(string sceneName, Vector3 spawnPosition)
    {
        lastSpawnPosition = spawnPosition;
        SaveSystem.LoadScene(sceneName); // ✅ FIXED: LoadScene, not LoadSceneAsync
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        if (player != null)
        {
            // Restore last spawn position
            if (lastSpawnPosition != Vector3.zero)
                player.TeleportPlayer(lastSpawnPosition);

            // Attach camera
            var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = player.transform;
                vcam.LookAt = player.transform;
            }

            hasSceneLoaded = true;
            Debug.Log($"[GameManager] Scene loaded: {scene.name}, Player repositioned.");
        }
        else
        {
            Debug.LogWarning("[GameManager] Player not found in new scene.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void RegisterPlayer(Player p)
    {
        player = p;
    }
}
