// Assets/Scripts/Title/TitleMenuManager.cs
using UnityEngine;
using UnityEngine.UI;
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

    private LoadMenu loadMenu;
    private OptionsMenu optionsMenu;

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

        // (Optional) If you want a fade/transition, add SceneTransitionManager under Save System in this scene.
        SaveSystem.autoUnloadAdditiveScenes = true;
        SaveSystem.debug = Debug.isDebugBuild;
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
        // Reset runtime state and optionally delete every slot, then go to first level.
        if (wipeAllSlotsOnNewGame) SaveUtility.DeleteAllSlots();
        SaveSystem.ResetGameState();
        SaveSystem.RestartGame(firstLevelSceneName);
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
        if (loadPanel == null) return;
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
        if (optionsPanel == null) return;
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
}

