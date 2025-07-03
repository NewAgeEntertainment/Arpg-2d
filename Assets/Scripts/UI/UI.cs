using Rewired;
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

    [SerializeField] private GameObject UI_Character;

    public UI_Craft craftUI { get; private set; }
    public UI_Merchant merchantUI;

    [Header("Book / Main Menu")]
    [SerializeField] private GameObject bookUI; // ✅ Drag your BookUI here!
    [SerializeField] private Animator bookAnimator;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string toggleInventoryAction = "OpenInventory";
    [SerializeField] private string toggleEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string toggleCharacterAction = "OpenCharacter";
    [SerializeField] private string toggleMainMenuAction = "OpenMainMenu";

    private Rewired.Player player;

    private void Awake()
    {
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();

        craftUI = GetComponentInChildren<UI_Craft>(true);
        merchantUI = GetComponentInChildren<UI_Merchant>(true);

        if (UI_Character != null) UI_Character.SetActive(false);
        if (inventoryUI != null) inventoryUI.gameObject.SetActive(false);
        if (skillTreeUI != null) skillTreeUI.gameObject.SetActive(false);
        if (equipmentInventoryPanel != null) equipmentInventoryPanel.gameObject.SetActive(false);
        if (bookAnimator != null)
            bookAnimator.SetBool("Open", false);
    }

    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
    }

    private void Update()
    {
        if (player.GetButtonDown(toggleSkillTreeAction)) OpenSkillTreeUI();
        if (player.GetButtonDown(toggleInventoryAction)) OpenInventoryUI();
        if (player.GetButtonDown(toggleEquipmentAction)) OpenEquipmentUI();
        if (player.GetButtonDown(toggleCharacterAction)) ToggleCharacterUI();
        if (player.GetButtonDown(toggleMainMenuAction)) ToggleMainMenu();
    }

    public void ToggleBookUI()
    {
        bool isActive = bookUI.activeSelf;
        bookUI.SetActive(!isActive);

        // Whenever we open the BookUI, make sure it starts in "Closed"
        if (!isActive && bookAnimator != null)
            bookAnimator.SetBool("Open", false);

        Debug.Log($"[UI] BookUI now active: {!isActive}");
    }


    private void CloseAllPanels()
    {
        inventoryUI?.gameObject.SetActive(false);
        skillTreeUI?.gameObject.SetActive(false);
        equipmentInventoryPanel?.gameObject.SetActive(false);
        UI_Character?.SetActive(false);
        SwitchOffAllToolTips();
    }

    public void OpenInventoryUI()
    {
        CloseAllPanels();
        Debug.Log("[UI] OpenInventoryUI");
        inventoryUI?.gameObject.SetActive(true);
        inventoryUI?.UpdateUI();
    }

    public void OpenSkillTreeUI()
    {
        CloseAllPanels();
        Debug.Log("[UI] OpenSkillTreeUI");
        skillTreeUI?.gameObject.SetActive(true);
    }

    public void OpenEquipmentUI()
    {
        CloseAllPanels();
        Debug.Log("[UI] OpenEquipmentUI");
        equipmentInventoryPanel?.gameObject.SetActive(true);
        equipmentInventoryPanel?.UpdateUI();
    }

    public void ToggleCharacterUI()
    {
        bool newState = !UI_Character.activeSelf;
        CloseAllPanels();
        UI_Character?.SetActive(newState);
        Debug.Log($"[UI] ToggleCharacterUI → {newState}");
    }

    public void ToggleMainMenu()
    {
        bool isActive = bookUI.activeSelf;
        bookUI.SetActive(!isActive);

        // Whenever we open the BookUI, make sure it starts in "Closed"
        if (!isActive && bookAnimator != null)
            bookAnimator.SetBool("Open", false);

        Debug.Log($"[UI] BookUI now active: {!isActive}");
    }

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, (Inventory_Item)null);
        statToolTip?.ShowToolTip(false, null);
    }
}
