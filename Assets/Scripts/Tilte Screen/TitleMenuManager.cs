using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PixelCrushers;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;     // "MainMenu"
    [SerializeField] private GameObject loadPanel;     // "Save_Load Panel" (has UI_SaveLoadPanel)
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Main Buttons Root (hide when Save/Load is open)")]
    [SerializeField] private GameObject mainButtonsRoot; // <-- drag your "Button Holder" here

    [Header("Buttons (optional wire-up)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Names")]
    [SerializeField] private string firstLevelSceneName = "Level_01";

    [Header("Save Wipe Options for 'New Game'")]
    [SerializeField] private bool wipeAllSlotsOnNewGame = false;

    [Header("Transition (local fade if no PixelCrushers SceneTransitionManager)")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    private LoadMenu loadMenu;
    private OptionsMenu optionsMenu;
    private bool isTransitioning = false;

    // Save/Load handling (like UI.cs)
    private UI_SaveLoadPanel uiSaveLoad;
    private bool saveLoadHooked = false;

    private void Awake()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (loadPanel != null) loadPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        // Optional: guess Button Holder if not assigned
        if (mainButtonsRoot == null && mainPanel != null)
        {
            foreach (Transform child in mainPanel.transform)
            {
                if (loadPanel != null && child == loadPanel.transform) continue;
                if (child.GetComponentInChildren<Button>(true) != null)
                {
                    mainButtonsRoot = child.gameObject;
                    break;
                }
            }
        }

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
            if (uiSaveLoad != null && uiSaveLoad.IsOpen)
            {
                if (uiSaveLoad.HandleCancel()) return; // Closed via panel; Closed event will re-show buttons.
            }

            if (controlsPanel != null && controlsPanel.activeSelf) { CloseControls(); return; }
            if (optionsPanel != null && optionsPanel.activeSelf) { CloseOptions(); return; }
            if (loadPanel != null && loadPanel.activeSelf) { CloseLoad(); return; }
        }
    }

    // ----------------- Play/Quit -----------------

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

    // ----------------- OPEN LOAD -----------------

    public void OpenLoad()
    {
        if (isTransitioning) return;

        bool loadIsChildOfMain = (mainPanel != null && loadPanel != null &&
                                  loadPanel.transform.IsChildOf(mainPanel.transform));

        if (!loadIsChildOfMain) mainPanel?.SetActive(false);
        else if (!mainPanel.activeSelf) mainPanel.SetActive(true);

        EnsureActiveAncestors(loadPanel);
        loadPanel?.SetActive(true);

        if (uiSaveLoad == null && loadPanel != null)
            uiSaveLoad = loadPanel.GetComponentInChildren<UI_SaveLoadPanel>(true);
#if UNITY_2022_1_OR_NEWER
        if (uiSaveLoad == null)
            uiSaveLoad = FindFirstObjectByType<UI_SaveLoadPanel>(FindObjectsInactive.Include);
#else
        if (uiSaveLoad == null)
            uiSaveLoad = FindObjectOfType<UI_SaveLoadPanel>(true);
#endif
        if (uiSaveLoad == null)
        {
            Debug.LogError("[TitleMenuManager] UI_SaveLoadPanel not found in scene.");
            return;
        }

        HookSaveLoadClosed(true);

        // HIDE MENU BUTTONS while Save/Load is shown
        SetMainButtonsVisible(false);

        if (!uiSaveLoad.gameObject.activeSelf) uiSaveLoad.gameObject.SetActive(true);
        uiSaveLoad.OpenForLoad(UI_SaveLoadPanel.OpenContext.TitleMenu);

        if (loadMenu == null && loadPanel != null)
            loadMenu = loadPanel.GetComponentInChildren<LoadMenu>(true);
        loadMenu?.RefreshList();
    }

    public void CloseLoad()
    {
        if (uiSaveLoad != null && uiSaveLoad.IsOpen)
            uiSaveLoad.ClosePanel();

        loadPanel?.SetActive(false);

        // SHOW MENU BUTTONS again
        SetMainButtonsVisible(true);

        if (mainPanel != null && !mainPanel.activeSelf) mainPanel.SetActive(true);

        HookSaveLoadClosed(false);
    }

    // ----------------- Options -----------------

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

    // ----------------- Transitions -----------------

    private IEnumerator NewGameTransition_Co()
    {
        isTransitioning = true;
        SetMenuInteractable(false);

        SaveSystem.ResetGameState();
        PlayerPrefs.DeleteKey(SaveSystem.LastSavedGameSlotPlayerPrefsKey);
        PlayerPrefs.Save();

        yield return StartCoroutine(LocalFadeGuard_Co(show: true, duration: fadeDuration));

        SaveSystem.RestartGame(firstLevelSceneName);

        isTransitioning = false;

        PlayTimeTracker.ResetAndStart();
        PixelCrushers.SaveSystem.sceneLoaded += OnFirstGameplayLoaded_StartTimer;

        SaveSystem.RestartGame(firstLevelSceneName);
    }

    private void OnFirstGameplayLoaded_StartTimer(string sceneName, int sceneIndex)
    {
        PixelCrushers.SaveSystem.sceneLoaded -= OnFirstGameplayLoaded_StartTimer;
        PlayTimeTracker.ResetAndStart();
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

    // ----------------- Save/Load bounce-back -----------------

    private void HookSaveLoadClosed(bool hook)
    {
        if (uiSaveLoad == null) return;
        if (hook && !saveLoadHooked)
        {
            uiSaveLoad.Closed += OnSaveLoadClosedFromTitle;
            saveLoadHooked = true;
        }
        else if (!hook && saveLoadHooked)
        {
            uiSaveLoad.Closed -= OnSaveLoadClosedFromTitle;
            saveLoadHooked = false;
        }
    }

    private void OnSaveLoadClosedFromTitle(UI_SaveLoadPanel.OpenContext ctx)
    {
        if (ctx == UI_SaveLoadPanel.OpenContext.TitleMenu)
        {
            loadPanel?.SetActive(false);
            SetMainButtonsVisible(true);                // <- show buttons again
            if (mainPanel != null && !mainPanel.activeSelf) mainPanel.SetActive(true);
            HookSaveLoadClosed(false);
        }
    }

    // ----------------- Helpers -----------------

    private static void EnsureActiveAncestors(GameObject go)
    {
        if (go == null) return;
        var t = go.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    private void SetMainButtonsVisible(bool visible)
    {
        if (mainButtonsRoot != null) mainButtonsRoot.SetActive(visible);
    }
}
