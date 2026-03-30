using PixelCrushers;
using PixelCrushers.QuestMachine.Wrappers;
using Rewired;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    // -------- Singleton Guard --------
    public static UI Instance { get; private set; }

    #region Components
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public Inventory_Item hoveredItem;

    // Cache of panels that actually exist at runtime, in cycle order.
    private UIPanelKind[] _panelCycleCache;

    [Header("Popup References")]
    public UI_LevelUpPopup levelUpPopup;

    public UI_StatToolTip statToolTip { get; private set; }

    // Only allow assign-preview to close when we intentionally exit (Esc/Back/Done)
    private bool _allowAssignPreviewHide = false;

    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Root container for ALL UI")]
    [SerializeField] private GameObject uiRoot;

    [Header("Main UI Panels")]
    [SerializeField] private UI_Inventory inventoryUI;
    public UI_Inventory InventoryUI => inventoryUI;

    [SerializeField] private UI_SkillTree skillTreeUI;
    public UI_SkillTree SkillTreeUI => skillTreeUI;

    [SerializeField] private UI_StatusPanel statusPanel;
    public UI_StatusPanel StatusPanel => statusPanel;

    [SerializeField] private UI_Conquest conquestUI;
    [SerializeField] private CharacterProfileSO defaultConquestProfile; // optional
    [SerializeField] private Entity_Stats defaultConquestStats;         // optional

    [SerializeField] private UI_SaveLoadPanel saveLoadPanel;   // assign in Inspector (or lazy find)
    private bool isSaveOpen = false;

    [SerializeField] private UI_Storage storageUI;
    public UI_Storage StorageUI => storageUI;

    [SerializeField] private UI_Merchant merchantUI;
    public UI_Merchant MerchantUI => merchantUI;

    [SerializeField] private UI_Craft craftUI;
    public UI_Craft CraftUI => craftUI;

    [SerializeField] private UI_EquipmentInventory equipmentInventoryPanel;

    public UI_InGame inGameUI;
    public UI_Options optionsUI { get; private set; }

    public UI_PlayerExpBar playerExpBar;
    public UI_HealthBar playerHealthBar;
    public UI_ManaBar playerManaBar;

    [Header("Main Menu Panel")]
    [SerializeField] private GameObject mainMenuPanel;

    // UI.cs
    [SerializeField] private float skillRefreshRetrySeconds = 0.1f;
    [SerializeField] private int skillRefreshMaxTries = 10;
    private Coroutine _skillRefreshCo;


    // ===== Quest Journal (Quest Machine) =====
    [Header("Quest Journal (Quest Machine)")]
    [Tooltip("Optional parent GameObject that contains the UnityUIQuestJournalUI; toggled with the journal.")]
    [SerializeField] private GameObject questJournalRoot;
    [Tooltip("Assign the UnityUIQuestJournalUI (wrapper) component here.")]
    [SerializeField] private UnityUIQuestJournalUI questJournalUI;
    [Tooltip("If true, re-open the main menu panel after closing the journal.")]
    [SerializeField] private bool reopenMainMenuAfterJournalClose = true;


    // === UI Theme – Button Colors ===
    [Header("UI Theme – Buttons")]
    [SerializeField] private Color btnNormal = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color btnHighlighted = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color btnSelected = new Color(1f, 0.85f, 0.35f, 1f); // <- selected via gamepad/keys
    [SerializeField] private Color btnPressed = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color btnDisabled = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private float btnFade = 0.08f;
    [SerializeField] private float btnMultiplier = 1f;

    private bool _openedJournalFromMainMenu = false;

    private bool isQuestJournalOpen = false;

    // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    // New: Menu Health/Mana bindings (drag your MENU UI widgets here, not the HUD ones)
    [Header("Main Menu - Health & Mana (drag from Menu UI)")]
    [SerializeField] private Slider menuHealthSlider;
    [SerializeField] private TMP_Text menuHealthText;
    [SerializeField] private Slider menuManaSlider;
    [SerializeField] private TMP_Text menuManaText;

    // NEW: Location & Time Played (drag the top bar TMPs here)
    [Header("Top Bar - Location & Time")]
    [SerializeField] private TMP_Text locationLabel;     // e.g., "Location: Forest Area 0"
    [SerializeField] private TMP_Text timePlayedLabel;   // e.g., "Time Played: 00:00"

    [Header("Menu Bars Update (fallback polling)")]
    [SerializeField] private float menuPollInterval = 0.1f;
    // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string openSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string openInventoryAction = "OpenInventory";
    [SerializeField] private string openEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string openConquestAction = "OpenConquest";
    [SerializeField] private string openOptionsAction = "OpenOptions";
    [SerializeField] private string openMainMenuAction = "OpenMainMenu";
    [SerializeField] private string cancelAction = "UICancel";
    [SerializeField] private string openSavePanelAction = "OpenSavePanel"; // optional

    [Header("Menus")]
    [SerializeField] private GameObject outsideMenuPanel;   // shown when book is CLOSED (main menu)
    [SerializeField] private GameObject insideMenuPanel;    // shown when book is OPEN (tabs/buttons inside book)


    [Header("Time Played – Unscaled")]
    [SerializeField] private bool timePlayedUsesUnscaled = true;  // turn on to count while paused
    private int _realSecondsPlayed = -1;
    private Coroutine _realTimeTickerCo;

    // Save panel return behavior
    private bool _saveOpenedFromCycle = false;
    private UIPanelKind _panelBeforeSave = UIPanelKind.Inventory;


    private bool _suppressSaveClosedHandler = false;


    // Exact prefix you want to display
    private const string TimePlayedPrefix = "played Time ";

    // Cache the last full string we rendered (used to override stray writes)
    private string _timePlayedRendered = null;


    // (Optional) You can add a mapped action to toggle the journal if you want:
    //[SerializeField] private string toggleQuestJournalAction = "ToggleQuestJournal";

    private Rewired.Player player;

    // --- Rewired cache for map switching (Gameplay/UI) ---
    private Rewired.Player rplayerUI;
    private void CacheRewired()
    {
        if (rplayerUI == null)
        {
            try { rplayerUI = ReInput.players.GetPlayer(playerID); }
            catch { }
        }
    }

    private bool isInventoryOpen = false;
    private bool isSkillTreeOpen = false;
    private bool isEquipmentOpen = false;
    private bool isStatusPanelOpen = false;
    private bool isConquestOpen = false;
    private bool _journalOpenedFromMainMenu = false;
    private bool isOptionsOpen = false;
    private bool isStorageOpen = false;
    private bool isMerchantOpen = false;
    private bool isCraftOpen = false;
    private bool isStatusSelectionMode = false;
    private bool isEquipmentSelectionMode = false;

    // track event subscription so we don’t double-subscribe when scenes change
    private bool goldSubscribed = false;
    private Inventory_Player cachedInv;
    #endregion



    public bool IsStatusSelectionModeActive() => isStatusSelectionMode;
    public bool IsEquipmentSelectionModeActive()
    {
        Debug.Log($"[UI] IsEquipmentSelectionModeActive() -> {isEquipmentSelectionMode}");
        return isEquipmentSelectionMode;
    }


    // ======================= Title Screen / Return-to-Title settings =======================
    [Header("Title Screen")]
    [SerializeField] private string titleSceneName = "Title Screen"; // set to your title scene name
    [Tooltip("DDOL objects to keep when returning to Title (e.g., SaveSystem, Rewired Input Manager, Dialogue Manager if needed).")]
    [SerializeField] private GameObject[] ddolEssentials;

    private const int SuspendSlot = -1;              // temp "suspend" slot
    private const string SuspendKey = "suspend_exists";
    // =======================================================================================

    // ====== Menu bars internal (player stats reflection + events) ==========================
    private Component statsComp;                 // Player_Stats / Entity_Stats / Player
    private PropertyInfo pCurHP, pMaxHP, pCurMP, pMaxMP;
    private EventInfo eHP, eMP;
    private Coroutine menuPollCo;

    // ============================ Assign-preview (hide the tree) ===========================
    private bool _assignPreviewActive = false;
    private bool _sexUIOpenedByPreview = false;
    private bool _combatHUDActivatedByPreview = false;

    // we now hide/show the tree instead of fading it
    private bool _skillTreeWasActive = false;

    public bool IsAssignPreviewActive => _assignPreviewActive;


    private CursorLockMode _prevLockMode;
    private bool _prevCursorVisible;

    [Header("Book UI")]
    [SerializeField] private BookOpenManager bookUI;

    // -------------------- ADDED: Panel Switching (Rewired) --------------------
    [SerializeField] private string nextPanelAction = "NextUIPanel";
    [SerializeField] private string prevPanelAction = "PrevUIPanel";

    public enum UIPanelKind
    {
        Inventory,
        SkillTree,
        Equipment,
        Status,
        Conquest,
        QuestJournal,
        Options,
        Save
    }



    [SerializeField]
    private UIPanelKind[] panelCycleOrder = new UIPanelKind[]
{
    UIPanelKind.Inventory,
    UIPanelKind.SkillTree,
    UIPanelKind.Equipment,
    UIPanelKind.Status,
    UIPanelKind.Conquest,
    UIPanelKind.QuestJournal,
    UIPanelKind.Options,
    UIPanelKind.Save,
};



    private void Awake()
    {
        // ---- Singleton/DDOL guard ----
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ---- Cache child components ----
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>(true);
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>(true);
        statToolTip = GetComponentInChildren<UI_StatToolTip>(true);
        craftUI = GetComponentInChildren<UI_Craft>(true);
        merchantUI = GetComponentInChildren<UI_Merchant>(true);
        storageUI = GetComponentInChildren<UI_Storage>(true);
        inGameUI = GetComponentInChildren<UI_InGame>(true);
        optionsUI = GetComponentInChildren<UI_Options>(true);

        // Auto-disable popup at start
        if (levelUpPopup != null) levelUpPopup.gameObject.SetActive(false);

        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
        statusPanel?.gameObject.SetActive(false);
        optionsUI?.gameObject.SetActive(false);
        storageUI?.gameObject.SetActive(false);
        merchantUI?.gameObject.SetActive(false);
        craftUI?.gameObject.SetActive(false);
        mainMenuPanel?.SetActive(false);
        uiRoot?.SetActive(false);


        // Auto-find main menu panel if not assigned
        if (mainMenuPanel == null)
        {
            // Try common names you’ve used
            var found = GameObject.Find("MenuPanel") ?? GameObject.Find("MainMenuPanel");

            if (found != null)
                mainMenuPanel = found;
            else
                Debug.LogWarning("[UI] mainMenuPanel is not assigned and could not be found (MenuPanel/MainMenuPanel).");
        }

        // Journal off by default
        if (questJournalUI != null) questJournalUI.gameObject.SetActive(false);
        if (questJournalRoot != null) questJournalRoot.SetActive(false);

        AutoFindUIPanelsIfMissing();
        EnsureValidPanelCycle();
    }


    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
        skillTreeUI?.UnlockDefaultSkills();

        // Make sure we see the current gold immediately in a fresh scene
        TrySubscribeGold();

        // Bind to player stats (for menu bars)
        BindStats(AutoFindStats());

        // Kick a one-time HUD/slots refresh when starting from title
        StartCoroutine(RefreshHUDOnceCo());

        // Top bar immediate fill:
        UpdateLocationLabel();
        UpdateTimePlayedLabelImmediate();
    }

    private void OnEnable()
    {
        // NEW: hook sceneLoaded so AfterSceneLoad_Co runs after any load
        SceneManager.sceneLoaded += OnSceneLoaded_UIRefresh;

        // Re-arm gold subscription in case of scene reload
        TrySubscribeGold();

        // Rebind stats after scene loads
        PlayTimeTracker.OnSecondChanged -= HandleSecondTick;

        if (timePlayedUsesUnscaled)
        {
            EnsureRealSecondsInit();
            if (_realTimeTickerCo == null)
                _realTimeTickerCo = StartCoroutine(RealTimeTicker_Co());
        }
        else
        {
            PlayTimeTracker.OnSecondChanged += HandleSecondTick;
        }

        SceneManager.activeSceneChanged += HandleActiveSceneChanged;

        UpdateTimePlayedLabelImmediate();
        UpdateLocationLabel();
    }

    private void OnDisable()
    {
        // NEW: unhook (you already had this line – keep it)
        SceneManager.sceneLoaded -= OnSceneLoaded_UIRefresh;

        UnsubscribeGold();
        UnhookStatEvents();
        StopMenuPoll();

        if (timePlayedUsesUnscaled)
        {
            if (_realTimeTickerCo != null)
            {
                StopCoroutine(_realTimeTickerCo);
                _realTimeTickerCo = null;
            }
        }
        else
        {
            PlayTimeTracker.OnSecondChanged -= HandleSecondTick;
        }

        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void LateUpdate()
    {
        SyncMenusToBook();
        //WatchForPanelsThatClosedThemselves();
    }


    private void OnDestroy()
    {
        UnsubscribeGold();
        UnhookStatEvents();
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded_UIRefresh(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AfterSceneLoad_Co());
        StartCoroutine(RefreshHUDOnceCo());
        if (_skillRefreshCo != null) StopCoroutine(_skillRefreshCo);
        _skillRefreshCo = StartCoroutine(EnsureSkillsVisibleCo());
        UpdateLocationLabel();

        // NEW: re-scan for panels and rebuild cycle on scene change
        AutoFindUIPanelsIfMissing();
        EnsureValidPanelCycle();
    }


    private IEnumerator EnsureSkillsVisibleCo()
    {
        // give spawners & tree a couple of frames
        yield return null;
        yield return null;

        for (int i = 0; i < skillRefreshMaxTries; i++)
        {
            TryRefreshSkillSlotsOnce();

            // stop as soon as at least one slot has a skill
            if (inGameUI != null && inGameUI.AnySkillSlotHasSkill())
                break;

            yield return new WaitForSecondsRealtime(skillRefreshRetrySeconds);
        }

        _skillRefreshCo = null;
    }

    

    private void TryRefreshSkillSlotsOnce()
    {
        if (inGameUI == null) return;

        if (skillTreeUI == null)
            skillTreeUI = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);

        if (skillTreeUI != null)
            inGameUI.RefreshSkillSlotsFromTree(skillTreeUI);
    }


    private IEnumerator AfterSceneLoad_Co()
    {
        // Let SaveSystem & PlayTimeTracker finish restoring values.
        yield return null;

        BindStats(AutoFindStats());

        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            ForceRefreshMenuBars();

        // 🔹 NEW: sync timer from the tracker after every scene load
        ResyncPlayTimeFromTracker();
    }



    private IEnumerator RefreshHUDOnceCo()
    {
        yield return null;

        TrySubscribeGold();
        inGameUI?.ForceRefreshFromCurrentState();

        if (skillTreeUI != null)
            inGameUI?.RefreshSkillSlotsFromTree(skillTreeUI);

        // ⬇️ New: after player is spawned, force skill slots to use the live manager values
        var mgr = FindFirstObjectByType<Player_SkillManager>(FindObjectsInactive.Include);
        if (inGameUI != null && mgr != null)
        {
            var allSlots = inGameUI.GetComponentsInChildren<UI_SkillSlot>(true);
            foreach (var s in allSlots) s.RefreshText(mgr);
        }
    }


    private void TrySubscribeGold()
    {
        if (goldSubscribed) return;

        cachedInv = FindFirstObjectByType<Inventory_Player>();
        if (cachedInv != null)
        {
            cachedInv.OnGoldChanged += UpdateGoldUI;
            goldSubscribed = true;
            UpdateGoldUI(cachedInv.gold); // show current gold immediately
        }
    }

    private void UnsubscribeGold()
    {
        if (!goldSubscribed) return;
        if (cachedInv != null) cachedInv.OnGoldChanged -= UpdateGoldUI;
        cachedInv = null;
        goldSubscribed = false;
    }

    private void Update()
    {
        if (player == null) return;

        if (player.GetButtonDown(openSkillTreeAction)) OpenSkillTree();
        if (player.GetButtonDown(openInventoryAction)) OpenInventory();
        if (player.GetButtonDown(openEquipmentAction)) OpenEquipment();
        if (player.GetButtonDown(openOptionsAction)) OpenOptions();
        if (player.GetButtonDown(openMainMenuAction)) OpenMainMenuDirect();
        if (player.GetButtonDown(cancelAction)) HandleBackAction();
        // if (player.GetButtonDown(toggleQuestJournalAction)) ToggleQuestJournalFromUI();

        // optional: hotkey for save panel
        if (player.GetButtonDown(openSavePanelAction)) OpenSavePanel();

        // -------------------- ADDED: Cycle between panels --------------------
        if (player.GetButtonDown(nextPanelAction)) SwitchUIPanel(+1);
        if (player.GetButtonDown(prevPanelAction)) SwitchUIPanel(-1);
        // --------------------------------------------------------------------
    }

    //private void LateUpdate()
    //{
    //    if (!timePlayedLabel) return;
    //    if (!string.IsNullOrEmpty(_timePlayedRendered) && timePlayedLabel.text != _timePlayedRendered)
    //    {
    //        // Re-assert our authoritative text (prevents “numbers-only” overrides)
    //        timePlayedLabel.text = _timePlayedRendered;
    //    }
    //}


    public static UI EnsureExists(UI prefab)
    {
        if (Instance != null) return Instance;
        var ui = Instantiate(prefab);
        ui.name = prefab != null ? prefab.name : "UI";
        return ui;
    }

    public void UpdateGoldUI(int newGoldAmount)
    {
        if (goldText != null)
            goldText.text = $"{newGoldAmount:N0} G";
    }

    private void OpenPanelWithBook(System.Action openPanel)
    {
        if (bookUI == null)
        {
            openPanel?.Invoke();
            return;
        }

        bookUI.PlayOpenThen(() => openPanel?.Invoke());
    }



    #region Open/Close Panels

    public void OpenInventory()
    {
        if (outsideMenuPanel != null && outsideMenuPanel.activeSelf)
        {
            OpenPanelFromOutsideMenu(() =>
            {
                isInventoryOpen = true;
                inventoryUI?.gameObject.SetActive(true);
                inventoryUI?.OpenInventory();
            });
            return;
        }

        // otherwise switching inside book
        SwitchPanelWithPageTurn(() =>
        {
            isInventoryOpen = true;
            inventoryUI?.gameObject.SetActive(true);
            inventoryUI?.OpenInventory();
        }, turnRight: false);
    }



    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryUI?.CloseInventory();
        OpenMainMenuDirect();
    }




    public void OpenSkillTree()
    {
        // ✅ hide main menu FIRST (same frame)
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // keep UI root on + enter UI mode
        EnsureUIRootIsActive();

        // turn off other panels (but DON'T touch the book object)
        CloseAllPanels();

        // ✅ now play the book open, then show skill tree
        OpenPanelWithBook(() =>
        {
            isSkillTreeOpen = true;

            skillTreeUI?.gameObject.SetActive(true);
            // (Optional) if you have a method to refresh/force update on open, call it here.
            // skillTreeUI?.RefreshUI();
        });
    }


    public void CloseSkillTree()
    {
        isSkillTreeOpen = false;
        if (skillTreeUI != null) skillTreeUI.gameObject.SetActive(false);
        OpenMainMenuDirect();
    }



    public void OpenEquipment()
    {
        Debug.Log("[UI] OpenEquipment() CALLED - waiting for character selection");

        isEquipmentSelectionMode = true;
        isStatusSelectionMode = false;
        isEquipmentOpen = false;

        Debug.Log($"[UI] After OpenEquipment -> isEquipmentSelectionMode={isEquipmentSelectionMode}");

        if (equipmentInventoryPanel != null)
            equipmentInventoryPanel.gameObject.SetActive(false);
    }




    public void SelectEquipmentCharacter(GameObject character)
    {
        Debug.Log(
            $"[UI] SelectEquipmentCharacter -> " +
            $"character={(character != null ? character.name : "NULL")}, " +
            $"isEquipmentSelectionMode={isEquipmentSelectionMode}"
        );

        if (character == null || equipmentInventoryPanel == null)
            return;

        if (!isEquipmentSelectionMode)
        {
            Debug.Log("[UI] Early return: equipment selection mode is not active.");
            return;
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        EnsureUIRootIsActive();
        CloseAllPanels();

        OpenPanelWithBook(() =>
        {
            Debug.Log($"[UI] Opening equipment panel for {character.name}");

            isEquipmentSelectionMode = false;
            isEquipmentOpen = true;

            equipmentInventoryPanel.gameObject.SetActive(true);
            equipmentInventoryPanel.OpenForCharacter(character);
        });
    }


    public void CloseEquipment()
    {
        isEquipmentOpen = false;
        equipmentInventoryPanel?.Close();
        OpenMainMenuDirect();
    }




    public void OpenOptions()
    {
        // If we're already inside the book UI, do NOT PlayOpenThen / Close book.
        if (bookUI != null && bookUI.gameObject.activeInHierarchy && !bookUI.IsInClosedIdle())
        {
            SwitchToPanel(UIPanelKind.Options);
            return;
        }

        // Otherwise opening from outside/main menu: normal flow.
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        EnsureUIRootIsActive();
        CloseAllPanels();

        OpenPanelWithBook(() =>
        {
            isOptionsOpen = true;

            if (optionsUI != null)
            {
                optionsUI.gameObject.SetActive(true);
                optionsUI.OpenOptions();
                ApplyButtonTheme(optionsUI.transform);
            }

            EnterUIMode();
        });
    }



    public void CloseOptions()
    {
        isOptionsOpen = false;
        optionsUI?.ClosePanel();
        OpenMainMenuDirect();
    }




    public void OpenStatusPanel()
    {
        Debug.Log("[UI] OpenStatusPanel() CALLED - waiting for character selection");

        isStatusSelectionMode = true;
        isEquipmentSelectionMode = false;
        isStatusPanelOpen = false;

        if (statusPanel != null)
            statusPanel.gameObject.SetActive(false);
    }


    public void CloseStatusPanel()
    {
        isStatusPanelOpen = false;
        statusPanel?.ClosePanel();
        OpenMainMenuDirect();
    }



    public void OpenConquestPanel()
    {
        // ✅ hide main menu FIRST
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        EnsureUIRootIsActive();
        CloseAllPanels();

        if (conquestUI == null)
        {
            Debug.LogWarning("[UI] Conquest UI not assigned.");
            return;
        }

        OpenPanelWithBook(() =>
        {
            conquestUI.gameObject.SetActive(true);
            conquestUI.OpenRosterFirst();
            isConquestOpen = true;
        });
    }


    public void CloseConquest()
    {
        isConquestOpen = false;
        conquestUI?.Close();
        OpenMainMenuDirect();
    }

    // ===== Save Panel =====

    public void OpenSavePanel()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        EnsureUIRootIsActive();
        CloseAllPanels();

        if (saveLoadPanel == null)
            saveLoadPanel = FindFirstObjectByType<UI_SaveLoadPanel>(FindObjectsInactive.Include);

        if (saveLoadPanel == null)
        {
            Debug.LogError("[UI] UI_SaveLoadPanel not found in scene.");
            return;
        }

        saveLoadPanel.Closed -= OnSaveLoadClosed;
        saveLoadPanel.Closed += OnSaveLoadClosed;

        var ctx = (mainMenuPanel != null && mainMenuPanel.activeSelf)
            ? UI_SaveLoadPanel.OpenContext.TitleMenu
            : UI_SaveLoadPanel.OpenContext.PauseMenu;

        isSaveOpen = true;

        // ✅ If book is already open (page-turn path), do NOT PlayOpenThen.
        if (IsBookOpenAndReady())
        {
            saveLoadPanel.gameObject.SetActive(true);
            saveLoadPanel.OpenForSave(ctx);
            ApplyButtonTheme(saveLoadPanel.transform);
            EnterUIMode();
            return;
        }

        // Otherwise opening from outside/main menu: normal open-book flow.
        OpenPanelWithBook(() =>
        {
            saveLoadPanel.gameObject.SetActive(true);
            saveLoadPanel.OpenForSave(ctx);
            ApplyButtonTheme(saveLoadPanel.transform);
            EnterUIMode();
        });
    }




    public void CloseSavePanel()
    {
        // mark state first
        isSaveOpen = false;

        // Close the save UI WITHOUT triggering Closed event handling
        if (saveLoadPanel != null && saveLoadPanel.IsOpen)
        {
            _suppressSaveClosedHandler = true;
            saveLoadPanel.Closed -= OnSaveLoadClosed;
            saveLoadPanel.ClosePanel();   // this will invoke Closed, but suppressed + unsubscribed
            _suppressSaveClosedHandler = false;
        }

        // ✅ Single path: this will play book close then show main menu
        OpenMainMenuDirect();
    }







    private void OnSaveLoadClosed(UI_SaveLoadPanel.OpenContext ctx)
    {
        if (_suppressSaveClosedHandler)
            return;

        isSaveOpen = false;

        if (ctx == UI_SaveLoadPanel.OpenContext.GameOver)
        {
            UI_GameOver.ShowStatic();
            return;
        }

        // ✅ Single authority handles the book close
        OpenMainMenuDirect();
    }





    private void ShowConfirm(string message, System.Action onYes, System.Action onNo)
    {
        onYes?.Invoke();
    }


    private void ShowSavePanelImmediate(UI_SaveLoadPanel.OpenContext ctx)
    {
        if (saveLoadPanel == null)
            saveLoadPanel = FindFirstObjectByType<UI_SaveLoadPanel>(FindObjectsInactive.Include);

        if (saveLoadPanel == null)
        {
            Debug.LogError("[UI] UI_SaveLoadPanel not found in scene.");
            return;
        }

        saveLoadPanel.Closed -= OnSaveLoadClosed;
        saveLoadPanel.Closed += OnSaveLoadClosed;

        isSaveOpen = true;

        saveLoadPanel.gameObject.SetActive(true);
        saveLoadPanel.OpenForSave(ctx);
        ApplyButtonTheme(saveLoadPanel.transform);
        EnterUIMode();
    }



    public void OpenCraft()
    {
        isCraftOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (storageUI != null)
        {
            storageUI.gameObject.SetActive(false);
            isStorageOpen = false;
        }

        if (craftUI != null)
        {
            craftUI.gameObject.SetActive(true);
            Debug.Log("[UI] Craft UI opened");
        }
    }

    public void CloseCraft()
    {
        isCraftOpen = false;

        if (craftUI != null)
        {
            craftUI.gameObject.SetActive(false);
            Debug.Log("[UI] Craft UI closed");
        }

        CheckStopPlayerControls();
    }

    // UI.cs
    public void OnMerchantPanelClosed()
    {
        isMerchantOpen = false;   // <- clear the UI's idea of "shop open"
        CheckStopPlayerControls(); // will unpause & swap maps if nothing else is open
    }


    public void OpenMerchant(Inventory_Merchant merchant, Inventory_Player playerInv)
    {
        EnsureUIRootIsActive();
        CloseAllPanels();

        if (MerchantUI != null)
        {
            MerchantUI.gameObject.SetActive(true);
            MerchantUI.SetUpMerchantUI(merchant, playerInv);
            isMerchantOpen = true;
        }

        // 🔁 Switch Rewired maps to UI and (optionally) pause
        EnterUIMode(); // disables "Gameplay" map, enables "UI", sets Time.timeScale = 0

        Debug.Log("[UI] Merchant UI opened");
    }

    public void CloseMerchant()
    {
        isMerchantOpen = false;

        if (merchantUI != null)
        {
            merchantUI.gameObject.SetActive(false);
            Debug.Log("[UI] Merchant panel closed");
        }

        // Let this decide if gameplay should unpause / maps should swap.
        CheckStopPlayerControls();
    }


    public void OpenMainMenuDirect()
    {
        // ✅ don't stack close animations
        if (bookUI != null && bookUI.IsBusy())
            return;

        EnsureUIRootIsActive();
        CloseAllPanels();

        mainMenuPanel?.SetActive(false);

        if (bookUI == null)
        {
            ShowMainMenuNow();
            return;
        }

        bookUI.gameObject.SetActive(true);

        // If already closed, don't re-play close
        if (bookUI.IsInClosedIdle())
        {
            ShowMainMenuNow();
            return;
        }

        bookUI.PlayCloseThen(() =>
        {
            bookUI.ShowClosedIdle();
            ShowMainMenuNow();
        });
    }


    private void ShowMainMenuNow()
    {
        mainMenuPanel?.SetActive(true);
        ApplyButtonTheme(mainMenuPanel?.transform);
        ForceRefreshMenuBars();
        StartMenuPollIfNeeded();
        EnterUIMode();

        // Also keep outside/inside menus correct:
        SyncMenusToBook();
    }




    private void SwitchPanelWithPageTurn(System.Action showPanel, bool turnRight)
    {
        if (bookUI != null && bookUI.IsBusy())
            return;

        EnsureUIRootIsActive();

        // ✅ If we're switching pages, we don't want SavePanel's Closed event to close the book.
        _suppressSaveClosedHandler = true;
        CloseAllPanels();
        _suppressSaveClosedHandler = false;

        if (bookUI == null)
        {
            showPanel?.Invoke();
            return;
        }

        bookUI.gameObject.SetActive(true);

        if (!IsBookOpenAndReady())
        {
            bookUI.PlayOpenThen(() => showPanel?.Invoke());
            return;
        }

        if (turnRight) bookUI.PlayTurnRightThen(() => showPanel?.Invoke());
        else bookUI.PlayTurnLeftThen(() => showPanel?.Invoke());
    }




    private bool IsBookOpenAndReady()
    {
        return bookUI != null && bookUI.gameObject.activeInHierarchy && bookUI.IsInOpenIdle();
    }

    private void OpenPanelFromMainMenu(System.Action showPanel)
    {
        // called by MainMenu buttons
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        EnsureUIRootIsActive();
        CloseAllPanels();

        if (bookUI == null)
        {
            showPanel?.Invoke();
            return;
        }

        // Ensure book object is visible/active
        bookUI.gameObject.SetActive(true);

        // Open book first, THEN show panel
        bookUI.PlayOpenThen(() =>
        {
            showPanel?.Invoke();
        });
    }


    private void SyncMenusToBook()
    {
        if (bookUI == null || !bookUI.gameObject.activeInHierarchy) return;

        if (bookUI.IsBusy())
            return; // don't toggle menus mid-animation

        bool fullyClosed = bookUI.IsInClosedIdle();

        if (outsideMenuPanel != null) outsideMenuPanel.SetActive(fullyClosed);
        if (insideMenuPanel != null) insideMenuPanel.SetActive(!fullyClosed);
    }





    private void OpenPanelFromOutsideMenu(System.Action showPanel)
    {
        EnsureUIRootIsActive();

        if (outsideMenuPanel != null) outsideMenuPanel.SetActive(false);

        if (bookUI == null)
        {
            if (insideMenuPanel != null) insideMenuPanel.SetActive(true);
            showPanel?.Invoke();
            return;
        }

        bookUI.gameObject.SetActive(true);

        bookUI.PlayOpenThen(() =>
        {
            if (insideMenuPanel != null) insideMenuPanel.SetActive(true);
            showPanel?.Invoke();
            SyncMenusToBook();
        });
    }



    private int IndexOfKindInCycle(UIPanelKind k)
    {
        if (_panelCycleCache == null) return -1;
        return Array.IndexOf(_panelCycleCache, k);
    }

    private bool ShouldTurnRight(UIPanelKind from, UIPanelKind to)
    {
        int a = IndexOfKindInCycle(from);
        int b = IndexOfKindInCycle(to);
        if (a < 0 || b < 0) return true;
        return b > a; // forward in cycle => turn right
    }


    private bool TryOpenPanelByKind(UIPanelKind kind)
    {
        // IMPORTANT: do NOT call OpenInventory/OpenSkillTree here (they play book anims)
        CloseAllPanels();
        EnsureUIRootIsActive();

        switch (kind)
        {
            case UIPanelKind.Inventory:
                if (inventoryUI == null) return false;
                isInventoryOpen = true;
                inventoryUI.gameObject.SetActive(true);
                inventoryUI.OpenInventory();
                return true;

            case UIPanelKind.SkillTree:
                if (skillTreeUI == null) return false;
                isSkillTreeOpen = true;
                skillTreeUI.gameObject.SetActive(true);
                return true;

            case UIPanelKind.Equipment:
                if (equipmentInventoryPanel == null) return false;
                isEquipmentOpen = true;
                equipmentInventoryPanel.gameObject.SetActive(true);
                equipmentInventoryPanel.Open();
                return true;

            case UIPanelKind.Status:
                if (statusPanel == null) return false;

                isStatusPanelOpen = true;
                statusPanel.gameObject.SetActive(true);

                Player player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
                if (player != null)
                    statusPanel.OpenPanel(player);

                return true;


            case UIPanelKind.Conquest:
                if (conquestUI == null) return false;
                isConquestOpen = true;
                conquestUI.gameObject.SetActive(true);
                conquestUI.OpenRosterFirst();
                return true;

            case UIPanelKind.QuestJournal:
                return EnsureQuestJournalVisibleImmediate();

            case UIPanelKind.Options:
                if (optionsUI == null) return false;
                isOptionsOpen = true;
                optionsUI.gameObject.SetActive(true);
                optionsUI.OpenOptions();
                ApplyButtonTheme(optionsUI.transform);
                return true;

            case UIPanelKind.Save:
                // We are already inside the book when cycling/switching.
                ShowSavePanelImmediate(UI_SaveLoadPanel.OpenContext.PauseMenu);
                return true;

        }

        return false;
    }

    public bool CanSwitchPanelsRightNow()
    {
        // must exist, must be active, must be open idle, must not be busy
        if (bookUI == null) return false;
        if (!bookUI.gameObject.activeInHierarchy) return false;

        // You should implement IsBusy() in BookOpenManager (recommended)
        if (bookUI.IsBusy()) return false;

        // Only allow switching when the book is open idle
        return bookUI.IsInOpenIdle();
    }

    // Called by button script so you don’t expose your private SwitchToPanel
    public void SwitchToPanelFromButton(UIPanelKind target)
    {
        // Only allow when book is already open (per your request)
        if (!CanSwitchPanelsRightNow()) return;

        // Use your existing switching logic (page turn between panels)
        SwitchToPanel(target);
    }

    private void ReturnToOutsideMenu()
    {
        CloseAllPanels();

        if (insideMenuPanel != null) insideMenuPanel.SetActive(false);

        if (bookUI != null)
        {
            bookUI.gameObject.SetActive(true);
            bookUI.PlayCloseThen(() =>
            {
                if (outsideMenuPanel != null) outsideMenuPanel.SetActive(true);
                bookUI.ShowClosedIdle();
                SyncMenusToBook();
            });
        }
        else
        {
            if (outsideMenuPanel != null) outsideMenuPanel.SetActive(true);
            SyncMenusToBook();
        }
    }



    //----------------------------- menu Panel---------------------------------------

    private CharacterProfileSO TryGetProfile(Entity_Stats s)
    {
        if (s == null) return null;
        var refComp = s.GetComponent<CharacterProfileRef>();
        return refComp ? refComp.profile : null;
    }

    private void SetUIMapEnabled(bool enable)
    {
        CacheRewired();
        if (rplayerUI != null)
            rplayerUI.controllers.maps.SetMapsEnabled(enable, "UI");
    }


    // ===== Quest Journal Open/Close/Toggle =====

    public void ToggleQuestJournalFromUI()
    {
        if (!TryFindQuestJournalUI())
        {
            Debug.LogWarning("[UI] questJournalUI not assigned and could not be found.");
            return;
        }

        bool opening = !questJournalUI.gameObject.activeInHierarchy;
        if (opening) StartCoroutine(OpenJournal_Co());
        else CloseQuestJournalFromUI();
    }

    private bool EnsureQuestJournalVisibleImmediate()
    {
        // Find refs
        if (!TryFindQuestJournalUI())
        {
            Debug.LogWarning("[UI] Quest Journal UI not found in scene.");
            return false;
        }

        // Make sure the root is active first
        if (questJournalRoot == null)
            questJournalRoot = questJournalUI.transform.root.gameObject;

        if (questJournalRoot != null) questJournalRoot.SetActive(true);
        if (questJournalUI != null) questJournalUI.gameObject.SetActive(true);

        // Try Quest Machine API methods synchronously
        if (!SafeInvokeNoArgs(questJournalUI, "Show"))
            if (!SafeInvokeNoArgs(questJournalUI, "OpenWindow"))
                SafeInvokeBool(questJournalUI, "SetVisible", true);

        isQuestJournalOpen = true;

        // Enter UI mode (pause, maps)
        EnterUIMode();
        return true;
    }


    public void OpenQuestJournalFromUI()
    {
        if (!TryFindQuestJournalUI()) return;
        StartCoroutine(OpenJournal_Co());
    }

    private IEnumerator OpenJournal_Co()
    {
        // Remember if we are coming from Main Menu
        _openedJournalFromMainMenu = (mainMenuPanel != null && mainMenuPanel.activeSelf);

        EnsureUIRootIsActive();
        CloseAllPanels(); // close others first

        // Try to locate journal if not wired
        if (!TryFindQuestJournalUI())
        {
            Debug.LogError("[UI] Quest Journal UI not found in scene. Recovering to Main Menu.");
            OpenMainMenuDirect();
            yield break;
        }

        if (questJournalRoot != null) questJournalRoot.SetActive(true);
        questJournalUI.gameObject.SetActive(true);

        // Let the journal initialize its UI
        yield return null;

        // Call a safe “show” on whatever API is present
        if (!SafeInvokeNoArgs(questJournalUI, "Show"))
            if (!SafeInvokeNoArgs(questJournalUI, "OpenWindow"))
                SafeInvokeBool(questJournalUI, "SetVisible", true);

        isQuestJournalOpen = true;

        // IMPORTANT: enter full UI mode (enable UI map, disable Gameplay map, pause time).
        EnterUIMode();
    }



    public void CloseQuestJournalFromUI()
    {
        // Try to locate the journal first (handles scene changes / lazy wiring)
        bool hadJournal = TryFindQuestJournalUI();

        if (hadJournal)
        {
            // Ask Quest Machine to hide using whatever API is available
            if (!SafeInvokeNoArgs(questJournalUI, "Hide"))
                if (!SafeInvokeNoArgs(questJournalUI, "CloseWindow"))
                    SafeInvokeBool(questJournalUI, "SetVisible", false);

            // Ensure the objects are actually inactive
            if (questJournalUI != null) questJournalUI.gameObject.SetActive(false);
            if (questJournalRoot != null) questJournalRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[UI] CloseQuestJournalFromUI: Journal UI not found; continuing cleanup.");
        }

        // Mark state
        isQuestJournalOpen = false;

        // If we opened the journal from the main menu (or the option is set), bounce back there.
        if (reopenMainMenuAfterJournalClose || _openedJournalFromMainMenu)
        {
            OpenMainMenuDirect();   // keeps UI map enabled & Time.timeScale = 0
        }
        else
        {
            // Otherwise, restore gameplay input/unpause if nothing else is open.
            CheckStopPlayerControls();

            // Failsafe: if paused and no panels are open, unpause explicitly.
            if (!IsAnySubPanelOpen() && Mathf.Approximately(Time.timeScale, 0f))
                ExitUIMode();
        }

        // Reset flag for next open
        _openedJournalFromMainMenu = false;
    }


    private bool TryFindQuestJournalUI()
    {
        if (questJournalUI != null) return true;

        // Look in active and inactive objects
        questJournalUI = FindFirstObjectByType<UnityUIQuestJournalUI>(FindObjectsInactive.Include);
        if (questJournalUI == null) return false;

        // If no root was wired, treat the journal’s topmost object as its root
        if (questJournalRoot == null)
            questJournalRoot = questJournalUI.transform.root.gameObject;

        return true;
    }



    private static bool SafeInvokeNoArgs(object target, string methodName)
    {
        if (target == null) return false;
        var t = target.GetType();
        var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (m == null) return false;
        m.Invoke(target, null);
        return true;
    }

    private static bool SafeInvokeBool(object target, string methodName, bool arg)
    {
        if (target == null) return false;
        var t = target.GetType();
        var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(bool) }, null);
        if (m == null) return false;
        m.Invoke(target, new object[] { arg });
        return true;
    }

    public void ApplyButtonTheme(Transform root = null)
    {
        if (root == null)
            root = mainMenuPanel != null ? mainMenuPanel.transform : transform;

        var selectables = root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
        foreach (var s in selectables)
        {
            // Make sure it's using ColorTint so selectedColor is used
            s.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

            var cb = s.colors;
            cb.normalColor = btnNormal;
            cb.highlightedColor = btnHighlighted; // mouse hover / pointer
            cb.selectedColor = btnSelected;    // keyboard/controller focus
            cb.pressedColor = btnPressed;
            cb.disabledColor = btnDisabled;
            cb.fadeDuration = btnFade;
            cb.colorMultiplier = btnMultiplier;
            s.colors = cb;
        }
    }




    #endregion

    public void SelectStatusCharacter(GameObject character)
    {
        Debug.Log(
            $"[UI] SelectStatusCharacter -> " +
            $"character={(character != null ? character.name : "NULL")}, " +
            $"isStatusSelectionMode={isStatusSelectionMode}"
        );

        if (character == null || statusPanel == null)
            return;

        if (!isStatusSelectionMode)
        {
            Debug.Log("[UI] Early return: status selection mode is not active.");
            return;
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        EnsureUIRootIsActive();
        CloseAllPanels();

        OpenPanelWithBook(() =>
        {
            Debug.Log($"[UI] Opening status panel for {character.name}");

            isStatusSelectionMode = false;
            isStatusPanelOpen = true;

            statusPanel.gameObject.SetActive(true);
            statusPanel.OpenPanel(character);
        });
    }


    #region Input Control

    // NOTE: used by panels other than the main menu to simply disable movement (no map swap)
    public void StopPlayerControls(bool stopGameplay)
    {
        CacheRewired();
        if (rplayerUI != null)
            rplayerUI.controllers.maps.SetMapsEnabled(!stopGameplay, "Gameplay");
    }

    private void CheckStopPlayerControls()
    {
        if (!IsAnySubPanelOpen() && (mainMenuPanel == null || !mainMenuPanel.activeSelf))
        {
            StopPlayerControls(false);     // re-enable Gameplay map for non-menu panels
            StopMenuPoll();

            // If we just closed the main menu, restore maps & unpause
            ExitUIMode();
        }
    }

    private bool IsAnySubPanelOpen()
    {
        return isInventoryOpen || isSkillTreeOpen || isEquipmentOpen || isOptionsOpen
               || isStorageOpen || isMerchantOpen || isCraftOpen || isStatusPanelOpen
               || isConquestOpen || isQuestJournalOpen || isSaveOpen;
    }

    private void EnsureUIRootIsActive()
    {
        uiRoot?.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        StopPlayerControls(true); // disable Gameplay while any panel is open
    }

    public void CloseAllPanels()
    {
        SwitchOffAllToolTips();  // 👈 hide all tips first

        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
        statusPanel?.gameObject.SetActive(false);
        conquestUI?.gameObject.SetActive(false);
        optionsUI?.gameObject.SetActive(false);
        storageUI?.gameObject.SetActive(false);
        merchantUI?.gameObject.SetActive(false);
        craftUI?.gameObject.SetActive(false);
        mainMenuPanel?.SetActive(false);

        // Quest Journal:
        if (questJournalUI != null) questJournalUI.gameObject.SetActive(false);
        if (questJournalRoot != null) questJournalRoot.SetActive(false);

        if (saveLoadPanel != null && saveLoadPanel.IsOpen)
        {
            isSaveOpen = false;

            if (_suppressSaveClosedHandler)
            {
                // ✅ Don't let Save close bounce to main menu while switching panels
                saveLoadPanel.Closed -= OnSaveLoadClosed;
                saveLoadPanel.ClosePanel();
                // We'll re-subscribe when opening Save again in OpenSavePanel()
            }
            else
            {
                saveLoadPanel.ClosePanel();
            }
        }


        ResetStates();
        StopMenuPoll();

        // ✅ IMPORTANT: book should NOT be closed here
        if (bookUI != null)
            bookUI.gameObject.SetActive(true);
    }


    private void ResetStates()
    {
        isInventoryOpen = false;
        isSkillTreeOpen = false;
        isEquipmentOpen = false;
        isOptionsOpen = false;
        isStatusPanelOpen = false;
        isConquestOpen = false;
        isStorageOpen = false;
        isMerchantOpen = false;
        isCraftOpen = false;
        isQuestJournalOpen = false;
        isSaveOpen = false;
    }

    public void HandleBackAction()
    {
        // 1️⃣ Hotbar assign preview
        if (_assignPreviewActive)
        {
            HideHotbarAssignPreview();
            return;
        }

        // 2️⃣ Inventory → let inventory handle its own back stack first
        if (inventoryUI != null && inventoryUI.IsOpen())
        {
            bool handled = inventoryUI.HandleCancel();

            // If inventory did not handle it internally, then close the inventory
            if (!handled || inventoryUI.IsOpen() == false)
            {
                // optional fallback if needed
            }

            return;
        }

        // 3️⃣ Equipment → let equipment handle its own back stack first
        if (equipmentInventoryPanel != null && equipmentInventoryPanel.IsOpen)
        {
            bool handled = equipmentInventoryPanel.HandleCancel();

            if (!handled || equipmentInventoryPanel.IsOpen == false)
            {
                // optional fallback if needed
            }

            return;
        }

        // 4️⃣ Crafting → stays in book
        if (craftUI != null && isCraftOpen && craftUI.HandleCancel())
            return;

        // 5️⃣ Merchant → stays in book
        if (merchantUI != null && merchantUI.IsOpen && merchantUI.HandleCancel())
            return;

        // 6️⃣ Skill Tree → ALWAYS back to main menu
        if (skillTreeUI != null && skillTreeUI.gameObject.activeInHierarchy)
        {
            skillTreeUI.HandleCancel();
            CloseSkillTree();
            return;
        }

        // 7️⃣ Status → ALWAYS back to main menu
        if (statusPanel != null && statusPanel.gameObject.activeInHierarchy)
        {
            CloseStatusPanel();
            return;
        }

        // 8️⃣ Conquest → Details backs to roster, roster backs to Main Menu
        if (conquestUI != null && conquestUI.gameObject.activeInHierarchy)
        {
            conquestUI.HandleCancel();

            if (conquestUI.gameObject.activeInHierarchy)
                return;

            CloseConquest();
            return;
        }

        // 9️⃣ Options → ALWAYS back to main menu
        if (optionsUI != null && optionsUI.gameObject.activeInHierarchy)
        {
            CloseOptions();
            return;
        }

        // 🔟 Quest Journal → ALWAYS back to main menu
        if (isQuestJournalOpen ||
            (questJournalUI != null && questJournalUI.gameObject.activeInHierarchy))
        {
            CloseQuestJournalFromUI();
            return;
        }

        // ⓫ Save Panel → close book + return to main menu
        if (saveLoadPanel != null && saveLoadPanel.IsOpen)
        {
            CloseSavePanel();
            return;
        }

        // ⓬ Main Menu → exit UI completely
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            mainMenuPanel.SetActive(false);

            if (bookUI != null)
            {
                bookUI.ShowClosedIdle();
                bookUI.gameObject.SetActive(false);
            }

            uiRoot?.SetActive(false);

            ExitUIMode();
            CheckStopPlayerControls();
            return;
        }

        // ⓭ Failsafe
        if (Mathf.Approximately(Time.timeScale, 0f) && !IsAnySubPanelOpen())
        {
            OpenMainMenuDirect();
            return;
        }

        Debug.Log("[UI] No panels handled cancel.");
    }



    #endregion

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, null);
        statToolTip?.ShowToolTip(false, null);
        skillToolTip?.ShowToolTip(false, null);
    }

    // ========================= Return-to-Title (Ys-style) & helpers =========================

    public void GoToTitleScreenClean()
    {
        StartCoroutine(ReturnToTitle_Co(saveSuspend: false));
    }

    public void ReturnToTitle_Ys()
    {
        ShowConfirm("Return to Title?\nUnsaved progress will be lost.",
            onYes: () => StartCoroutine(ReturnToTitle_Co(saveSuspend: false)),
            onNo: null);
    }

    public void ReturnToTitle_WithSuspend()
    {
        ShowConfirm("Suspend and return to Title?",
            onYes: () => StartCoroutine(ReturnToTitle_Co(saveSuspend: true)),
            onNo: null);
    }

    public static bool TryLoadSuspendAndClear()
    {
        if (PlayerPrefs.GetInt(SuspendKey, 0) == 1)
        {
            if (SaveSystem.hasInstance) SaveSystem.instance.allowNegativeSlotNumbers = true;

            if (SaveSystem.HasSavedGameInSlot(SuspendSlot))
            {
                SaveSystem.LoadFromSlot(SuspendSlot);
                SaveSystem.DeleteSavedGameInSlot(SuspendSlot);
                PlayerPrefs.DeleteKey(SuspendKey);
                return true;
            }

            PlayerPrefs.DeleteKey(SuspendKey);
        }
        return false;
    }

    private IEnumerator ReturnToTitle_Co(bool saveSuspend)
    {
        // 1) shut down UI & input
        SwitchOffAllToolTips();
        CloseAllPanels();
        mainMenuPanel?.SetActive(false);
        uiRoot?.SetActive(false);
        StopPlayerControls(true);
        ExitUIMode();
        yield return null;

        // 2) optional suspend save
        if (saveSuspend)
        {
            if (SaveSystem.hasInstance) SaveSystem.instance.allowNegativeSlotNumbers = true;
            SaveSystem.SaveToSlotImmediate(SuspendSlot);
            PlayerPrefs.SetInt(SuspendKey, 1);
            PlayerPrefs.Save();
        }

        // 3) purge DDOL except essentials
        CleanDontDestroyOnLoadExcept(ddolEssentials);

        // 4) jump to title (clean state)
        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogError("[UI] ReturnToTitle: titleSceneName is not set.");
            yield break;
        }

        SaveSystem.autoUnloadAdditiveScenes = true;
        SaveSystem.RestartGame(titleSceneName);
    }

    private void CleanDontDestroyOnLoadExcept(GameObject[] extrasToKeep)
    {
        var keep = new HashSet<GameObject>();
        if (extrasToKeep != null) foreach (var g in extrasToKeep) if (g) keep.Add(g);

        if (PixelCrushers.SaveSystem.hasInstance && PixelCrushers.SaveSystem.instance)
            keep.Add(PixelCrushers.SaveSystem.instance.gameObject);

        var rewired = FindObjectOfType<Rewired.InputManager>(true);
        if (rewired) keep.Add(rewired.gameObject);

        var ddolScene = gameObject.scene;
        var roots = new List<GameObject>();
        ddolScene.GetRootGameObjects(roots);

        for (int i = roots.Count - 1; i >= 0; i--)
        {
            var go = roots[i];
            if (!go) continue;
            if (keep.Contains(go)) continue;

            Destroy(go);
        }
    }

    // When backing out of a pause/menu panel, always show the Main Menu panel.
    private void ReturnToMainMenuAfterClosingPanel()
    {
        // If already showing it, do nothing
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            return;

        EnsureUIRootIsActive();
        CloseAllPanels();               // hides panels + keeps book active (per your CloseAllPanels code)

        // If no book, just show menu immediately
        if (bookUI == null)
        {
            mainMenuPanel?.SetActive(true);
            ApplyButtonTheme(mainMenuPanel?.transform);
            ForceRefreshMenuBars();
            StartMenuPollIfNeeded();
            EnterUIMode();
            return;
        }

        // ✅ Close animation first, THEN show menu
        bookUI.gameObject.SetActive(true);
        bookUI.PlayCloseThen(() =>
        {
            // now it is fully closed
            mainMenuPanel?.SetActive(true);

            // optional: make absolutely sure we’re on the closed idle pose
            bookUI.ShowClosedIdle();

            ApplyButtonTheme(mainMenuPanel?.transform);
            ForceRefreshMenuBars();
            StartMenuPollIfNeeded();
            EnterUIMode();
        });
    }




    // ============================ Menu Bars: internals =====================================

    private Component AutoFindStats()
    {
        // Try to find by exact type name without hard dependencies
        var playerStats = FindFirstObjectByType<Component>(FindObjectsInactive.Include);
        if (playerStats != null && playerStats.GetType().Name == "Player_Stats")
            return playerStats;

        // Fallback: scan all loaded components for common names
        foreach (var c in FindObjectsOfType<Component>(true))
        {
            var n = c.GetType().Name;
            if (n == "Player_Stats" || n == "Entity_Stats" || n == "Player")
                return c;
        }
        return null;
    }

    private void BindStats(Component comp)
    {
        if (comp == statsComp) return;

        UnhookStatEvents();
        statsComp = comp;

        if (statsComp == null)
        {
            pCurHP = pMaxHP = pCurMP = pMaxMP = null;
            eHP = eMP = null;
            return;
        }

        var t = statsComp.GetType();
        pCurHP = t.GetProperty("CurrentHealth") ?? t.GetProperty("currentHealth") ?? t.GetProperty("HP");
        pMaxHP = t.GetProperty("MaxHealth") ?? t.GetProperty("maxHealth") ?? t.GetProperty("MaxHP");
        pCurMP = t.GetProperty("CurrentMana") ?? t.GetProperty("currentMana") ?? t.GetProperty("MP");
        pMaxMP = t.GetProperty("MaxMana") ?? t.GetProperty("maxMana") ?? t.GetProperty("MaxMP");

        eHP = t.GetEvent("OnHealthChanged");
        eMP = t.GetEvent("OnManaChanged");

        HookStatEvents();
    }

    private void HookStatEvents()
    {
        if (statsComp == null) return;

        // Hook if signatures exist (int,int) or ()
        if (eHP != null)
        {
            var del = System.Delegate.CreateDelegate(eHP.EventHandlerType, this, nameof(OnHealthChangedEvent));
            eHP.AddEventHandler(statsComp, del);
        }

        if (eMP != null)
        {
            var del = System.Delegate.CreateDelegate(eMP.EventHandlerType, this, nameof(OnManaChangedEvent));
            eMP.AddEventHandler(statsComp, del);
        }
    }

    private void UnhookStatEvents()
    {
        if (statsComp == null) return;

        if (eHP != null)
        {
            var del = System.Delegate.CreateDelegate(eHP.EventHandlerType, this, nameof(OnHealthChangedEvent));
            eHP.RemoveEventHandler(statsComp, del);
        }
        if (eMP != null)
        {
            var del = System.Delegate.CreateDelegate(eMP.EventHandlerType, this, nameof(OnManaChangedEvent));
            eMP.RemoveEventHandler(statsComp, del);
        }
    }

    // Flexible event handlers (support () and (int,int))
    private void OnHealthChangedEvent() { RefreshMenuHealth(); }
    private void OnHealthChangedEvent(int current, int max) { SetMenuHealth(current, max); }
    private void OnManaChangedEvent() { RefreshMenuMana(); }
    private void OnManaChangedEvent(int current, int max) { SetMenuMana(current, max); }

    private void StartMenuPollIfNeeded()
    {
        if (menuPollCo != null || eHP != null || eMP != null) return; // already have events or a poll
        if (mainMenuPanel == null || !mainMenuPanel.activeSelf) return;
        menuPollCo = StartCoroutine(MenuPollLoop());
    }

    private void StopMenuPoll()
    {
        if (menuPollCo != null)
        {
            StopCoroutine(menuPollCo);
            menuPollCo = null;
        }
    }

    private IEnumerator MenuPollLoop()
    {
        var wait = new WaitForSeconds(menuPollInterval);
        while (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            RefreshMenuBars();
            yield return wait;
        }
        menuPollCo = null;
    }

    private void ForceRefreshMenuBars()
    {
        RefreshMenuBars();
    }

    private void RefreshMenuBars()
    {
        RefreshMenuHealth();
        RefreshMenuMana();
    }

    private void RefreshMenuHealth()
    {
        if (statsComp == null || pCurHP == null || pMaxHP == null) return;
        int cur = SafeGetInt(statsComp, pCurHP);
        int max = SafeGetInt(statsComp, pMaxHP);
        SetMenuHealth(cur, max);
    }

    private void RefreshMenuMana()
    {
        if (statsComp == null || pCurMP == null || pMaxMP == null) return;
        int cur = SafeGetInt(statsComp, pCurMP);
        int max = SafeGetInt(statsComp, pMaxMP);
        SetMenuMana(cur, max);
    }


    private void SetMenuHealth(int current, int max)
    {
        if (menuHealthSlider)
        {
            menuHealthSlider.maxValue = max;
            menuHealthSlider.value = Mathf.Clamp(current, 0, max);
        }
        if (menuHealthText)
            menuHealthText.text = $"{current}/{max}";
    }

    private void SetMenuMana(int current, int max)
    {
        if (menuManaSlider)
        {
            menuManaSlider.maxValue = max;
            menuManaSlider.value = Mathf.Clamp(current, 0, max);
        }
        if (menuManaText)
            menuManaText.text = $"{current}/{max}";
    }

    private static int SafeGetInt(object obj, PropertyInfo pi)
    {
        if (pi == null) return 0;
        var v = pi.GetValue(obj, null);
        if (v is int i) return i;
        if (v is float f) return Mathf.RoundToInt(f);
        if (v is double d) return Mathf.RoundToInt((float)d);
        return 0;
    }

    // ============================ Top Bar helpers =====================================

    private static string FormatHHMMSS(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int h = totalSeconds / 3600;
        int m = (totalSeconds % 3600) / 60;
        int s = totalSeconds % 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    // UI.cs
    // UI.cs
    public void ResyncPlayTimeFromTracker()
    {
        if (!timePlayedUsesUnscaled) return;

        // Whatever your tracker uses – adjust this line if needed.
        _realSecondsPlayed = PlayTimeTracker.TotalSecondsInt;

        Debug.Log($"[UI] ResyncPlayTimeFromTracker -> {_realSecondsPlayed} seconds");
        UpdateTimePlayedLabelImmediate();
    }


    public int CurrentTimePlayedSeconds
    {
        get
        {
            return timePlayedUsesUnscaled
                ? (_realSecondsPlayed < 0 ? PlayTimeTracker.TotalSecondsInt : _realSecondsPlayed)
                : PlayTimeTracker.TotalSecondsInt;
        }
    }


    // replaces your existing handler
    private void HandleSecondTick(int _)
    {
        if (timePlayedUsesUnscaled) return;  // ignore scaled ticks when using unscaled
        UpdateTimePlayedLabelImmediate();
    }


    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateLocationLabel();
    }

    private void EnsureRealSecondsInit()
    {
        if (_realSecondsPlayed < 0) // not set yet
            _realSecondsPlayed = PlayTimeTracker.TotalSecondsInt; // start from current saved/scaled total
    }


    private void UpdateTimePlayedLabelImmediate()
    {
        if (timePlayedLabel == null) return;

        int secs = timePlayedUsesUnscaled
            ? (_realSecondsPlayed < 0 ? PlayTimeTracker.TotalSecondsInt : _realSecondsPlayed)
            : PlayTimeTracker.TotalSecondsInt;

        string full = TimePlayedPrefix + FormatHHMMSS(secs);
        _timePlayedRendered = full;
        timePlayedLabel.text = full;
    }




    private IEnumerator RealTimeTicker_Co()
    {
        var wait = new WaitForSecondsRealtime(1f);
        EnsureRealSecondsInit();                 // make sure it isn’t -1
        while (true)
        {
            _realSecondsPlayed++;                // real second, even while paused
            UpdateTimePlayedLabelImmediate();
            yield return wait;
        }
    }



    private void UpdateLocationLabel()
    {
        if (locationLabel != null)
            locationLabel.text = $"Location: {SceneManager.GetActiveScene().name}";
    }

    // ====================== Assign preview (hide tree / pick on hotbar) ======================

    // Call this when you ENTER assign mode (right-click an unlocked node)
    public void ShowHotbarAssignPreview(SkillCategory category)
    {
        _assignPreviewActive = true;

        // 1) Hide the Skill Tree completely (store previous)
        if (skillTreeUI != null)
        {
            _skillTreeWasActive = skillTreeUI.gameObject.activeSelf;
            skillTreeUI.gameObject.SetActive(false);
        }

        // 2) Show the correct hotbar for picking
        if (category == SkillCategory.Sex)
        {
            var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
            if (sexUI != null)
            {
                if (!sexUI.IsOpen)
                {
                    sexUI.ShowAssignPreview();
                    _sexUIOpenedByPreview = true;
                }

                var slots = sexUI.Hotbar?.Slots;
                if (slots != null)
                {
                    var selects = slots.Where(s => s != null)
                                       .Select(s => s.GetComponent<Selectable>())
                                       .Where(s => s != null)
                                       .ToArray();
                    WireLinearNav(selects, horizontal: true);

                    // Select first empty slot (or first slot)
                    var first = slots.FirstOrDefault(s => s != null && !s.HasSkill) ?? slots.FirstOrDefault(s => s != null);
                    SelectGO(first != null ? first.gameObject : null);
                }
            }
        }
        else // Combat
        {
            if (inGameUI != null && !inGameUI.gameObject.activeInHierarchy)
            {
                inGameUI.gameObject.SetActive(true);
                _combatHUDActivatedByPreview = true;
            }
        }
    }

    // Call this ONLY when the PLAYER EXITS (Esc/Back/Done). Don't call automatically after assigning.
    public void HideHotbarAssignPreview()
    {
        if (!_assignPreviewActive) return;
        _assignPreviewActive = false;

        // 1) Restore the Skill Tree panel if it was visible before
        if (skillTreeUI != null && _skillTreeWasActive == true)
            skillTreeUI.gameObject.SetActive(true);
        _skillTreeWasActive = false;

        // Sync state so next Esc goes to main menu
        isSkillTreeOpen = skillTreeUI != null && skillTreeUI.gameObject.activeSelf;

        // 2) Close Sex UI if we opened it only for preview
        var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
        if (sexUI != null && _sexUIOpenedByPreview)
            sexUI.HideAssignPreview();
        _sexUIOpenedByPreview = false;

        // 3) Hide Combat HUD if we showed it only for preview
        if (inGameUI != null && _combatHUDActivatedByPreview)
            inGameUI.gameObject.SetActive(false);
        _combatHUDActivatedByPreview = false;
    }

    // The only blessed way to exit assign mode
    public void RequestExitAssignPreview()
    {
        HideHotbarAssignPreview();

        // Always bring the Skill Tree back after leaving assign mode
        if (skillTreeUI != null)
        {
            if (!skillTreeUI.gameObject.activeSelf)
            {
                OpenSkillTree(); // sets isSkillTreeOpen, enables panel, re-enables input
            }
            else
            {
                // ensure fully interactive
                var cg = skillTreeUI.GetComponent<CanvasGroup>();
                if (cg)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                isSkillTreeOpen = true;
            }
        }
    }

    // ===== NEW: Sex skill routing helper (used by SkillTree) =====
    public void AssignSexSkillToSexyTimeHotbar(Skill_DataSO sexSkill)
    {
        if (sexSkill == null || sexSkill.category != SkillCategory.Sex) return;

        // Persist regardless of UI presence
        SexyTimeUIController.AddPersistentSexSkill(sexSkill);

        // If controller exists, reflect immediately
        var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
        if (sexUI != null)
        {
            sexUI.ShowSexSkill(sexSkill);
            Debug.Log($"[UI] Routed Sex skill '{sexSkill.displayName}' to SexyTime hotbar (and persisted).");
        }
        else
        {
            Debug.Log($"[UI] SexyTimeUIController not found yet — persisted Sex skill '{sexSkill.displayName}' for later.");
        }
    }

    // ====================== UI Navigation helpers ======================

    public static void WireLinearNav(Selectable[] items, bool horizontal)
    {
        if (items == null) return;
        for (int i = 0; i < items.Length; i++)
        {
            var s = items[i];
            if (s == null) continue;
            var n = s.navigation;
            n.mode = Navigation.Mode.Explicit;

            var left = i > 0 ? items[i - 1] : null;
            var right = i < items.Length - 1 ? items[i + 1] : null;

            if (horizontal)
            {
                n.selectOnLeft = left;
                n.selectOnRight = right;
            }
            else
            {
                n.selectOnUp = left;
                n.selectOnDown = right;
            }

            s.navigation = n;
        }
    }

    public static void WireGridNav(Selectable[] items, int cols)
    {
        if (items == null || cols <= 0) return;
        for (int i = 0; i < items.Length; i++)
        {
            var s = items[i];
            if (s == null) continue;

            int row = i / cols;
            int col = i % cols;

            var n = s.navigation;
            n.mode = Navigation.Mode.Explicit;

            // left/right
            n.selectOnLeft = col > 0 ? items[i - 1] : null;
            n.selectOnRight = col < cols - 1 && i + 1 < items.Length ? items[i + 1] : null;

            // up/down
            int upIdx = i - cols;
            int downIdx = i + cols;
            n.selectOnUp = upIdx >= 0 ? items[upIdx] : null;
            n.selectOnDown = downIdx < items.Length ? items[downIdx] : null;

            s.navigation = n;
        }
    }

    private static void SelectGO(GameObject go)
    {
        if (go == null) return;
        EventSystem.current?.SetSelectedGameObject(go);
    }

    // ====================== Rewired Map swap for Main Menu ======================

    private void EnterUIMode()
    {
        CacheRewired();
        if (rplayerUI != null)
        {
            rplayerUI.controllers.maps.SetMapsEnabled(false, "Gameplay"); // disable gameplay
            rplayerUI.controllers.maps.SetMapsEnabled(true, "UI");       // enable UI navigation
        }

        var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        p?.SetInputEnabled(false); // optional: hard stop custom input in your states

        Time.timeScale = 0f; // pause while menu is open
    }

    private void ExitUIMode()
    {
        CacheRewired();
        if (rplayerUI != null)
        {
            rplayerUI.controllers.maps.SetMapsEnabled(true, "Gameplay"); // restore gameplay
            rplayerUI.controllers.maps.SetMapsEnabled(false, "UI");       // disable UI map
        }

        var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        p?.SetInputEnabled(true);

        if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;
    }

    // -------------------- ADDED: Panel Switch helpers --------------------
    private void SwitchUIPanel(int direction) // +1 next, -1 prev
    {
        if (direction == 0) return;

        // Don’t cycle while overwrite dialog is open
        if (saveLoadPanel != null && saveLoadPanel.IsOpen && saveLoadPanel.IsOverwriteOpen)
            return;

        // Don’t cycle during special flows
        if (isMerchantOpen || isCraftOpen || isStorageOpen || _assignPreviewActive)
            return;

        // Must already be inside the book UI (not on main menu)
        var active = GetActivePanelKind();
        if (!active.HasValue) return;

        // Use the filtered cache (only panels that exist)
        if (_panelCycleCache == null || _panelCycleCache.Length == 0) return;

        int cur = Array.IndexOf(_panelCycleCache, active.Value);
        if (cur < 0) return;

        int next = Mod(cur + direction, _panelCycleCache.Length);
        SwitchToPanel(_panelCycleCache[next]);   // ✅ this will page-turn between panels
    }


    private bool IsPanelAvailable(UIPanelKind kind)
    {
        switch (kind)
        {
            case UIPanelKind.Inventory: return inventoryUI != null;
            case UIPanelKind.SkillTree: return skillTreeUI != null;
            case UIPanelKind.Equipment: return equipmentInventoryPanel != null;
            case UIPanelKind.Status: return statusPanel != null;
            case UIPanelKind.Conquest: return conquestUI != null;
            case UIPanelKind.QuestJournal: return questJournalUI != null || TryFindQuestJournalUI();
            case UIPanelKind.Options: return optionsUI != null;
            case UIPanelKind.Save: return true; // lazy-find inside OpenSavePanel
            default: return false;
        }
    }

    private void SwitchToPanel(UIPanelKind target)
    {
        // If we're on main menu: open book, then show panel (no page turn)
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            OpenPanelFromMainMenu(() => TryOpenPanelByKind(target));
            return;
        }

        // Must be able to switch (book open idle, not busy)
        if (!CanSwitchPanelsRightNow())
            return;

        var active = GetActivePanelKind();

        // Save tracking (optional, but keep your intent)
        if (target == UIPanelKind.Save)
        {
            if (active.HasValue && active.Value != UIPanelKind.Save)
                _panelBeforeSave = active.Value;

            _saveOpenedFromCycle = true;
        }
        else
        {
            _saveOpenedFromCycle = false;
        }

        // Decide turn direction based on cycle order
        bool turnRight = true;
        if (active.HasValue)
            turnRight = ShouldTurnRight(active.Value, target);

        // ✅ THIS WAS MISSING: actually do the page turn and show the panel
        SwitchPanelWithPageTurn(() =>
        {
            TryOpenPanelByKind(target);
        }, turnRight);
    }




    private int FindCurrentPanelIndex()
    {
        UIPanelKind? active = GetActivePanelKind();
        if (active.HasValue)
        {
            for (int i = 0; i < panelCycleOrder.Length; i++)
                if (panelCycleOrder[i] == active.Value) return i;
        }
        return -1;
    }

    private UIPanelKind? GetActivePanelKind()
    {
        // Save first: it’s not a plain GameObject toggle
        if (saveLoadPanel != null && saveLoadPanel.IsOpen) return UIPanelKind.Save;

        // Use real active state so we don’t depend on any stale flags
        if (inventoryUI != null && inventoryUI.gameObject.activeInHierarchy) return UIPanelKind.Inventory;
        if (skillTreeUI != null && skillTreeUI.gameObject.activeInHierarchy) return UIPanelKind.SkillTree;
        if (equipmentInventoryPanel != null && equipmentInventoryPanel.gameObject.activeInHierarchy) return UIPanelKind.Equipment;
        if (statusPanel != null && statusPanel.gameObject.activeInHierarchy) return UIPanelKind.Status;

        if ((questJournalUI != null && questJournalUI.gameObject.activeInHierarchy) || isQuestJournalOpen)
            return UIPanelKind.QuestJournal;
        if (conquestUI != null && conquestUI.gameObject.activeInHierarchy) return UIPanelKind.Conquest;
        if (optionsUI != null && optionsUI.gameObject.activeInHierarchy) return UIPanelKind.Options;

        return null;
    }





    private void EnsureValidPanelCycle()
    {
        if (panelCycleOrder == null || panelCycleOrder.Length == 0)
        {
            _panelCycleCache = Array.Empty<UIPanelKind>();
            return;
        }

        var list = new List<UIPanelKind>(panelCycleOrder.Length);
        foreach (var k in panelCycleOrder)
        {
            if (IsPanelAvailable(k)) list.Add(k);
        }
        _panelCycleCache = list.Count > 0 ? list.ToArray() : Array.Empty<UIPanelKind>();

#if UNITY_EDITOR
        // Helpful debug: see what will actually cycle
        var joined = string.Join(" -> ", _panelCycleCache.Select(x => x.ToString()));
        Debug.Log($"[UI] Panel cycle (filtered): {joined}");
#endif
    }

    private void AutoFindUIPanelsIfMissing()
    {
        var include = FindObjectsInactive.Include;

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<UI_Inventory>(include);

        if (skillTreeUI == null)
            skillTreeUI = FindFirstObjectByType<UI_SkillTree>(include);

        if (statusPanel == null) // ✅ ADD THIS
            statusPanel = FindFirstObjectByType<UI_StatusPanel>(include);

        if (conquestUI == null)
            conquestUI = FindFirstObjectByType<UI_Conquest>(include);

        if (questJournalUI == null)
            questJournalUI = FindFirstObjectByType<UnityUIQuestJournalUI>(include);

        if (optionsUI == null)
            optionsUI = FindFirstObjectByType<UI_Options>(include);

    }



    //private void WatchForPanelsThatClosedThemselves()
    //{
    //    // Save: if it closed itself, go back to main menu (unless we're switching panels)
    //    if (!_suppressSaveClosedHandler && isSaveOpen && saveLoadPanel != null && !saveLoadPanel.IsOpen)
    //    {
    //        isSaveOpen = false;
    //        OpenMainMenuDirect();
    //    }

    //    // Status
    //    if (isStatusPanelOpen && (statusPanel == null || !statusPanel.gameObject.activeInHierarchy))
    //    {
    //        isStatusPanelOpen = false;
    //        OpenMainMenuDirect();
    //    }

    //    // Options
    //    if (isOptionsOpen && (optionsUI == null || !optionsUI.gameObject.activeInHierarchy))
    //    {
    //        isOptionsOpen = false;
    //        OpenMainMenuDirect();
    //    }

    //    // Conquest
    //    if (isConquestOpen && (conquestUI == null || !conquestUI.gameObject.activeInHierarchy))
    //    {
    //        isConquestOpen = false;
    //        OpenMainMenuDirect();
    //    }

    //    // Quest Journal
    //    if (isQuestJournalOpen && (questJournalUI == null || !questJournalUI.gameObject.activeInHierarchy))
    //    {
    //        isQuestJournalOpen = false;
    //        OpenMainMenuDirect();
    //    }
    //}







    private static int Mod(int x, int m) => (x % m + m) % m;

    // ---------------------------------------------------------------------
}
