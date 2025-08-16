using Rewired;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using PixelCrushers;
using UnityEngine.UI;
using System.Reflection;

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

    // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    // New: Menu Health/Mana bindings (drag your MENU UI widgets here, not the HUD ones)
    [Header("Main Menu - Health & Mana (drag from Menu UI)")]
    [SerializeField] private Slider menuHealthSlider;
    [SerializeField] private TMP_Text menuHealthText;
    [SerializeField] private Slider menuManaSlider;
    [SerializeField] private TMP_Text menuManaText;

    [Header("Menu Bars Update (fallback polling)")]
    [SerializeField] private float menuPollInterval = 0.1f;
    // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string openSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string openInventoryAction = "OpenInventory";
    [SerializeField] private string openEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string openOptionsAction = "OpenOptions";
    [SerializeField] private string openMainMenuAction = "OpenMainMenu";
    [SerializeField] private string cancelAction = "UICancel";
    [SerializeField] private string openSavePanelAction = "OpenSavePanel"; // optional

    private Rewired.Player player;

    private bool isInventoryOpen = false;
    private bool isSkillTreeOpen = false;
    private bool isEquipmentOpen = false;
    private bool isStatusPanelOpen = false;
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
    }

    private void OnEnable()
    {
        // Re-arm gold subscription in case of scene reload
        TrySubscribeGold();

        // Rebind stats after scene loads
        SceneManager.sceneLoaded += OnSceneLoaded_UIRefresh;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_UIRefresh;
        UnsubscribeGold();
        UnhookStatEvents();
        StopMenuPoll();
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

        // optional: hotkey for save panel
        if (player.GetButtonDown(openSavePanelAction)) OpenSavePanel();
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

    // UI.cs
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
               || isStorageOpen || isMerchantOpen || isCraftOpen || isStatusPanelOpen;
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
        optionsUI?.gameObject.SetActive(false);
        storageUI?.gameObject.SetActive(false);
        merchantUI?.gameObject.SetActive(false);
        craftUI?.gameObject.SetActive(false);
        mainMenuPanel?.SetActive(false);

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
        isStorageOpen = false;
        isMerchantOpen = false;
        isCraftOpen = false;
    }

    public void HandleBackAction()
    {
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

        if (skillTreeUI != null && isSkillTreeOpen && skillTreeUI.HandleCancel()) return;
        if (statusPanel != null && isStatusPanelOpen && statusPanel.HandleCancel()) return;

        if (optionsUI != null && isOptionsOpen)
        {
            CloseOptions();
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
        // Modern Unity: FindFirstObjectByType<T>() only takes an optional Inactive flag.
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
}
