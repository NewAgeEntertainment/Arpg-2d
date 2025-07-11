using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Rewired;

public class UI_InGame : MonoBehaviour
{
    private Player player; // Reference to Player MonoBehaviour (your RPG Player, not Rewired!)
    private Rewired.Player rplayer; // Rewired player input

    [Header("Quick Slots")]
    [SerializeField] private UI_QuickItemSlot quickSlot1;
    [SerializeField] private UI_QuickItemSlot quickSlot2;
    [SerializeField] private UI_QuickItemSlot quickSlot3;
    [SerializeField] private UI_QuickItemSlot quickSlot4;

    [Header("Assign Popup")]
    [SerializeField] private GameObject quickSlotAssignPopup;
    [SerializeField] private TMP_InputField amountInputField;

    [Header("Use or Assign Popup")]
    [SerializeField] private GameObject useOrAssignPopup;

    [Header("Rewired")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string quickSlot1Action = "QuickSlot1";
    [SerializeField] private string quickSlot2Action = "QuickSlot2";
    [SerializeField] private string quickSlot3Action = "QuickSlot3";
    [SerializeField] private string quickSlot4Action = "QuickSlot4";

    [Header("Character Selector Popup")]
    [SerializeField] private UI_CharacterProfilePopup profilePopup;

    [Header("Skill Slots")]
    [SerializeField] private List<UI_SkillSlot> skillSlots = new();

    [Header("Health & Mana")]
    [SerializeField] private RectTransform healthRect;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private RectTransform manaRect;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    [HideInInspector] public Inventory_Player playerInventory;

    private Inventory_Item itemToAssign;
    private Inventory_Item actionPopupItem; // New!
    private void Awake()
    {
        rplayer = ReInput.players.GetPlayer(playerID);
        playerInventory = FindFirstObjectByType<Inventory_Player>();

        if (playerInventory != null)
            playerInventory.OnInventoryChange += UpdateQuickSlots;

        if (quickSlotAssignPopup != null)
            quickSlotAssignPopup.SetActive(false);

        if (useOrAssignPopup != null)
            useOrAssignPopup.SetActive(false);
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();
        
            player.health.OnHealthUpdate += UpdateHealthBar;
            player.mana.OnManaUpdate += UpdateManaBar;

            UpdateHealthBar();
            UpdateManaBar();
        

        UpdateQuickSlots();
    }

    private void Update()
    {
        if (playerInventory == null) return;

        if (rplayer.GetButtonDown(quickSlot1Action))
            playerInventory.TryUseQuickItemInSlot(1);

        if (rplayer.GetButtonDown(quickSlot2Action))
            playerInventory.TryUseQuickItemInSlot(2);

        if (rplayer.GetButtonDown(quickSlot3Action))
            playerInventory.TryUseQuickItemInSlot(3);

        if (rplayer.GetButtonDown(quickSlot4Action))
            playerInventory.TryUseQuickItemInSlot(4);
    }

    // ------------------------------
    // 📌 ASSIGN QUICK SLOT POPUP
    // ------------------------------

    public void OpenAssignQuickSlotPopup(Inventory_Item item)
    {
        itemToAssign = item;

        int currentAmount = 1;
        foreach (var slot in playerInventory.quickSlots)
        {
            if (slot.item == item)
            {
                currentAmount = slot.slotStack;
                break;
            }
        }

        if (amountInputField != null)
            amountInputField.text = currentAmount.ToString();

        quickSlotAssignPopup.SetActive(true);
        Debug.Log($"[UI_InGame] OpenAssignQuickSlotPopup for {item.itemData.itemName}");
    }

    public void AssignSlot1() => AssignSlot(1);
    public void AssignSlot2() => AssignSlot(2);
    public void AssignSlot3() => AssignSlot(3);
    public void AssignSlot4() => AssignSlot(4);

    private void AssignSlot(int slotNumber)
    {
        int amount = GetAmountInput();
        if (amount > 0)
            AssignToQuickSlot(slotNumber, amount);
    }

    private void AssignToQuickSlot(int slotNumber, int amount)
    {
        if (itemToAssign != null && itemToAssign.itemData.itemType == ItemType.Consumable)
        {
            playerInventory.SetQuickItemInSlot(slotNumber, itemToAssign, amount);
            UpdateQuickSlots();
        }

        itemToAssign = null;
        quickSlotAssignPopup.SetActive(false);
    }

    public void OpenCharacterProfilePopup(Inventory_Item item)
    {
        profilePopup.Open(item, player);
    }
    private int GetAmountInput()
    {
        if (amountInputField == null) return 0;

        if (int.TryParse(amountInputField.text, out int amount))
            return amount;

        return 0;
    }

    public void IncreaseAssignAmount()
    {
        if (itemToAssign == null || amountInputField == null) return;

        int current = GetAmountInput();
        current++;

        int totalOwned = 0;
        foreach (var item in playerInventory.itemList)
        {
            if (item.itemData == itemToAssign.itemData)
                totalOwned += item.stackSize;
        }

        int assignedElsewhere = 0;
        foreach (var slot in playerInventory.quickSlots)
        {
            if (slot.item != null && slot.item.itemData == itemToAssign.itemData)
                assignedElsewhere += slot.slotStack;
        }

        int maxPossible = totalOwned;

        if (current > maxPossible) current = maxPossible;

        amountInputField.text = current.ToString();
    }

    public void DecreaseAssignAmount()
    {
        if (itemToAssign == null || amountInputField == null) return;

        int current = GetAmountInput();
        current = Mathf.Max(1, current - 1);

        amountInputField.text = current.ToString();
    }

    // ------------------------------
    // 📌 USE OR ASSIGN POPUP
    // ------------------------------

    public void OpenUseOrAssignPopup(Inventory_Item item)
    {
        actionPopupItem = item;
        useOrAssignPopup.SetActive(true);
    }

    public void ClickUseItem()
    {
        if (actionPopupItem != null)
        {
            playerInventory.TryUseItem(actionPopupItem);
        }
        useOrAssignPopup.SetActive(false);
        actionPopupItem = null;
    }

    public void ClickAssignItem()
    {
        if (actionPopupItem != null)
        {
            OpenAssignQuickSlotPopup(actionPopupItem);
        }
        useOrAssignPopup.SetActive(false);
        actionPopupItem = null;
    }

    // ------------------------------
    // 📌 HEALTH & MANA
    // ------------------------------

    private void UpdateHealthBar()
    {
        float currentHealth = Mathf.RoundToInt(player.health.GetCurrentHealth());
        float maxHealth = player.stats.GetMaxHealth();

        healthText.text = $"{currentHealth}/{maxHealth}";
        healthSlider.value = player.health.GetHealthPercent();
    }

    public void UpdateManaBar()
    {
        float currentMana = Mathf.RoundToInt(player.mana.GetCurrentMana());
        float maxMana = player.stats.GetMaxMana();

        manaText.text = $"{currentMana}/{maxMana}";
        manaSlider.value = player.mana.GetManaPercent();
    }

    // ------------------------------
    // 📌 QUICK SLOT UI
    // ------------------------------

    public void UpdateQuickSlots()
    {
        if (playerInventory.quickSlots.Length < 4)
        {
            Debug.LogError("[UI_InGame] quickSlots does not have length 4!");
            return;
        }

        quickSlot1.UpdateQuickSlotUI(playerInventory.quickSlots[0]);
        quickSlot2.UpdateQuickSlotUI(playerInventory.quickSlots[1]);
        quickSlot3.UpdateQuickSlotUI(playerInventory.quickSlots[2]);
        quickSlot4.UpdateQuickSlotUI(playerInventory.quickSlots[3]);
    }

    // ------------------------------
    // 📌 SKILL SLOT ACCESS
    // ------------------------------

    public UI_SkillSlot GetSkillSlot(SkillType type)
    {
        foreach (var slot in skillSlots)
        {
            if (slot != null && slot.skillType == type)
                return slot;
        }

        Debug.LogWarning($"[UI_InGame] No skill slot found for SkillType: {type}");
        return null;
    }
}
