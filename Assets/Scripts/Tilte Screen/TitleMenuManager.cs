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
    [Tooltip("Your first gameplay scene name (must be in Build Settings).\nExample: Level_01")]
    [SerializeField] private string firstLevelSceneName = "Level_01";

    [Header("Save Wipe Options for 'New Game'")]
    [Tooltip("Also delete any saved files/slots before starting a new game.")]
    [SerializeField] private bool wipeAllSlotsOnNewGame = true;

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

        // Optional button wiring if not using the Inspector events:
        if (playButton != null) playButton.onClick.AddListener(OnClickPlay);
        if (loadButton != null) loadButton.onClick.AddListener(OpenLoad);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);

        // PixelCrushers defaults
        SaveSystem.autoUnloadAdditiveScenes = true;
        SaveSystem.debug = Debug.isDebugBuild;

        // Ensure overlay starts hidden but blocks nothing until used
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    private void Update()
    {
        // Global ESC handling (only if a subpanel is open)
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
        if (loadPanel == null || isTransitioning) return;
        mainPanel?.SetActive(false);
        loadPanel.SetActive(true);
        loadMenu?.RefreshList(); // build list each time
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

        // Optional: wipe saves for a "fresh" new game
        if (wipeAllSlotsOnNewGame) SaveUtility.DeleteAllSlots();

        // Reset runtime state (PixelCrushers)
        SaveSystem.ResetGameState();

        // If a PixelCrushers SceneTransitionManager is present under SaveSystem,
        // SaveSystem.RestartGame will use it automatically (fade/async).
        bool hasPCMTransition = PixelCrushersTools_HasSceneTransitionManager();

        if (hasPCMTransition)
        {
            SaveSystem.RestartGame(firstLevelSceneName);
            // Block input until the SaveSystem kicks the new scene (a small guard)
            yield return StartCoroutine(LocalFadeGuard_Co(show: true, duration: 0.01f)); // almost instant
        }
        else
        {
            // Local fade → load → keep faded until new scene is ready
            yield return StartCoroutine(LocalFadeGuard_Co(show: true, duration: fadeDuration));
            SaveSystem.RestartGame(firstLevelSceneName);
            // You can keep it faded; PixelCrushers will swap the scene quickly.
            // Optionally add a small delay if you want to guarantee black between scenes:
            yield return null;
        }

        // We don’t auto-fade back in here because your gameplay scene should own its own entry fade
        // (e.g., via a ScreenFader in the first level). If you want this menu to fade out AND in:
        // yield return StartCoroutine(LocalFadeGuard_Co(show:false, duration:fadeDuration));

        isTransitioning = false;
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
        if (fadeOverlay == null || duration <= 0f)
        {
            // If no overlay, at least block clicks during transition
            yield break;
        }

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
        // If you added the SceneTransitionManager under the Save System prefab, this returns true.
#if PIXELCRUSHERS
        var stm = Object.FindFirstObjectByType<PixelCrushers.SceneTransitionManager>(FindObjectsInactive.Include);
        return stm != null;
#else
        // If you don't use the symbol, just try to find it by type name safely:
        var stm = Object.FindFirstObjectByType<MonoBehaviour>(FindObjectsInactive.Include);
        // naive fallback: you can simplify to "return false;" if you prefer not to reflect/name-check
        return false;
#endif
    }
}
