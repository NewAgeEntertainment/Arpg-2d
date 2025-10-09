using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Reflection;
using Rewired;

#if REWIRED
using Rewired.UI.ControlMapper;
#endif

public class UI_Options : MonoBehaviour
{
    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    private Rewired.Player rPlayer;

    [Header("Option Toggles")]
    [SerializeField] private Toggle healthBarToggle;
    [SerializeField] private Toggle manaBarToggle;

    [Header("Controls (Rewired Control Mapper)")]
    [SerializeField] private Button controlsButton;

#if REWIRED
    [Tooltip("Existing ControlMapper in scene (optional).")]
    [SerializeField] private ControlMapper controlMapper;

    [Tooltip("Prefab with ControlMapper (used if none found in scene).")]
    [SerializeField] private GameObject controlMapperPrefab;

    [Tooltip("UI element to reselect when Options is re-shown (Title screen path).")]
    [SerializeField] private Selectable defaultSelectable;
#endif

    [Header("Behaviour")]
    [Tooltip("In-game: after mapper closes, return to UI main menu instead of back to Options.")]
    [SerializeField] private bool returnToUIMenuWhenMapperCloses = true;

    private CanvasGroup cg;
    private Player player; // gameplay Player (null on title screen)

    // Cache a scene-level TitleMenuManager (it may be a sibling, not a parent)
    private TitleMenuManager titleMenuManagerScene;

    private void Awake()
    {
        player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (healthBarToggle != null) healthBarToggle.onValueChanged.AddListener(OnHealthToggleChanged);
        if (manaBarToggle != null) manaBarToggle.onValueChanged.AddListener(OnManaToggleChanged);
        if (controlsButton != null) controlsButton.onClick.AddListener(OnClickOpenControlMapper);

        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        TryCacheRewired();
        CacheTitleMenuManager();
    }

    private void OnEnable()
    {
#if REWIRED
        HookMapperEvents(true);
#endif
        CacheTitleMenuManager(); // scene could have changed
    }

    private void OnDisable()
    {
#if REWIRED
        HookMapperEvents(false);
#endif
    }

    private void Update()
    {
        if (rPlayer == null) { TryCacheRewired(); return; }
        if (rPlayer.GetButtonDown(cancelAction)) HandleCancel();
    }

    // ---------- Public API ----------

    public void OpenOptions()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        BringToFront();
        SetInteractable(true);
        Debug.Log("[UI_Options] Options panel opened.");
    }

    public bool HandleCancel()
    {
        // Cancel from Options should: on Title → close *all* option panels and show main;
        // in-game → go back to UI main menu.
        if (IsTitleScreenContext())
        {
            ReturnToTitleMainAndCloseSelf();
        }
        else
        {
            // In-game
            HideOptionsForMapper(true);
            UI.Instance?.OpenMainMenuDirect();
            gameObject.SetActive(false);
        }
        return true;
    }

    public void ClosePanel()
    {
        // Generic close (TitleMenuManager.CloseOptions also calls this path)
        SetInteractable(false);
        gameObject.SetActive(false);
        Debug.Log("[UI_Options] Closed.");
    }

    // ---------- Toggles ----------

    private void OnHealthToggleChanged(bool isOn)
    {
        if (player != null && player.health != null)
            player.health.EnableHealthBar(isOn);
    }

    private void OnManaToggleChanged(bool isOn)
    {
        if (player != null && player.mana != null)
            player.mana.EnableManaBar(isOn);
    }

    public bool IsOpen
    {
        get
        {
            // active + interactable (so it doesn't count while hidden behind ControlMapper)
            if (!gameObject.activeInHierarchy) return false;
            return cg == null ? true : cg.blocksRaycasts;
        }
    }


    // ---------- Control Mapper ----------

    private void OnClickOpenControlMapper()
    {
#if !REWIRED
        Debug.LogError("[UI_Options] Rewired not present. Cannot open Control Mapper.");
        return;
#else
        var cm = EnsureControlMapper();
        if (cm == null)
        {
            Debug.LogError("[UI_Options] ControlMapper not found and no prefab assigned.");
            return;
        }

        // Hide Options while mapper is visible
        HideOptionsForMapper(true);

        try { cm.Open(); }
        catch
        {
            var m = cm.GetType().GetMethod("Open", BindingFlags.Public | BindingFlags.Instance);
            if (m != null) m.Invoke(cm, null);
            else cm.gameObject.SetActive(true);
        }
#endif
    }

#if REWIRED
    private ControlMapper EnsureControlMapper()
    {
        if (controlMapper != null) return controlMapper;

#if UNITY_2022_1_OR_NEWER
        controlMapper = FindFirstObjectByType<ControlMapper>(FindObjectsInactive.Include);
#else
        controlMapper = FindObjectOfType<ControlMapper>(true);
#endif
        if (controlMapper != null) return controlMapper;

        if (controlMapperPrefab != null)
        {
            var go = Instantiate(controlMapperPrefab);
            controlMapper = go.GetComponentInChildren<ControlMapper>(true);
        }
        return controlMapper;
    }

    private void HookMapperEvents(bool hook)
    {
        var cm = EnsureControlMapper();
        if (cm == null) return;

        if (hook)
        {
            cm.ScreenOpenedEvent += OnMapperOpened;
            cm.ScreenClosedEvent += OnMapperClosed;
        }
        else
        {
            cm.ScreenOpenedEvent -= OnMapperOpened;
            cm.ScreenClosedEvent -= OnMapperClosed;
        }
    }

    private void OnMapperOpened()
    {
        HideOptionsForMapper(true);
    }

    private void OnMapperClosed()
    {
        if (IsTitleScreenContext())
        {
            // Title screen: close ALL option panels and return to main
            ReturnToTitleMainAndCloseSelf();
            return;
        }

        // In-game: go to UI main menu if desired
        if (returnToUIMenuWhenMapperCloses)
        {
            UI.Instance?.OpenMainMenuDirect();
            gameObject.SetActive(false);
        }
        else
        {
            // Re-open Options
            HideOptionsForMapper(false);
            BringToFront();
            if (defaultSelectable != null && EventSystem.current != null)
                StartCoroutine(SelectNextFrame(defaultSelectable.gameObject));
        }
    }
#endif

    // ---------- Helpers ----------

    private void TryCacheRewired()
    {
        try { rPlayer = ReInput.players.GetPlayer(playerID); } catch { }
    }

    private void CacheTitleMenuManager()
    {
#if UNITY_2022_1_OR_NEWER
        titleMenuManagerScene = FindFirstObjectByType<TitleMenuManager>(FindObjectsInactive.Include);
#else
        titleMenuManagerScene = FindObjectOfType<TitleMenuManager>(true);
#endif
    }

    private bool IsTitleScreenContext()
    {
        // If there is a TitleMenuManager anywhere in the scene, we consider this the title screen
        return titleMenuManagerScene != null;
    }

    // UI_Options.cs
    private void ReturnToTitleMainAndCloseSelf()
    {
        // Don't block clicks during the switch
        HideOptionsForMapper(true);

        // If we’re on the title screen, force-close all option panels
        if (titleMenuManagerScene != null)
        {
            titleMenuManagerScene.CloseAllOptionPanels();
        }

        // Hide this options instance (there may be a second Options under gameplay UI)
        gameObject.SetActive(false);
    }


    private void HideOptionsForMapper(bool hide)
    {
        if (cg != null)
        {
            cg.alpha = hide ? 0f : 1f;
            cg.interactable = !hide;
            cg.blocksRaycasts = !hide;
        }
        else
        {
            gameObject.SetActive(!hide);
        }
    }

    private void SetInteractable(bool on)
    {
        if (cg != null)
        {
            cg.alpha = 1f;          // keep visible unless explicitly hiding
            cg.interactable = on;
            cg.blocksRaycasts = on;
        }
    }

    private void BringToFront()
    {
        transform.SetAsLastSibling();
    }

    private IEnumerator SelectNextFrame(GameObject go)
    {
        yield return null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }
}
