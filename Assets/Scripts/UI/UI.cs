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
    public UI_Inventory InventoryUI => inventoryUI;
    [SerializeField] private UI_SkillTree skillTreeUI;
    public UI_SkillTree SkillTreeUI => skillTreeUI;
    [SerializeField] private UI_Storage storageUI;
    public UI_Storage StorageUI => storageUI;
    [SerializeField] public UI_EquipmentInventory equipmentInventoryPanel;
    public GameObject UI_Character; // <- drag your whole UI_character GameObject here!
    public UI_Craft craftUI { get; private set; }

    private bool characterUIEnabled = false;
    private bool skillTreeEnabled;
    private bool inventoryUIEnabled;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string toggleInventoryAction = "OpenInventory";
    [SerializeField] private string toggleEquipmentAction = "OpenEquipmentInventory";
    [SerializeField] private string toggleCharacterAction = "OpenCharacter"; // <- make sure this matches your Rewired action name

    private Rewired.Player player;

    private void Awake()
    {
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();

        // ✅ DO NOT auto-find these anymore — you drag them!
        skillTreeEnabled = skillTreeUI.gameObject.activeSelf;
        inventoryUIEnabled = inventoryUI.gameObject.activeSelf;
        craftUI = GetComponentInChildren<UI_Craft>(true);

        if (UI_Character == null)
            Debug.LogError("[UI] UI_Character is not assigned!");

        // start hidden if you want
        if (UI_Character != null)
            UI_Character.SetActive(false);

    }

    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
    }

    public void SwitchOffAllToolTips()
    {
        itemToolTip?.ShowToolTip(false, (Inventory_Item)null);
        statToolTip?.ShowToolTip(false, null);
    }

    private void Update()
    {
        if (player != null && player.GetButtonDown(toggleSkillTreeAction))
        {
            ToggleSkillTreeUI();
        }

        if (player != null && player.GetButtonDown(toggleInventoryAction))
        {
            Debug.Log($"[UI] ToggleInventoryUI triggered at frame: {Time.frameCount}");
            ToggleInventoryUI();
        }

        if (player != null && player.GetButtonDown(toggleEquipmentAction))
        {
            Debug.Log("[UI] ToggleEquipmentInventory triggered by input!");

            if (inventoryUI.equipmentInventoryPanel != null)
                inventoryUI.equipmentInventoryPanel.Toggle();  // ✅ CORRECT!
        }

        if (player != null && player.GetButtonDown(toggleCharacterAction))
        {
            ToggleCharacterUI();
        }
    }

    public void ToggleSkillTreeUI()
    {
        skillTreeEnabled = !skillTreeEnabled;

        skillTreeUI.gameObject.SetActive(skillTreeEnabled);
        skillToolTip.ShowToolTip(false, null);
    }

    public void ToggleInventoryUI()
    {
        

        inventoryUIEnabled = !inventoryUIEnabled;

     

        if (inventoryUIEnabled)
        {
            
            inventoryUI.UpdateUI();
        }
        Debug.Log($"[UI] Will call SetActive({inventoryUIEnabled}) on: {inventoryUI.gameObject.name}");

        inventoryUI.gameObject.SetActive(inventoryUIEnabled);

        Debug.Log($"[UI] GameObject now activeSelf: {inventoryUI.gameObject.activeSelf}");

        //if (inventoryUIEnabled && inventoryUI.equipmentInventoryPanel != null)
        //    inventoryUI.equipmentInventoryPanel.Toggle(false);

        SwitchOffAllToolTips();
    }

    public void ToggleCharacterUI()
    {
        characterUIEnabled = !characterUIEnabled;

        Debug.Log($"[UI] ToggleCharacterUI → {characterUIEnabled}");

        if (UI_Character != null)
            UI_Character.SetActive(characterUIEnabled);
    }
}
