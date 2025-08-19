// UICanvasSingleton.cs
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps a single instance of the Game UI canvas alive across scenes,
/// optionally hidden until the first gameplay scene, and culls duplicates.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class UICanvasSingleton : MonoBehaviour
{
    public static UICanvasSingleton Instance { get; private set; }

    [Header("Identity")]
    [Tooltip("Singleton key so only canvases with the SAME key compete.\nUse a unique key for your gameplay HUD, e.g. 'GameUIRoot'.")]
    [SerializeField] private string key = "GameUIRoot";

    [Header("Persistence")]
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Activation")]
    [Tooltip("Keep the Game UI hidden until this scene loads once.")]
    [SerializeField] private bool activateOnlyOnFirstGameplay = true;

    [Tooltip("Name of your first gameplay scene.")]
    [SerializeField] private string firstGameplaySceneName = "Level_01";

    [Header("Visibility Targets (optional)")]
    [Tooltip("If empty, all Canvas components under this object are toggled.")]
    [SerializeField] private Canvas[] canvasesToToggle;

    [Tooltip("Optional extra roots to hide/show (children only). DO NOT assign this GameObject here.")]
    [SerializeField] private GameObject[] extraRootsToToggle;

    [Header("Duplicate Control")]
    [Tooltip("If another instance with the same key exists, destroy this one on Awake.")]
    [SerializeField] private bool destroyDuplicatesOnAwake = true;

    [Tooltip("Also cull duplicates again on every scene load (paranoid mode).")]
    [SerializeField] private bool cullOnEverySceneLoad = true;

    private static bool hasActivatedOnce = false;

    private void Awake()
    {
        // Optional: destroy immediately if another with same key exists
        if (destroyDuplicatesOnAwake)
        {
            var allSameKey = FindObjectsOfType<UICanvasSingleton>(true)
                .Where(s => s.key == key)
                .ToList();

            // Keep the oldest existing (or first in list) and kill newcomers.
            foreach (var s in allSameKey)
            {
                if (s != this && Instance != null && Instance.key == key)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // Singleton guard (per key)
        if (Instance != null && Instance != this && Instance.key == key)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        if (canvasesToToggle == null || canvasesToToggle.Length == 0)
            canvasesToToggle = GetComponentsInChildren<Canvas>(true);

        // Hide until first gameplay (but keep this GameObject active so code can run)
        if (activateOnlyOnFirstGameplay && !hasActivatedOnce)
            SetVisible(false);
        else
            SetVisible(true);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (cullOnEverySceneLoad) CullDuplicatesNow();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (cullOnEverySceneLoad) CullDuplicatesNow();

        if (activateOnlyOnFirstGameplay && !hasActivatedOnce)
        {
            if (scene.name == firstGameplaySceneName)
            {
                SetVisible(true);
                hasActivatedOnce = true;
            }
            else
            {
                SetVisible(false);
            }
        }
    }

    private void SetVisible(bool v)
    {
        if (canvasesToToggle != null)
        {
            foreach (var c in canvasesToToggle)
                if (c) c.enabled = v;
        }

        if (extraRootsToToggle != null)
        {
            foreach (var g in extraRootsToToggle)
                if (g && g != gameObject) g.SetActive(v);
        }
    }

    /// <summary>
    /// Removes all other UICanvasSingletons with the same key, keeping this one.
    /// </summary>
    public void CullDuplicatesNow()
    {
        var all = FindObjectsOfType<UICanvasSingleton>(true)
            .Where(s => s.key == key)
            .ToList();

        // Prefer to keep 'this' instance.
        foreach (var s in all)
        {
            if (s == this) continue;
            Destroy(s.gameObject);
        }
    }

    /// <summary>
    /// Static helper: remove duplicates for the given key (keep the first found).
    /// You can call this from TitleMenuManager before/after loading a scene.
    /// </summary>
    public static void CullAll(string keyToCull = "GameUIRoot")
    {
        var all = FindObjectsOfType<UICanvasSingleton>(true)
            .Where(s => s.key == keyToCull)
            .ToList();

        if (all.Count <= 1) return;

        var keeper = all[0];
        foreach (var s in all)
        {
            if (s == keeper) continue;
            Destroy(s.gameObject);
        }
        Instance = keeper;
    }
}
