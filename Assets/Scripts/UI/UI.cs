using Rewired;
using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    #region Components
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public Inventory_Item hoveredItem;
    public UI_StatToolTip statToolTip { get; private set; }

    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Root container for ALL UI")]
    [SerializeField] private GameObject uiRoot;

    [Header("Main UI Panels")]
    [SerializeField] private UI_Inventory inventoryUI;
    public UI_Inventory InventoryUI => inventoryUI;
    [SerializeField] private UI_SkillTree skillTreeUI;
    public UI_SkillTree SkillTreeUI => skillTreeUI;

    [SerializeField] private UI_Storage storageUI;
    public UI_Storage StorageUI => storageUI;

    [SerializeField] private UI_Merchant merchantUI;
    public UI_Merchant MerchantUI => merchantUI;

    [SerializeField] private UI_Craft craftUI;
    public UI_Craft CraftUI => craftUI;

    [SerializeField] private UI_EquipmentInventory equipmentInventoryPanel;
    public UI_InGame inGameUI;
    public UI_Options optionsUI { get; private set; }

    public UI_HealthBar playerHealthBar;
    public UI_ManaBar playerManaBar;

    [Header("Main Menu Panel")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string toggleInventoryAction = "OpenInventory";
    [SerializeField] private string toggleEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string toggleOptionsAction = "OpenOptions";
    [SerializeField] private string toggleMainMenuAction = "OpenMainMenu";
    [SerializeField] private string closeAllAction = "CloseMainMenu";

    #endregion

    private Rewired.Player player;

    private bool isInventoryOpen = false;
    private bool isSkillTreeOpen = false;
    private bool isEquipmentOpen = false;
    private bool isOptionsOpen = false;
    private bool isStorageOpen = false;
    private bool isMerchantOpen = false;
    private bool isCraftOpen = false;

    private void Awake()
    {
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();
        craftUI = GetComponentInChildren<UI_Craft>(true);
        merchantUI = GetComponentInChildren<UI_Merchant>(true);
        storageUI = GetComponentInChildren<UI_Storage>(true);
        inGameUI = GetComponentInChildren<UI_InGame>(true);
        optionsUI = GetComponentInChildren<UI_Options>(true);

        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
        optionsUI?.gameObject.SetActive(false);
        storageUI?.gameObject.SetActive(false);
        merchantUI?.gameObject.SetActive(false);
        craftUI?.gameObject.SetActive(false);

        mainMenuPanel?.SetActive(false);
        uiRoot?.SetActive(false);

        if (inGameUI == null)
            inGameUI = GetComponentInChildren<UI_InGame>(true);

        Debug.Log($"[UI] Found InGameUI: {inGameUI}");

        if (inGameUI == null) Debug.LogError("[UI] inGameUI is NOT assigned!");
    }

    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
        skillTreeUI.UnlockDefaultSkills();
    }

    private void Update()
    {
        if (player.GetButtonDown(toggleSkillTreeAction)) ToggleSkillTree();
        if (player.GetButtonDown(toggleInventoryAction)) ToggleInventory();
        if (player.GetButtonDown(toggleEquipmentAction)) ToggleEquipment();
        if (player.GetButtonDown(toggleOptionsAction)) ToggleOptions();
        if (player.GetButtonDown(toggleMainMenuAction)) ToggleMainMenu();
        if (player.GetButtonDown(closeAllAction)) CloseAllPanelsAndReset();

        if (player.GetButtonDown(closeAllAction))
        {
            if (inventoryUI != null && inventoryUI.IsOpen() && inventoryUI.HandleCancel()) return;
            if (equipmentInventoryPanel != null && equipmentInventoryPanel.IsOpen && equipmentInventoryPanel.HandleCancel()) return;
            if (skillTreeUI != null && isSkillTreeOpen && skillTreeUI.HandleCancel()) return;
            //if (storageUI != null && isStorageOpen && storageUI.HandleCancel()) return;
            //if (merchantUI != null && isMerchantOpen && merchantUI.HandleCancel()) return;
            //if (craftUI != null && isCraftOpen && craftUI.HandleCancel()) return;
            //if (optionsUI != null && isOptionsOpen && optionsUI.HandleCancel()) return;

            CloseAllPanelsAndReset();
        }
    }

    #region Toggle Methods

    public void ToggleInventory() { if (isInventoryOpen) CloseInventory(); else OpenInventory(); }
    public void ToggleSkillTree() { if (isSkillTreeOpen) CloseSkillTree(); else OpenSkillTree(); }
    public void ToggleEquipment() { if (isEquipmentOpen) CloseEquipment(); else OpenEquipment(); }
    public void ToggleOptions() { if (isOptionsOpen) CloseOptions(); else OpenOptions(); }

    public void ToggleMainMenu()
    {
        bool isOpen = mainMenuPanel.activeSelf;

        if (!isOpen)
        {
            EnsureUIRootIsActive();
            mainMenuPanel?.SetActive(true);
            StopPlayerControls(true);
            Debug.Log("[UI] Main Menu OPENED.");
        }
        else
        {
            CloseAllPanelsAndReset();
            Debug.Log("[UI] Main Menu CLOSED.");
        }
    }

    #endregion

    #region Open/Close Panels

    public void OpenInventory()
    {
        isInventoryOpen = true;
        EnsureUIRootIsActive();

        CloseAllPanels();  // ✅ First close any other panels

        inventoryUI?.gameObject.SetActive(true);  // ✅ FORCE IT ACTIVE
        inventoryUI?.OpenInventory();  // ✅ Then open the Inventory (and it stays ON)
        Debug.Log("[UI] Inventory OPENED.");
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryUI?.CloseInventory();
        Debug.Log("[UI] Inventory CLOSED.");
        CheckStopPlayerControls();
    }

    public void OpenSkillTree()
    {
        isSkillTreeOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        skillTreeUI?.gameObject.SetActive(true);
        Debug.Log("[UI] SkillTree OPENED.");
    }

    public void CloseSkillTree()
    {
        isSkillTreeOpen = false;
        skillTreeUI?.gameObject.SetActive(false);
        Debug.Log("[UI] SkillTree CLOSED.");
        CheckStopPlayerControls();
    }

    public void OpenEquipment()
    {
        isEquipmentOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        equipmentInventoryPanel?.gameObject.SetActive(true);
        equipmentInventoryPanel?.UpdateUI();
        Debug.Log("[UI] Equipment OPENED.");
    }

    public void CloseEquipment()
    {
        isEquipmentOpen = false;
        equipmentInventoryPanel?.gameObject.SetActive(false);
        Debug.Log("[UI] Equipment CLOSED.");
        CheckStopPlayerControls();
    }

    public void OpenOptions()
    {
        isOptionsOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        optionsUI?.gameObject.SetActive(true);
        Debug.Log("[UI] Options OPENED.");
    }

    public void CloseOptions()
    {
        isOptionsOpen = false;
        optionsUI?.gameObject.SetActive(false);
        Debug.Log("[UI] Options CLOSED.");
        CheckStopPlayerControls();
    }

    public void OpenStorage()
    {
        isStorageOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        storageUI?.gameObject.SetActive(true);
        storageUI?.UpdateUI();
        Debug.Log("[UI] Storage OPENED.");
    }

    public void CloseStorage()
    {
        isStorageOpen = false;
        storageUI?.gameObject.SetActive(false);
        Debug.Log("[UI] Storage CLOSED.");
    }

    public void OpenMerchant()
    {
        isMerchantOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        merchantUI?.gameObject.SetActive(true);
        Debug.Log("[UI] Merchant OPENED.");
    }

    public void CloseMerchant()
    {
        isMerchantOpen = false;
        merchantUI?.gameObject.SetActive(false);
        Debug.Log("[UI] Merchant CLOSED.");
        CheckStopPlayerControls();
    }

    public void OpenCraft()
    {
        isCraftOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();
        craftUI?.gameObject.SetActive(true);
        Debug.Log("[UI] Crafting OPENED.");
    }

    public void CloseCraft()
    {
        isCraftOpen = false;
        craftUI?.gameObject.SetActive(false);
        Debug.Log("[UI] Crafting CLOSED.");
    }

    public void CloseAllPanelsAndReset()
    {
        CloseAllPanels();
        mainMenuPanel?.SetActive(false);
        ResetStates();
        StopPlayerControls(false);
        Debug.Log("[UI] All Panels CLOSED.");
    }

    private void EnsureUIRootIsActive()
    {
        // ✅ Do NOT toggle the root itself.
        // If your root is called `UI_ROOT`, it should always be active.
        StopPlayerControls(true);
    }

    public void CloseAllPanels()
    {
        // ✅ ONLY deactivate child panels, not the root
        mainMenuPanel?.SetActive(false);
        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
        optionsUI?.gameObject.SetActive(false);
        storageUI?.gameObject.SetActive(false);
        merchantUI?.gameObject.SetActive(false);
        craftUI?.gameObject.SetActive(false);

        ResetStates();
    }

    private void ResetStates()
    {
        isInventoryOpen = false;
        isSkillTreeOpen = false;
        isEquipmentOpen = false;
        isOptionsOpen = false;
        isStorageOpen = false;
        isMerchantOpen = false;
        isCraftOpen = false;
    }

    #endregion

    #region Input Control

    public void StopPlayerControls(bool stopGameplay)
    {
        player.controllers.maps.SetMapsEnabled(!stopGameplay, "Default");
        Debug.Log("[UI] Player GAMEPLAY maps: " + (stopGameplay ? "DISABLED" : "ENABLED"));
    }

    private void CheckStopPlayerControls()
    {
        if (!IsAnySubPanelOpen() && !mainMenuPanel.activeSelf)
        {
            StopPlayerControls(false);
        }
    }

    private bool IsAnySubPanelOpen()
    {
        return isInventoryOpen || isSkillTreeOpen || isEquipmentOpen || isOptionsOpen || isStorageOpen || isMerchantOpen || isCraftOpen;
    }

    #endregion

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, null);
        statToolTip?.ShowToolTip(false, null);
    }

    public void OpenMainMenuDirect()
    {
        EnsureUIRootIsActive();
        mainMenuPanel?.SetActive(true);
        StopPlayerControls(true);
        Debug.Log("[UI] Main Menu opened directly.");
    }

    public void HandleBackAction()
    {
        if (inventoryUI != null && inventoryUI.IsOpen() && inventoryUI.HandleCancel()) return;
        if (equipmentInventoryPanel != null && equipmentInventoryPanel.IsOpen && equipmentInventoryPanel.HandleCancel()) return;
        if (skillTreeUI != null && isSkillTreeOpen && skillTreeUI.HandleCancel())
        {
            return;
        }
        if (skillTreeUI != null && isSkillTreeOpen && skillTreeUI.HandleCancel()) return;
        if (optionsUI != null && isOptionsOpen) { CloseOptions(); return; }
        if (storageUI != null && isStorageOpen) { CloseStorage(); return; }
        if (merchantUI != null && isMerchantOpen) { CloseMerchant(); return; }
        if (craftUI != null && isCraftOpen) { CloseCraft(); return; }

        CloseAllPanelsAndReset();
    }


}
