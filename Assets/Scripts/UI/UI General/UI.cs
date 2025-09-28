using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Rewired;
using PixelCrushers;
using PixelCrushers.QuestMachine.Wrappers;

public class UI : MonoBehaviour
{
    // -------- Singleton Guard --------
    public static UI Instance { get; private set; }

    #region Components
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public Inventory_Item hoveredItem;

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

    // ===== Quest Journal (Quest Machine) =====
    [Header("Quest Journal (Quest Machine)")]
    [Tooltip("Optional parent GameObject that contains the UnityUIQuestJournalUI; toggled with the journal.")]
    [SerializeField] private GameObject questJournalRoot;
    [Tooltip("Assign the UnityUIQuestJournalUI (wrapper) component here.")]
    [SerializeField] private UnityUIQuestJournalUI questJournalUI;
    [Tooltip("If true, re-open the main menu panel after closing the journal.")]
    [SerializeField] private bool reopenMainMenuAfterJournalClose = true;

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
    // (Optional) You can add a mapped action to toggle the journal if you want:
    //[SerializeField] private string toggleQuestJournalAction = "ToggleQuestJournal";

    private Rewired.Player player;

    private bool isInventoryOpen = false;
    private bool isSkillTreeOpen = false;
    private bool isEquipmentOpen = false;
    private bool isStatusPanelOpen = false;
    private bool isConquestOpen = false;
    private bool isOptionsOpen = false;
    private bool isStorageOpen = false;
    private bool isMerchantOpen = false;
    private bool isCraftOpen = false;

    // track event subscription so we don’t double-subscribe when scenes change
    private bool goldSubscribed = false;
    private Inventory_Player cachedInv;
    #endregion

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

    // ============================ Assign-preview (fade the tree) ===========================
    // We only fade and disable tree raycasts; no need to mess with sorting orders.
    private bool _assignPreviewActive = false;
    private bool _sexUIOpenedByPreview = false;
    private bool _combatHUDActivatedByPreview = false;

    private CanvasGroup _treeCg;
    private float _treePrevAlpha = 1f;
    private bool _treePrevInteractable = true;
    private bool _treePrevBlocks = true;

    // --- Assign-preview state (deactivate tree while picking) ---

    private bool _skillTreeWasActive = false;



    public bool IsAssignPreviewActive => _assignPreviewActive;


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

        // Journal off by default
        if (questJournalUI != null) questJournalUI.gameObject.SetActive(false);
        if (questJournalRoot != null) questJournalRoot.SetActive(false);
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
        // Re-arm gold subscription in case of scene reload
        TrySubscribeGold();

        // Rebind stats after scene loads
        SceneManager.sceneLoaded += OnSceneLoaded_UIRefresh;

        // NEW: hook playtime + scene change to keep the top bar hot
        PlayTimeTracker.OnSecondChanged += HandleSecondTick;
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;

        // immediate refresh
        HandleSecondTick(PlayTimeTracker.TotalSecondsInt);
        UpdateLocationLabel();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_UIRefresh;
        UnsubscribeGold();
        UnhookStatEvents();
        StopMenuPoll();

        // NEW: unhook
        PlayTimeTracker.OnSecondChanged -= HandleSecondTick;
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void OnDestroy()
    {
        UnsubscribeGold();
        UnhookStatEvents();
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded_UIRefresh(Scene scene, LoadSceneMode mode)
    {
        // ensure we have current stats reference after spawns
        StartCoroutine(AfterSceneLoad_Co());
        StartCoroutine(RefreshHUDOnceCo());

        // update location label when scene loads
        UpdateLocationLabel();
    }

    private IEnumerator AfterSceneLoad_Co()
    {
        yield return null;
        BindStats(AutoFindStats());
        if (mainMenuPanel != null && mainMenuPanel.activeSelf) ForceRefreshMenuBars();
    }

    private IEnumerator RefreshHUDOnceCo()
    {
        // Let spawners create Player/Inventory this frame
        yield return null;

        // Ensure we’re listening to the current Inventory for gold updates
        TrySubscribeGold();

        // Force HUD to pull current state (gold, quick slots, exp/sex exp, HP/MP)
        inGameUI?.ForceRefreshFromCurrentState();

        // Populate skill slots from the current skill tree
        if (skillTreeUI != null)
            inGameUI?.RefreshSkillSlotsFromTree(skillTreeUI);
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
    }

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

    #region Open/Close Panels

    public void OpenInventory()
    {
        isInventoryOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        inventoryUI?.gameObject.SetActive(true);
        inventoryUI?.OpenInventory();
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryUI?.CloseInventory();
        CheckStopPlayerControls();
    }

    public void OpenSkillTree()
    {
        isSkillTreeOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        skillTreeUI?.gameObject.SetActive(true);
    }

    public void CloseSkillTree()
    {
        isSkillTreeOpen = false;
        skillTreeUI?.gameObject.SetActive(false);
        CheckStopPlayerControls();
    }

    public void OpenEquipment()
    {
        isEquipmentOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        equipmentInventoryPanel?.gameObject.SetActive(true);
        equipmentInventoryPanel?.Open();
    }

    public void CloseEquipment()
    {
        isEquipmentOpen = false;
        equipmentInventoryPanel?.Close();
        CheckStopPlayerControls();
    }

    public void OpenOptions()
    {
        CloseAllPanels();

        if (optionsUI != null)
        {
            isOptionsOpen = true;
            EnsureUIRootIsActive();
            optionsUI.OpenOptions();
            Debug.Log("[UI] Opening Options Panel");
        }
    }

    public void CloseOptions()
    {
        isOptionsOpen = false;
        optionsUI?.ClosePanel();
        CheckStopPlayerControls();
    }

    public void OpenStatusPanel()
    {
        isStatusPanelOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        if (statusPanel != null)
        {
            statusPanel.gameObject.SetActive(true);
            statusPanel.UpdateStatus(FindObjectOfType<Player>());
        }
    }

    public void CloseStatusPanel()
    {
        isStatusPanelOpen = false;
        statusPanel?.ClosePanel();
        CheckStopPlayerControls();
    }

    public void OpenConquestPanel()
    {
        EnsureUIRootIsActive();
        CloseAllPanels();

        if (conquestUI == null)
        {
            Debug.LogWarning("[UI] Conquest UI not assigned.");
            return;
        }

        conquestUI.gameObject.SetActive(true);
        conquestUI.OpenRosterFirst();   // always show roster first
        isConquestOpen = true;
    }

    public void CloseConquest()
    {
        isConquestOpen = false;
        if (conquestUI != null) conquestUI.Close();
        CheckStopPlayerControls();
    }

    private CharacterProfileSO TryGetProfile(Entity_Stats s)
    {
        if (s == null) return null;
        var refComp = s.GetComponent<CharacterProfileRef>();
        return refComp ? refComp.profile : null;
    }

    // ===== Quest Journal Open/Close/Toggle =====

    // --- Replace this ---
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

    // --- Replace this ---
    public void OpenQuestJournalFromUI()
    {
        // keep for API compatibility; just call the coroutine path
        if (!TryFindQuestJournalUI()) return;
        StartCoroutine(OpenJournal_Co());
    }

    // --- NEW: open deferred next frame ---
    private IEnumerator OpenJournal_Co()
    {
        EnsureUIRootIsActive();
        CloseAllPanels(); // close others first

        if (questJournalRoot != null) questJournalRoot.SetActive(true);
        questJournalUI.gameObject.SetActive(true);

        // Wait one frame so QuestMachine UI can run OnEnable/Start/layout first.
        yield return null;

        // Safely try common open/show methods without ambiguity:
        if (!SafeInvokeNoArgs(questJournalUI, "Show"))
            if (!SafeInvokeNoArgs(questJournalUI, "OpenWindow"))
                SafeInvokeBool(questJournalUI, "SetVisible", true);

        isQuestJournalOpen = true;
        StopPlayerControls(true);
    }

    // --- Replace this ---
    public void CloseQuestJournalFromUI()
    {
        if (!TryFindQuestJournalUI()) return;

        if (!SafeInvokeNoArgs(questJournalUI, "Hide"))
            if (!SafeInvokeNoArgs(questJournalUI, "CloseWindow"))
                SafeInvokeBool(questJournalUI, "SetVisible", false);

        questJournalUI.gameObject.SetActive(false);
        if (questJournalRoot != null) questJournalRoot.SetActive(false);

        isQuestJournalOpen = false;

        if (reopenMainMenuAfterJournalClose) OpenMainMenuDirect();
        else CheckStopPlayerControls();
    }

    // --- Add these helpers anywhere inside UI.cs ---
    private bool TryFindQuestJournalUI()
    {
        if (questJournalUI != null) return true;
        questJournalUI = FindFirstObjectByType<UnityUIQuestJournalUI>(FindObjectsInactive.Include);
        if (questJournalUI == null) return false;
        if (questJournalRoot == null) questJournalRoot = questJournalUI.transform.root.gameObject;
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

    // ===== Save Panel =====

    public void OpenSavePanel()
    {
        EnsureUIRootIsActive();
        CloseAllPanels();

        if (saveLoadPanel == null)
            saveLoadPanel = FindFirstObjectByType<UI_SaveLoadPanel>(FindObjectsInactive.Include);

        if (saveLoadPanel == null)
        {
            Debug.LogError("[UI] UI_SaveLoadPanel not found in scene.");
            return;
        }

        // Ensure the holder object is active so its inner 'panel' can show
        saveLoadPanel.gameObject.SetActive(true);

        saveLoadPanel.OpenForSave();   // or OpenForLoad();
        isSaveOpen = true;
    }

    public void CloseSavePanel()
    {
        isSaveOpen = false;
        if (saveLoadPanel != null)
        {
            saveLoadPanel.ClosePanel();
        }
        CheckStopPlayerControls();
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

    public void OpenMerchant(Inventory_Merchant merchant, Inventory_Player playerInv)
    {
        EnsureUIRootIsActive();
        CloseAllPanels();

        if (MerchantUI != null)
        {
            MerchantUI.gameObject.SetActive(true);
            MerchantUI.SetUpMerchantUI(merchant, playerInv);
        }

        StopPlayerControls(true);
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

        CheckStopPlayerControls();
    }

    public void OpenMainMenuDirect()
    {
        EnsureUIRootIsActive();
        CloseAllPanels();
        mainMenuPanel?.SetActive(true);

        // Refresh menu HP/MP immediately and start polling if needed
        ForceRefreshMenuBars();
        StartMenuPollIfNeeded();

        StopPlayerControls(true);
    }

    #endregion

    #region Input Control

    public void StopPlayerControls(bool stopGameplay)
    {
        player.controllers.maps.SetMapsEnabled(!stopGameplay, "Default");
    }

    private void CheckStopPlayerControls()
    {
        if (!IsAnySubPanelOpen() && (mainMenuPanel == null || !mainMenuPanel.activeSelf))
        {
            StopPlayerControls(false);
            StopMenuPoll();
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
        StopPlayerControls(true);
    }

    public void CloseAllPanels()
    {
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

        ResetStates();
        StopMenuPoll();
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

        // If we’re picking a slot, Esc exits assign mode (restores the Skill Tree)
        if (_assignPreviewActive)
        {
            HideHotbarAssignPreview();
            return;
        }



        if (inventoryUI != null && inventoryUI.IsOpen() && inventoryUI.HandleCancel()) return;

        if (equipmentInventoryPanel != null && equipmentInventoryPanel.IsOpen && equipmentInventoryPanel.HandleCancel())
        {
            Debug.Log("[UI] Equipment panel handled cancel.");
            return;
        }

        if (craftUI != null && isCraftOpen && craftUI.HandleCancel())
        {
            Debug.Log("[UI] Craft panel handled cancel.");
            return;
        }

        if (merchantUI != null && merchantUI.IsOpen && merchantUI.HandleCancel())
        {
            Debug.Log("[UI] Merchant panel handled cancel.");
            return;
        }

        // Let the tree close itself if it’s visible
        if (skillTreeUI != null && skillTreeUI.gameObject.activeInHierarchy && skillTreeUI.HandleCancel()) return;

        if (statusPanel != null && isStatusPanelOpen && statusPanel.HandleCancel()) return;

        if (optionsUI != null && isOptionsOpen)
        {
            CloseOptions();
            return;
        }

        if (conquestUI != null && conquestUI.gameObject.activeInHierarchy)
        {
            Debug.Log("[UI] Conquest panel handling cancel...");
            if (conquestUI.HandleCancel()) return;
        }

        // Quest Journal close on cancel:
        if (questJournalUI != null && questJournalUI.gameObject.activeInHierarchy)
        {
            CloseQuestJournalFromUI();
            return;
        }

        // Save/Load panel handles cancel
        if (saveLoadPanel != null && saveLoadPanel.IsOpen && saveLoadPanel.HandleCancel())
        {
            if (!saveLoadPanel.IsOpen) isSaveOpen = false;
            return;
        }

        if (merchantUI != null && isMerchantOpen)
        {
            CloseMerchant();
            return;
        }

        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            mainMenuPanel.SetActive(false);
            CheckStopPlayerControls();
            return;
        }

        Debug.Log("[UI] No panels handled cancel.");
    }

    #endregion

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, null);
        statToolTip?.ShowToolTip(false, null);
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

    private void ShowConfirm(string message, System.Action onYes, System.Action onNo)
    {
        onYes?.Invoke();
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

    private void HandleSecondTick(int _)
    {
        UpdateTimePlayedLabelImmediate();
    }

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateLocationLabel();
    }

    private void UpdateTimePlayedLabelImmediate()
    {
        if (timePlayedLabel != null)
            timePlayedLabel.text = $"Time Played: {PlayTimeTracker.FormatHHMM(PlayTimeTracker.TotalSecondsInt)}";
    }

    private void UpdateLocationLabel()
    {
        if (locationLabel != null)
            locationLabel.text = $"Location: {SceneManager.GetActiveScene().name}";
    }

    // ====================== Assign preview (fade tree / pass-through) ======================

    // Call this when you ENTER pick mode (right-click an unlocked node)
    // Call this when you ENTER assign mode (right-click an unlocked node)
    public void ShowHotbarAssignPreview(SkillCategory category)
    {
        _assignPreviewActive = true;

        // 1) Hide the Skill Tree completely
        if (skillTreeUI != null)
        {
            _skillTreeWasActive = skillTreeUI.gameObject.activeSelf;
            skillTreeUI.gameObject.SetActive(false);
        }

        // 2) Show the correct hotbar for picking
        if (category == SkillCategory.Sex)
        {
            var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
            if (sexUI != null && !sexUI.IsOpen)
            {
                sexUI.ShowAssignPreview();      // also hides pleasure bars during preview
                _sexUIOpenedByPreview = true;
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

        // 1) Restore the Skill Tree panel
        if (skillTreeUI != null && _skillTreeWasActive == true)
            skillTreeUI.gameObject.SetActive(true);
        _skillTreeWasActive = false;

        // Let UI state know the tree is open so the next Esc goes to main menu
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

    // The only blessed way to exit assign mode
    public void RequestExitAssignPreview()
    {
        // Let the hotbar preview close & restore
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



}
