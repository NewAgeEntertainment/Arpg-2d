using Rewired;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using PixelCrushers;

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

        TrySubscribeGold();
    }

    private void OnEnable()
    {
        // In case of scene reloads, ensure subscription is valid
        TrySubscribeGold();
    }

    private void OnDisable()
    {
        UnsubscribeGold();
    }

    private void OnDestroy()
    {
        UnsubscribeGold();
        if (Instance == this) Instance = null;
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
    }

    public void UpdateGoldUI(int newGoldAmount)
    {
        if (goldText != null)
            goldText.text = $"{newGoldAmount:N0} G:";
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

        // Make sure the GameObject holding UI_SaveLoadPanel is active,
        // otherwise its child 'panel' can't become visible.
        saveLoadPanel.gameObject.SetActive(true);

        // Show it in Save mode (or call OpenForLoad for a load menu)
        saveLoadPanel.OpenForSave();
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

    /// <summary>
    /// Immediate "Go to Title" that resets game state and loads the title scene.
    /// Keeps only ddolEssentials (and SaveSystem/Rewired by default in cleaner).
    /// </summary>
    public void GoToTitleScreenClean()
    {
        StartCoroutine(ReturnToTitle_Co(saveSuspend: false));
    }

    /// <summary>
    /// Show confirm and return to title WITHOUT saving (typical Ys).
    /// </summary>
    public void ReturnToTitle_Ys()
    {
        ShowConfirm("Return to Title?\nUnsaved progress will be lost.",
            onYes: () => StartCoroutine(ReturnToTitle_Co(saveSuspend: false)),
            onNo: null);
    }

    /// <summary>
    /// Optional: create a temporary suspend save, then return to title.
    /// </summary>
    public void ReturnToTitle_WithSuspend()
    {
        ShowConfirm("Suspend and return to Title?",
            onYes: () => StartCoroutine(ReturnToTitle_Co(saveSuspend: true)),
            onNo: null);
    }

    /// <summary>
    /// Title-screen helper; call this from a "Continue" button to auto-load the suspend save if present.
    /// </summary>
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

    /// <summary>
    /// Destroys all root objects in the DontDestroyOnLoad scene except ones explicitly allowed.
    /// Also always preserves SaveSystem and Rewired Input Manager if present.
    /// </summary>
    private void CleanDontDestroyOnLoadExcept(GameObject[] extrasToKeep)
    {
        var keep = new HashSet<GameObject>();
        if (extrasToKeep != null) foreach (var g in extrasToKeep) if (g) keep.Add(g);

        // Always keep SaveSystem & Rewired (remove if your title scene has its own)
        if (PixelCrushers.SaveSystem.hasInstance && PixelCrushers.SaveSystem.instance)
            keep.Add(PixelCrushers.SaveSystem.instance.gameObject);

        var rewired = FindObjectOfType<Rewired.InputManager>(true);
        if (rewired) keep.Add(rewired.gameObject);

        // Destroy everything in DDOL except the allow-list.
        var ddolScene = gameObject.scene;               // this UI lives here too
        var roots = new List<GameObject>();
        ddolScene.GetRootGameObjects(roots);

        for (int i = roots.Count - 1; i >= 0; i--)
        {
            var go = roots[i];
            if (!go) continue;
            if (keep.Contains(go)) continue;

            // IMPORTANT: also destroy THIS UI root so its canvases don't carry over
            Destroy(go);
        }
    }


    // TODO: replace with your actual confirm popup. For now it auto-accepts.
    private void ShowConfirm(string message, System.Action onYes, System.Action onNo)
    {
        onYes?.Invoke();
    }

    // ========================================================================================
}
