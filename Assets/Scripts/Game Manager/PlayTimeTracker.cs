using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers;

public class PlayTimeTracker : Saver
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

    [Serializable]
    private class Data
    {
        public int totalSeconds;
        public string sceneName;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheSceneNames(SceneManager.GetActiveScene());
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

        // unscaled so it continues while paused / in menus
        _accumulator += Time.unscaledDeltaTime;
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
                if (!string.IsNullOrEmpty(m.raw) &&
                    string.Equals(m.raw, raw, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(m.display)) return m.display;
                }
            }
        }
        return raw ?? string.Empty;
    }

    public static void OverrideCurrentSceneDisplay(string display)
    {
        CurrentSceneDisplay = display ?? string.Empty;
        OnSceneChanged?.Invoke(CurrentSceneDisplay);
    }

    // -------- Public API (timer control) --------

    public static void ResetAndStart()
    {
        TotalSecondsInt = 0;
        if (Instance != null) Instance._accumulator = 0f;
        IsRunning = true;
        OnSecondChanged?.Invoke(TotalSecondsInt);
    }

    public static void StartTimer() => ResetAndStart();

    public static void Resume() => IsRunning = true;

    public static void Pause() => IsRunning = false;

    public static void StopAndReset()
    {
        IsRunning = false;
        TotalSecondsInt = 0;
        if (Instance != null) Instance._accumulator = 0f;
        OnSecondChanged?.Invoke(TotalSecondsInt);
    }

    // -------- Formatting helpers --------

    public static string FormatHHMM(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Mathf.Max(0, totalSeconds));
        int hours = (int)ts.TotalHours;
        return $"{hours:00}:{ts.Minutes:00}";
    }

    public static string FormatHHMMSS(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Mathf.Max(0, totalSeconds));
        int hours = (int)ts.TotalHours;
        return $"{hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    }

    // ================= PixelCrushers Save/Load =================

    public override string RecordData()
    {
        var data = new Data
        {
            totalSeconds = TotalSecondsInt,
            sceneName = CurrentSceneName
        };

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            // No saved data for this slot (brand new game)
            TotalSecondsInt = 0;
            _accumulator = 0f;
        }
        else
        {
            var data = SaveSystem.Deserialize<Data>(s);
            if (data != null)
            {
                TotalSecondsInt = data.totalSeconds;
                _accumulator = 0f;   // restart local counter
                CurrentSceneName = data.sceneName;
                CurrentSceneDisplay = ResolvePrettyName(CurrentSceneName);
            }
        }

        Debug.Log($"[PlayTimeTracker] ApplyData -> {TotalSecondsInt} seconds");
        OnSecondChanged?.Invoke(TotalSecondsInt);
        OnSceneChanged?.Invoke(CurrentSceneDisplay);

        // important: after load, keep counting
        IsRunning = true;
    }

}
