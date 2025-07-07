using Rewired;
using System.Collections;
using UnityEngine;

public class UI : MonoBehaviour
{
    #region Components  
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public Inventory_Item hoveredItem;
    public UI_StatToolTip statToolTip { get; private set; }

    [Header("Main UI Panels")]
    [SerializeField] private UI_Inventory inventoryUI;
    [SerializeField] private UI_SkillTree skillTreeUI;
    public UI_SkillTree SkillTreeUI => skillTreeUI;

    [SerializeField] private UI_Storage storageUI;
    public UI_Storage StorageUI => storageUI;

    [SerializeField] private UI_EquipmentInventory equipmentInventoryPanel;
    [SerializeField] private BookOpenManager bookOpenManager;
    public UI_Craft craftUI { get; private set; }
    public UI_Merchant merchantUI;
    public UI_InGame inGameUI;

    [Header("Book / Main Menu")]
    [SerializeField] private GameObject bookUI;
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private float bookAnimDuration = 1.0f;

    [Header("Main Menu Panel inside Book")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string toggleInventoryAction = "OpenInventory";
    [SerializeField] private string toggleEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string toggleBookAction = "OpenMainMenu";
    [SerializeField] private string closeAllAction = "CloseMainMenu";

    private Rewired.Player player;
    private Coroutine bookRoutine;

    private bool isInventoryOpen = false;
    private bool isSkillTreeOpen = false;
    private bool isEquipmentOpen = false;
    #endregion

    private void Awake()
    {
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();
        craftUI = GetComponentInChildren<UI_Craft>(true);
        merchantUI = GetComponentInChildren<UI_Merchant>(true);
        inGameUI = GetComponentInChildren<UI_InGame>(true);

        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);

        bookUI?.SetActive(false);
        bookAnimator?.SetBool("Open", false);
        mainMenuPanel?.SetActive(false);
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
        if (player.GetButtonDown(toggleBookAction)) ToggleBookMenu();
        if (player.GetButtonDown(closeAllAction)) CloseAllPanelsAndReturnToIdle();
    }

    #region Toggle Methods  

    public void ToggleInventory()
    {
        if (isInventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void ToggleSkillTree()
    {
        if (isSkillTreeOpen)
            CloseSkillTree();
        else
            OpenSkillTree();
    }

    public void ToggleEquipment()
    {
        if (isEquipmentOpen)
            CloseEquipment();
        else
            OpenEquipment();
    }
    #endregion

    #region Open / Close  

    public void OpenInventory()
    {
        isInventoryOpen = true;
        StopPlayerControls(true);
        bookOpenManager.OpenBookIfNeeded(() =>
        {
            CloseAllPanels();
            inventoryUI?.gameObject.SetActive(true);
            inventoryUI?.UpdateUI();
            Debug.Log("[UI] Inventory OPENED.");
        });
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryUI?.gameObject.SetActive(false);
        Debug.Log("[UI] Inventory CLOSED.");
        CheckStopPlayerControls();
    }

    public void OpenSkillTree()
    {
        isSkillTreeOpen = true;
        StopPlayerControls(true);
        bookOpenManager.OpenBookIfNeeded(() =>
        {
            CloseAllPanels();
            skillTreeUI?.gameObject.SetActive(true);
            Debug.Log("[UI] SkillTree OPENED.");
        });
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
        StopPlayerControls(true);
        bookOpenManager.OpenBookIfNeeded(() =>
        {
            CloseAllPanels();
            equipmentInventoryPanel?.gameObject.SetActive(true);
            equipmentInventoryPanel?.UpdateUI();
            Debug.Log("[UI] Equipment OPENED.");
        });
    }

    public void CloseEquipment()
    {
        isEquipmentOpen = false;
        equipmentInventoryPanel?.gameObject.SetActive(false);
        Debug.Log("[UI] Equipment CLOSED.");
        CheckStopPlayerControls();
    }
    #endregion

    #region Book Menu  

    public void ToggleBookMenu()
    {
        bool isOpen = bookUI.activeSelf;

        if (!isOpen)
        {
            bookUI.SetActive(true);
            bookAnimator.SetBool("Open", false);
            mainMenuPanel?.SetActive(true);
            StopPlayerControls(true);
            Debug.Log("[UI] Book opened.");
        }
        else
        {
            if (IsAnySubPanelOpen())
                CloseAllPanelsAndReturnToIdle();
            else
                CloseFully();
        }
    }

    private void CloseFully()
    {
        Debug.Log("[UI] Book closed completely.");
        bookUI?.SetActive(false);
        mainMenuPanel?.SetActive(false);
        CloseAllPanels();
        ResetStates();
        StopPlayerControls(false);
    }

    private bool IsAnySubPanelOpen()
    {
        return isInventoryOpen || isSkillTreeOpen || isEquipmentOpen;
    }

    private void CloseAllPanelsAndReturnToIdle()
    {
        Debug.Log("[UI] Closing all panels, returning idle.");
        CloseAllPanels();

        bookAnimator.SetBool("Open", false);

        if (bookRoutine != null)
            StopCoroutine(bookRoutine);

        StartCoroutine(ReturnToMainIdleAfterClose());
    }

    private IEnumerator ReturnToMainIdleAfterClose()
    {
        yield return new WaitForSeconds(bookAnimDuration);
        mainMenuPanel?.SetActive(true);
        ResetStates();
        StopPlayerControls(false);
    }

    private void CloseAllPanels()
    {
        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
        ResetStates();
    }

    private void ResetStates()
    {
        isInventoryOpen = false;
        isSkillTreeOpen = false;
        isEquipmentOpen = false;
    }
    #endregion

    #region Player Control Logic  

    private void StopPlayerControls(bool stopControls)
    {
        player.controllers.maps.SetAllMapsEnabled(!stopControls);
        Debug.Log("[UI] Player controls: " + (stopControls ? "DISABLED" : "ENABLED"));
    }

    private void CheckStopPlayerControls()
    {
        if (!IsAnySubPanelOpen() && !bookUI.activeSelf)
        {
            StopPlayerControls(false);
        }
    }
    #endregion

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, null);
        statToolTip?.ShowToolTip(false, null);
    }
}

