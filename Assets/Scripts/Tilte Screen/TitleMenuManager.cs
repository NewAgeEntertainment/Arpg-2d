using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PixelCrushers;
using Rewired;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Main Buttons Root (hide when Save/Load/Options are open)")]
    [SerializeField] private GameObject mainButtonsRoot;

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

    [Header("Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    private Rewired.Player rPlayer;

    [Header("Title Menu Audio")]
    [SerializeField] private bool playTitleBgmOnAwake = true;
    [SerializeField] private string titleBgmGroup = "MainMenuMusic";
    [SerializeField] private string gameplayBgmGroup = "LevelMusic";


    [Header("UI SFX Names")]
    [SerializeField] private string uiConfirmSfx = "UIButtonConfirm";
    [SerializeField] private string uiCancelSfx = "UIButtonCancel";
    [SerializeField] private string uiOpenPanelSfx = "UIOpenPanel";
    [SerializeField] private string uiClosePanelSfx = "UIClosePanel";

    private LoadMenu loadMenu;
    private UI_Options optionsUI;
    private bool isTransitioning = false;

    private UI_SaveLoadPanel uiSaveLoad;
    private bool saveLoadHooked = false;

    private CanvasGroup mainPanelCG;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);

        if (mainPanel != null) mainPanel.SetActive(true);
        if (loadPanel != null) loadPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

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

        AddHoverSfx(playButton);
        AddHoverSfx(loadButton);
        AddHoverSfx(optionsButton);
        AddHoverSfx(quitButton);

        SaveSystem.autoUnloadAdditiveScenes = true;
        SaveSystem.debug = Debug.isDebugBuild;

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }

        if (mainPanel != null)
        {
            mainPanelCG = mainPanel.GetComponent<CanvasGroup>();
            if (mainPanelCG == null) mainPanelCG = mainPanel.AddComponent<CanvasGroup>();

            mainPanelCG.interactable = true;
            mainPanelCG.blocksRaycasts = true;
        }

        if (playTitleBgmOnAwake && AudioManager.instance != null && !string.IsNullOrWhiteSpace(titleBgmGroup))
        {
            AudioManager.instance.StartBGM(titleBgmGroup);
        }
    }

    private void Update()
    {
        if (rPlayer == null)
        {
            try { rPlayer = ReInput.players.GetPlayer(playerID); } catch { }
            return;
        }

        if (!rPlayer.GetButtonDown(cancelAction)) return;

        if (uiSaveLoad != null && uiSaveLoad.IsOpen)
        {
            if (uiSaveLoad.HandleCancel())
            {
                PlayUiSfx(uiCancelSfx);
                return;
            }
        }

        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            PlayUiSfx(uiCancelSfx);
            CloseControls();
            return;
        }

        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            if (optionsUI == null && optionsPanel != null)
                optionsUI = optionsPanel.GetComponentInChildren<UI_Options>(true);

            if (optionsUI != null && optionsUI.HandleCancel())
            {
                PlayUiSfx(uiCancelSfx);
                return;
            }

            PlayUiSfx(uiCancelSfx);
            CloseOptions();
            return;
        }

        if (loadPanel != null && loadPanel.activeSelf)
        {
            PlayUiSfx(uiCancelSfx);
            CloseLoad();
            return;
        }
    }

    private void AddHoverSfx(Button button)
    {
        if (button == null) return;

        var hover = button.GetComponent<UI_ButtonHoverSfx>();
        if (hover == null)
            hover = button.gameObject.AddComponent<UI_ButtonHoverSfx>();
    }

    public void OnClickPlay()
    {
        if (isTransitioning) return;

        PlayUiSfx(uiConfirmSfx);
        StartCoroutine(NewGameTransition_Co());
    }

    public void OnClickQuit()
    {
        PlayUiSfx(uiConfirmSfx);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenLoad()
    {
        if (isTransitioning) return;

        PlayUiSfx(uiOpenPanelSfx);

        EnsureActiveAncestors(loadPanel);
        loadPanel?.SetActive(true);

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
        RestoreMainMenuInteractivity();
        HookSaveLoadClosed(false);

        PlayUiSfx(uiClosePanelSfx);
    }

    public void OpenOptions()
    {
        if (optionsPanel == null || isTransitioning) return;

        PlayUiSfx(uiOpenPanelSfx);

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
        RestoreMainMenuInteractivity();

        PlayUiSfx(uiClosePanelSfx);
    }

    public void OpenControls()
    {
        if (controlsPanel == null || optionsPanel == null) return;

        controlsPanel.SetActive(true);
        PlayUiSfx(uiOpenPanelSfx);
    }

    public void CloseControls()
    {
        if (controlsPanel == null) return;

        controlsPanel.SetActive(false);
        PlayUiSfx(uiClosePanelSfx);
    }

    private IEnumerator NewGameTransition_Co()
    {
        isTransitioning = true;
        SetMenuInteractable(false);

        if (wipeAllSlotsOnNewGame)
        {
            WipeAllSaveSlotMetadata();
        }

        SaveSystem.ResetGameState();
        PlayerPrefs.DeleteKey(SaveSystem.LastSavedGameSlotPlayerPrefsKey);
        PlayerPrefs.Save();

        yield return StartCoroutine(LocalFadeGuard_Co(show: true, duration: fadeDuration));

        if (AudioManager.instance != null)
        {
            if (!string.IsNullOrWhiteSpace(gameplayBgmGroup))
                AudioManager.instance.StartBGM(gameplayBgmGroup);
            else
                AudioManager.instance.StopBGM();
        }

        PlayTimeTracker.ResetAndStart();
        PixelCrushers.SaveSystem.sceneLoaded += OnFirstGameplayLoaded_StartTimer;

        SaveSystem.RestartGame(firstLevelSceneName);
    }

    private void WipeAllSaveSlotMetadata()
    {
        int slotCount = 4; // match your real slot count

        for (int i = 0; i < slotCount; i++)
        {
            PlayerPrefs.DeleteKey($"SaveSlot_{i}_scene");
            PlayerPrefs.DeleteKey($"SaveSlot_{i}_sceneDisplay");
            PlayerPrefs.DeleteKey($"SaveSlot_{i}_playSeconds");
            PlayerPrefs.DeleteKey($"SaveSlot_{i}_time");
            PlayerPrefs.DeleteKey($"SaveSlot_{i}_exists");
            PlayerPrefs.DeleteKey($"SaveSlot_{i}_musicGroup");
        }

        PlayerPrefs.Save();
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
        loadPanel?.SetActive(false);
        RestoreMainMenuInteractivity();
        HookSaveLoadClosed(false);

        PlayUiSfx(uiClosePanelSfx);
    }

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
        if (mainButtonsRoot != null)
            mainButtonsRoot.SetActive(visible);
    }

    private void HideMainMenuInteractivity()
    {
        SetMainButtonsVisible(false);

        if (mainPanelCG != null)
        {
            mainPanelCG.interactable = false;
            mainPanelCG.blocksRaycasts = false;
        }
    }

    public void CloseAllOptionPanels()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (mainPanel != null) mainPanel.SetActive(true);
        SetMainButtonsVisible(true);

        if (mainPanelCG != null)
        {
            mainPanelCG.interactable = true;
            mainPanelCG.blocksRaycasts = true;
        }

        PlayUiSfx(uiClosePanelSfx);
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
            mainPanel.SetActive(true);
    }

    private void PlayUiSfx(string soundName)
    {
        if (AudioManager.instance == null) return;
        if (string.IsNullOrWhiteSpace(soundName)) return;

        AudioManager.instance.PlayGlobalSFX(soundName);
    }
}