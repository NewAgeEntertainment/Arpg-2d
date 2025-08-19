using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PixelCrushers;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;     // Play / Load / Options / End Game
    [SerializeField] private GameObject loadPanel;     // Holds LoadMenu
    [SerializeField] private GameObject optionsPanel;  // Holds OptionsMenu
    [SerializeField] private GameObject controlsPanel; // Optional child of options

    [Header("Buttons (optional wire-up)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Names")]
    [Tooltip("Your first gameplay scene name (must be in Build Settings).")]
    [SerializeField] private string firstLevelSceneName = "Level_01";

    [Header("Save Wipe Options for 'New Game'")]
    [Tooltip("If true, ALL save slots are deleted before starting a new game. Leave OFF to preserve saves.")]
    [SerializeField] private bool wipeAllSlotsOnNewGame = false; // <-- default OFF

    [Header("Transition (local fade if no PixelCrushers SceneTransitionManager)")]
    [SerializeField] private CanvasGroup fadeOverlay;   // Fullscreen Image under a Canvas, Raycast Target ON
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    private LoadMenu loadMenu;
    private OptionsMenu optionsMenu;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (loadPanel != null) loadPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        loadMenu = (loadPanel != null) ? loadPanel.GetComponentInChildren<LoadMenu>(true) : null;
        optionsMenu = (optionsPanel != null) ? optionsPanel.GetComponentInChildren<OptionsMenu>(true) : null;

        if (playButton != null) playButton.onClick.AddListener(OnClickPlay);
        if (loadButton != null) loadButton.onClick.AddListener(OpenLoad);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);

        SaveSystem.autoUnloadAdditiveScenes = true;
        SaveSystem.debug = Debug.isDebugBuild;

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (controlsPanel != null && controlsPanel.activeSelf) { CloseControls(); return; }
            if (optionsPanel != null && optionsPanel.activeSelf) { CloseOptions(); return; }
            if (loadPanel != null && loadPanel.activeSelf) { CloseLoad(); return; }
        }
    }

    // --- Main actions ---

    public void OnClickPlay()
    {
        if (isTransitioning) return;
        StartCoroutine(NewGameTransition_Co());
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Load ---

    public void OpenLoad()
    {
        if (isTransitioning || loadPanel == null) return;

        mainPanel?.SetActive(false);
        loadPanel.SetActive(true);

        // ✅ If you're using the custom UI_SaveLoadPanel, open it here:
        var uiSaveLoad = loadPanel.GetComponent<UI_SaveLoadPanel>();
        if (uiSaveLoad != null)
        {
            uiSaveLoad.OpenForLoad();   // turns on content + refreshes slots
        }

        // (Optional) If you still use PixelCrushers LoadMenu somewhere inside:
        if (loadMenu == null) loadMenu = loadPanel.GetComponentInChildren<LoadMenu>(true);
        loadMenu?.RefreshList();
    }

    public void CloseLoad()
    {
        if (loadPanel == null) return;
        loadPanel.SetActive(false);
        mainPanel?.SetActive(true);
    }

    // --- Options ---

    public void OpenOptions()
    {
        if (optionsPanel == null || isTransitioning) return;
        mainPanel?.SetActive(false);
        optionsPanel.SetActive(true);
        controlsPanel?.SetActive(false);
    }

    public void CloseOptions()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(false);
        mainPanel?.SetActive(true);
    }

    public void OpenControls()
    {
        if (controlsPanel == null || optionsPanel == null) return;
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        if (controlsPanel == null) return;
        controlsPanel.SetActive(false);
    }

    // =========================
    // Transition Orchestration
    // =========================

    private IEnumerator NewGameTransition_Co()
    {
        isTransitioning = true;
        SetMenuInteractable(false);

        // ⛔ Do NOT delete saves when starting a new game.
        // If you still have a wipe flag, make sure it's false or remove that code.
        // if (wipeAllSlotsOnNewGame) SaveUtility.DeleteAllSlots(); // <- remove/disable

        // ✅ Fresh in-memory state only (doesn't touch files on disk)
        SaveSystem.ResetGameState();

        // ✅ Prevent any auto-save that "uses the last slot" from overwriting old files
        PlayerPrefs.DeleteKey(SaveSystem.LastSavedGameSlotPlayerPrefsKey);
        PlayerPrefs.Save();

        // (optional) local fade before kicking the scene change
        yield return StartCoroutine(LocalFadeGuard_Co(show: true, duration: fadeDuration));

        // ✅ Load your first gameplay scene (PixelCrushers handles transitions if present)
        SaveSystem.RestartGame(firstLevelSceneName);

        // If you prefer to bypass SaveSystem's scene loader:
        // UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevelSceneName);

        isTransitioning = false;

        // Reset counter for a fresh file:
        PlayTimeTracker.ResetAndStart();

        // Start timer when the first gameplay scene has finished loading:
        PixelCrushers.SaveSystem.sceneLoaded += OnFirstGameplayLoaded_StartTimer;

        // Kick the scene change:
        SaveSystem.RestartGame(firstLevelSceneName);

    }

    private void OnFirstGameplayLoaded_StartTimer(string sceneName, int sceneIndex)
    {
        PixelCrushers.SaveSystem.sceneLoaded -= OnFirstGameplayLoaded_StartTimer;
        PlayTimeTracker.ResetAndStart();   // start from 0 and begin counting
    }



    private void SetMenuInteractable(bool interactable)
    {
        if (mainPanel != null) ToggleCanvasGroup(mainPanel, interactable);
        if (loadPanel != null) ToggleCanvasGroup(loadPanel, interactable);
        if (optionsPanel != null) ToggleCanvasGroup(optionsPanel, interactable);
        if (controlsPanel != null) ToggleCanvasGroup(controlsPanel, interactable);

        if (playButton) playButton.interactable = interactable;
        if (loadButton) loadButton.interactable = interactable;
        if (optionsButton) optionsButton.interactable = interactable;
        if (quitButton) quitButton.interactable = interactable;
    }

    private void ToggleCanvasGroup(GameObject go, bool interactable)
    {
        var cg = go ? go.GetComponent<CanvasGroup>() : null;
        if (cg == null) return;
        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;
    }



    private IEnumerator LocalFadeGuard_Co(bool show, float duration)
    {
        if (fadeOverlay == null || duration <= 0f) yield break;

        fadeOverlay.blocksRaycasts = true;
        fadeOverlay.interactable = true;

        float start = fadeOverlay.alpha;
        float end = show ? 1f : 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeOverlay.alpha = Mathf.Lerp(start, end, k);
            yield return null;
        }
        fadeOverlay.alpha = end;

        if (!show)
        {
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    private bool PixelCrushersTools_HasSceneTransitionManager()
    {
#if PIXELCRUSHERS
        var stm = Object.FindFirstObjectByType<PixelCrushers.SceneTransitionManager>(FindObjectsInactive.Include);
        return stm != null;
#else
        return false;
#endif
    }
}
