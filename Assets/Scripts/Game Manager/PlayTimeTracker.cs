using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayTimeTracker : MonoBehaviour
{
    public static PlayTimeTracker Instance { get; private set; }

    // Events
    public static event Action<int> OnSecondChanged;     // total seconds
    public static event Action<string> OnSceneChanged;   // pretty scene display

    // State
    public static int TotalSecondsInt { get; private set; } = 0;
    public static bool IsRunning { get; private set; } = false;

    public static string CurrentSceneName { get; private set; } = "";
    public static string CurrentSceneDisplay { get; private set; } = "";

    [Header("Optional Persistence")]
    [SerializeField] private bool loadFromPrefsOnAwake = false;
    [SerializeField] private string playerPrefsKey = "PlayTime_TotalSeconds";

    [Header("Optional Pretty Names (raw -> display)")]
    [SerializeField]
    private SceneNameMap[] prettyNames =
    {
        new SceneNameMap{ raw = "Level_01", display = "Forest Area 0" },
        new SceneNameMap{ raw = "Level_02", display = "Forest Area 1" },
    };

    [Serializable]
    public struct SceneNameMap { public string raw; public string display; }

    private float _accumulator = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheSceneNames(SceneManager.GetActiveScene());

        if (loadFromPrefsOnAwake)
        {
            TotalSecondsInt = PlayerPrefs.GetInt(playerPrefsKey, 0);
            OnSecondChanged?.Invoke(TotalSecondsInt);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Update()
    {
        if (!IsRunning) return;

        _accumulator += Time.unscaledDeltaTime; // unaffected by timescale
        if (_accumulator >= 1f)
        {
            int whole = (int)_accumulator;
            _accumulator -= whole;
            TotalSecondsInt += whole;
            OnSecondChanged?.Invoke(TotalSecondsInt);
        }
    }

    // -------- Scene tracking --------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CacheSceneNames(scene);

    private void OnActiveSceneChanged(Scene from, Scene to) => CacheSceneNames(to);

    private void CacheSceneNames(Scene s)
    {
        if (!s.IsValid()) return;

        CurrentSceneName = s.name;
        CurrentSceneDisplay = ResolvePrettyName(CurrentSceneName);
        OnSceneChanged?.Invoke(CurrentSceneDisplay);
    }

    private string ResolvePrettyName(string raw)
    {
        if (prettyNames != null)
        {
            foreach (var m in prettyNames)
            {
                if (!string.IsNullOrEmpty(m.raw) && string.Equals(m.raw, raw, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(m.display)) return m.display;
                }
            }
        }
        return raw ?? string.Empty;
    }

    /// <summary>Programmatically override the pretty display for the current scene.</summary>
    public static void OverrideCurrentSceneDisplay(string display)
    {
        CurrentSceneDisplay = display ?? string.Empty;
        OnSceneChanged?.Invoke(CurrentSceneDisplay);
    }

    // -------- Public API (timer control) --------

    /// <summary>Set counter to zero and start counting.</summary>
    public static void ResetAndStart()
    {
        TotalSecondsInt = 0;
        if (Instance != null) Instance._accumulator = 0f;
        IsRunning = true;
        OnSecondChanged?.Invoke(TotalSecondsInt);
    }

    /// <summary>Alias for ResetAndStart (legacy name).</summary>
    public static void StartTimer() => ResetAndStart();

    /// <summary>Start/resume without resetting.</summary>
    public static void Resume() => IsRunning = true;

    /// <summary>Pause the counter (doesn't reset).</summary>
    public static void Pause() => IsRunning = false;

    /// <summary>Stop and reset to zero.</summary>
    public static void StopAndReset()
    {
        IsRunning = false;
        TotalSecondsInt = 0;
        if (Instance != null) Instance._accumulator = 0f;
        OnSecondChanged?.Invoke(TotalSecondsInt);
    }

    /// <summary>Persist current seconds to PlayerPrefs.</summary>
    public static void SaveToPrefs()
    {
        if (Instance == null) return;
        PlayerPrefs.SetInt(Instance.playerPrefsKey, TotalSecondsInt);
        PlayerPrefs.Save();
    }

    /// <summary>Load seconds from PlayerPrefs (does not auto-start).</summary>
    public static void LoadFromPrefs()
    {
        if (Instance == null) return;
        TotalSecondsInt = PlayerPrefs.GetInt(Instance.playerPrefsKey, 0);
        OnSecondChanged?.Invoke(TotalSecondsInt);
    }

    // -------- Formatting helpers (used by UI.cs) --------

    /// <summary>Format total seconds as HH:MM (hours collapsed, e.g., 27:05).</summary>
    public static string FormatHHMM(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Mathf.Max(0, totalSeconds));
        int hours = (int)ts.TotalHours; // include days
        return $"{hours:00}:{ts.Minutes:00}";
    }

    /// <summary>Format total seconds as HH:MM:SS (hours collapsed).</summary>
    public static string FormatHHMMSS(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Mathf.Max(0, totalSeconds));
        int hours = (int)ts.TotalHours;
        return $"{hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    }
}
