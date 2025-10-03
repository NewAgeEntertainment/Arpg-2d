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

        // NEW: also try for a short window in case the Player is spawned later this frame.
        StartCoroutine(FindPlayerForAFewFrames());
    }

    // GameManager.cs
    public void AddOnPlayerRegisteredListener(System.Action<Player> listener, bool fireImmediately = true)
    {
        OnPlayerRegistered += listener;
        if (fireImmediately && Player != null) // if we already have one, deliver it now
            listener(Player);
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

    // ==================== ADDED: Bootstrap to guarantee a GM exists ====================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists_BeforeSceneLoad()
    {
        if (Instance != null) return;

        var found = Object.FindObjectOfType<GameManager>(true);
        if (found != null)
        {
            Instance = found;
            Object.DontDestroyOnLoad(found.gameObject);
            return;
        }

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>(); // Awake will set Instance + DDOL
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists_AfterSceneLoad()
    {
        if (Instance != null) return;

        var found = Object.FindObjectOfType<GameManager>(true);
        if (found != null)
        {
            Instance = found;
            Object.DontDestroyOnLoad(found.gameObject);
            return;
        }

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    // ==================== ADDED: Delayed Player discovery after scene load ====================

    private System.Collections.IEnumerator FindPlayerForAFewFrames()
    {
        const float timeout = 2f; // seconds (unscaled)
        float t = 0f;

        while (player == null && t < timeout)
        {
            var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (p != null)
            {
                RegisterPlayer(p);
                yield break;
            }

            yield return null;
            t += Time.unscaledDeltaTime;
        }
    }
}
