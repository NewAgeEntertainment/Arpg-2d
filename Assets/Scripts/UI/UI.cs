using Rewired;
using System.Collections;
using UnityEngine;

public class UI : MonoBehaviour
{
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
    [SerializeField] private BookOpenManager bookOpenManager; // DRAG this!
    public UI_Craft craftUI { get; private set; }
    public UI_Merchant merchantUI;

    [Header("Book / Main Menu")]
    [SerializeField] private GameObject bookUI;
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private float bookAnimDuration = 1.0f; // match your animation!

    [Header("Main Menu Panel inside Book")]
    [SerializeField] private GameObject mainMenuPanel; // ✅ This is the panel with the big buttons


    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string toggleInventoryAction = "OpenInventory";
    [SerializeField] private string toggleEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string toggleBookAction = "OpenMainMenu";
    [SerializeField] private string closeAllAction = "CloseMainMenu";


    private Rewired.Player player;
    private Coroutine bookRoutine;

    private void Awake()
    {
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();
        craftUI = GetComponentInChildren<UI_Craft>(true);
        merchantUI = GetComponentInChildren<UI_Merchant>(true);

        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);

        //if (bookAnimator != null) bookAnimator.SetBool("Open", false);

        bookUI.SetActive(false);
        bookAnimator?.SetBool("Open", false);

        mainMenuPanel?.SetActive(false);
    }

    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
    }

    private void Update()
    {
        //if (player.GetButtonDown(toggleSkillTreeAction)) OpenSkillTreeUI();
        //if (player.GetButtonDown(toggleInventoryAction)) OpenInventoryUI();
        //if (player.GetButtonDown(toggleEquipmentAction)) OpenEquipmentUI();
        if (player.GetButtonDown(toggleBookAction))
            ToggleBookMenu();

        if (player.GetButtonDown(closeAllAction))
            CloseAllPanelsAndReturnToIdle(); ;
    }

    public void ToggleBookMenu()
    {
        bool isOpen = bookUI.activeSelf;

        // If the book is not shown at all → show it in closed idle state
        if (!isOpen)
        {
            bookUI.SetActive(true);
            bookAnimator.SetBool("Open", false); // force closed idle
            mainMenuPanel?.SetActive(true);
            Debug.Log("[UI] Book opened in idle closed state with menu.");
        }
        else
        {
            // Book is already visible.
            // If any sub-panel is open → return to idle instead.
            if (IsAnySubPanelOpen())
            {
                CloseAllPanelsAndReturnToIdle();
            }
            else
            {
                // If we’re ALREADY idle (no subpanel open) → fully close the Book UI
                CloseFully();
            }
        }
    }

    private void CloseFully()
    {
        Debug.Log("[UI] Closing Book completely.");
        bookUI.SetActive(false);
        mainMenuPanel?.SetActive(false);
        CloseAllPanels();
    }


    private bool IsAnySubPanelOpen()
    {
        return (inventoryUI != null && inventoryUI.gameObject.activeSelf)
            || (skillTreeUI != null && skillTreeUI.gameObject.activeSelf)
            || (equipmentInventoryPanel != null && equipmentInventoryPanel.gameObject.activeSelf);
    }

    public void OpenInventory()
    {
        OpenPanelWithBook("Inventory");
    }

    public void OpenSkillTree()
    {
        OpenPanelWithBook("SkillTree");
    }

    public void OpenEquipmentUI()
    {
        bookOpenManager.OpenBookIfNeeded(() =>
        {
            CloseAllPanels();
            equipmentInventoryPanel?.gameObject.SetActive(true);
            equipmentInventoryPanel?.UpdateUI();
            Debug.Log("[UI] Equipment UI OPENED after book is open.");
        });
    }

    private void OpenPanelWithBook(string panel)
    {
        if (bookRoutine != null)
            StopCoroutine(bookRoutine);

        // Play open animation
        bookAnimator.SetBool("Open", true);
        mainMenuPanel?.SetActive(false);

        CloseAllPanels();

        bookRoutine = StartCoroutine(OpenPanelAfterBookAnim(panel));
    }

    private IEnumerator OpenPanelAfterBookAnim(string panel)
    {
        yield return new WaitForSeconds(bookAnimDuration);

        switch (panel)
        {
            case "Inventory":
                inventoryUI?.gameObject.SetActive(true);
                inventoryUI?.UpdateUI();
                break;
            case "SkillTree":
                skillTreeUI?.gameObject.SetActive(true);
                break;
            case "Equipment":
                equipmentInventoryPanel?.gameObject.SetActive(true);
                equipmentInventoryPanel?.UpdateUI();
                break;
        }
    }

    public void CloseAllPanelsAndReturnToIdle()
    {
        Debug.Log("[UI] Closing panels + playing Close anim.");

        CloseAllPanels();

        bookAnimator.SetBool("Open", false); // play close anim

        if (bookRoutine != null)
            StopCoroutine(bookRoutine);

        StartCoroutine(ReturnToMainIdleAfterClose());
    }

    private IEnumerator ReturnToMainIdleAfterClose()
    {
        yield return new WaitForSeconds(bookAnimDuration);

        mainMenuPanel?.SetActive(true);
    }

    private void CloseAllPanels()
    {
        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
    }

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, null);
        statToolTip?.ShowToolTip(false, null);
    }






}
