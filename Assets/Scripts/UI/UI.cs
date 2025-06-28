using UnityEngine;
using Rewired;

public class UI : MonoBehaviour
{
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public Inventory_Item hoveredItem; // 👈 tracks which equipment is hovered
    public UI_StatToolTip statToolTip { get; private set; }

    public UI_SkillTree skillTreeUI { get; private set; }
    public UI_Inventory inventoryUI { get; private set; }
    public UI_Storage storageUI { get; private set; }


    private bool skillTreeEnabled;
    private bool inventoryUIEnabled;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";
    [SerializeField] private string toggleInventoryAction = "OpenInventory";

    private Rewired.Player player;

    private void Awake()
    {
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();
        
        skillTreeUI = GetComponentInChildren<UI_SkillTree>(true);
        inventoryUI = GetComponentInChildren<UI_Inventory>(true);
        storageUI = GetComponentInChildren<UI_Storage>(true);

        skillTreeEnabled = skillTreeUI.gameObject.activeSelf;
        inventoryUIEnabled = inventoryUI.gameObject.activeSelf;
    }

    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
    }

    public void SwitchOffAllToolTips()
    {
        skillToolTip.ShowToolTip(false, null);
        itemToolTip.ShowToolTip(false, null);
        statToolTip.ShowToolTip(false, null);
    }

    private void Update()
    {
        if (player != null && player.GetButtonDown(toggleSkillTreeAction))
        {
            ToggleSkillTreeUI();
        }
        
        if (player != null && player.GetButtonDown(toggleInventoryAction))
        {
            ToggleInventoryUI();
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
        inventoryUI.gameObject.SetActive(inventoryUIEnabled);
        statToolTip.ShowToolTip(false, null);
        itemToolTip.ShowToolTip(false, null);
    }
}
