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

    [SerializeField] private UI_StatusPanel statusPanel;
    public UI_StatusPanel StatusPanel => statusPanel;

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

    private Rewired.Player player;

    private bool isInventoryOpen = false;
    private bool isSkillTreeOpen = false;
    private bool isEquipmentOpen = false;
    private bool isStatusPanelOpen = false;
    private bool isOptionsOpen = false;
    private bool isStorageOpen = false;
    private bool isMerchantOpen = false;
    private bool isCraftOpen = false;
    #endregion

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
        skillTreeUI.UnlockDefaultSkills();
    }

    private void Update()
    {
        if (player.GetButtonDown(openSkillTreeAction)) OpenSkillTree();
        if (player.GetButtonDown(openInventoryAction)) OpenInventory();
        if (player.GetButtonDown(openEquipmentAction)) OpenEquipment();
        if (player.GetButtonDown(openOptionsAction)) OpenOptions();
        if (player.GetButtonDown(openMainMenuAction)) OpenMainMenuDirect();
        if (player.GetButtonDown(cancelAction)) HandleBackAction();
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

    public void OpenCraft()
    {
        isCraftOpen = true;
        EnsureUIRootIsActive();
        CloseAllPanels();

        // 🔒 Ensure Main Menu & Storage stay closed
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
            Debug.Log("[UI] Merchant UI closed");
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

        // 🔒 Make sure main menu doesn't pop up when opening other panels
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

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
        mainMenuPanel?.SetActive(false);   // <- ensure MM is closed too

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

        // ✅ Prevent Equipment UI from closing completely when returning to EquippedPanel
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

        // ✅ ADD THIS
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
}
