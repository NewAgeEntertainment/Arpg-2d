using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)] // ensure this initializes very early
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Optional References")]
    [SerializeField] private Player player; // auto-found if left null

    /// <summary>Globally accessible Player reference (auto-caches if missing).</summary>
    public Player Player => player != null
        ? player
        : (player = FindFirstObjectByType<Player>(FindObjectsInactive.Include));

    /// <summary>Raised when a Player is registered (spawner or Player itself can call RegisterPlayer).</summary>
    public event System.Action<Player> OnPlayerRegistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // We don't initiate scene changes; we just ensure references survive them.
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        // If a Player already exists (e.g., DDOL), surface it to listeners.
        if (Player != null)
            OnPlayerRegistered?.Invoke(player);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // No scene-transition logic here—only keep references fresh if needed.
        if (player == null)
        {
            var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (p != null) RegisterPlayer(p);
        }
    }

    /// <summary>Call this after you instantiate the Player (e.g., in your PlayerSpawner).</summary>
    public void RegisterPlayer(Player p)
    {
        if (p == null) return;
        player = p;
        OnPlayerRegistered?.Invoke(p);
        // Debug.Log($"[GameManager] Player registered: {p.name}");
    }

    /// <summary>Clear cached refs (useful when returning to title).</summary>
    public void ClearCachedRefs()
    {
        player = null;
    }
}
