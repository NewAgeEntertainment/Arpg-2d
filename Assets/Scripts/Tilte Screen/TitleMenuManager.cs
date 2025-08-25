using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PixelCrushers;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;     // "MainMenu" root (this object stays active)
    [SerializeField] private GameObject loadPanel;     // "Save_Load Panel" (has UI_SaveLoadPanel)
    [SerializeField] private GameObject optionsPanel;  // root that holds UI_Options
    [SerializeField] private GameObject controlsPanel;

    [Header("Main Buttons Root (hide when Save/Load/Options are open)")]
    [SerializeField] private GameObject mainButtonsRoot; // drag your "Button Holder" here

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
    private UI_Options optionsUI;
    private bool isTransitioning = false;

    // Save/Load handling (like UI.cs)
    private UI_SaveLoadPanel uiSaveLoad;
    private bool saveLoadHooked = false;

    // NEW: keep manager alive—never disable mainPanel; use CanvasGroup instead
    private CanvasGroup mainPanelCG;

    private void Awake()
    {
        // Ensure initial visibility
        if (mainPanel != null) mainPanel.SetActive(true);
        if (loadPanel != null) loadPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        // If button root isn’t wired, try to guess it under mainPanel
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
        optionsUI = (optionsPanel != null) ? optionsPanel.GetComponentInChildren<UI_Options>(true) : null;

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

        // NEW: Add/Use CanvasGroup on mainPanel so we can disable input without disabling the object
        if (mainPanel != null)
        {
            mainPanelCG = mainPanel.GetComponent<CanvasGroup>();
            if (mainPanelCG == null) mainPanelCG = mainPanel.AddComponent<CanvasGroup>();
            mainPanelCG.interactable = true;
            mainPanelCG.blocksRaycasts = true;
            // alpha remains whatever your UI art needs; we are not fading the menu here
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Save/Load: let the panel swallow ESC first (handles overwrite dialogs, etc.)
            if (uiSaveLoad != null && uiSaveLoad.IsOpen)
            {
                if (uiSaveLoad.HandleCancel()) return; // its Closed event (or our CloseLoad) will re-show buttons
            }

            // Controls (inside Options)
            if (controlsPanel != null && controlsPanel.activeSelf) { CloseControls(); return; }

            // Options: mirror Save/Load behavior
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                if (optionsUI == null && optionsPanel != null)
                    optionsUI = optionsPanel.GetComponentInChildren<UI_Options>(true);

                if (optionsUI != null && optionsUI.HandleCancel())
                {
                    // After a handled cancel, restore main menu interactivity & buttons
                    RestoreMainMenuInteractivity();
                    return;
                }

                // Fallback safety
                CloseOptions();
                return;
            }

            // Legacy: Load panel wrapper (in case uiSaveLoad is nested)
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

    // ----------------- LOAD -----------------

    public void OpenLoad()
    {
        if (isTransitioning) return;

        EnsureActiveAncestors(loadPanel);
        loadPanel?.SetActive(true);

        // Keep manager alive: hide buttons + disable mainPanel raycasts
        HideMainMenuInteractivity();

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

        // Restore main menu interactivity & buttons
        RestoreMainMenuInteractivity();

        HookSaveLoadClosed(false);
    }

    // ----------------- OPTIONS -----------------

    public void OpenOptions()
    {
        if (optionsPanel == null || isTransitioning) return;

        // Keep manager alive: hide buttons + disable mainPanel raycasts
        HideMainMenuInteractivity();

        EnsureActiveAncestors(optionsPanel);
        optionsPanel.SetActive(true);
        controlsPanel?.SetActive(false);

        if (optionsUI == null && optionsPanel != null)
            optionsUI = optionsPanel.GetComponentInChildren<UI_Options>(true);

        optionsUI?.OpenOptions();
    }

    public void CloseOptions()
    {
        if (optionsPanel == null) return;

        if (optionsUI == null && optionsPanel != null)
            optionsUI = optionsPanel.GetComponentInChildren<UI_Options>(true);

        if (optionsUI != null && optionsPanel.activeSelf)
            optionsUI.ClosePanel();

        controlsPanel?.SetActive(false);

        // Restore main menu interactivity & buttons
        RestoreMainMenuInteractivity();
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
            RestoreMainMenuInteractivity();
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

    // NEW: centralize main menu interactivity toggles so manager never gets disabled
    private void HideMainMenuInteractivity()
    {
        SetMainButtonsVisible(false);
        if (mainPanelCG != null)
        {
            mainPanelCG.interactable = false;
            mainPanelCG.blocksRaycasts = false;
            // alpha unchanged (keep background art)
        }
    }

    // TitleMenuManager.cs
    public void CloseAllOptionPanels()
    {
        // Close sub-panels first
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Show the title main panel + buttons again
        if (mainPanel != null) mainPanel.SetActive(true);
        SetMainButtonsVisible(true);   // uses your existing mainButtonsRoot
    }


    private void RestoreMainMenuInteractivity()
    {
        SetMainButtonsVisible(true);
        if (mainPanelCG != null)
        {
            mainPanelCG.interactable = true;
            mainPanelCG.blocksRaycasts = true;
        }
        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(true); // safety in case someone else disabled it
    }
}
