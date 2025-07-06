using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    private Player player;
    private Inventory_Player inventory;
    private UI_SkillSlot[] skillSlots;

    [SerializeField] private RectTransform healthRect;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private RectTransform manaRect;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Quick Item Slots")]
    [SerializeField] private float yOffsetQuickItemParent = 150;
    [SerializeField] private Transform quickItemOptionsParent;
    private UI_QuickItemSlotOption[] quickItemOptions;
    private UI_QuickItemSlot[] quickItemSlots;

    private void Start()
    {
        quickItemSlots = GetComponentsInChildren<UI_QuickItemSlot>();


        player = FindObjectOfType<Player>();
        player.health.OnHealthUpdate += UpdateHealthBar; // Subscribe to health update event
        player.mana.OnManaUpdate += UpdateManaBar; // Subscribe to mana update event

        inventory = player.inventory;
        inventory.OnInventoryChange += UpdateQuickSlotsUI;
        inventory.OnQuickSlotUsed += PlayQuickSlotFeedback;

        skillSlots = GetComponentsInChildren<UI_SkillSlot>(true); // Get all skill slots in the UI
    }

    public void PlayQuickSlotFeedback(int slotNumber) => quickItemSlots[slotNumber].SimulateButtonFeedback();


    public void UpdateQuickSlotsUI()
    {
        Inventory_Item[] quickItems = inventory.quickItems;

        int count = Mathf.Min(quickItems.Length, quickItemSlots.Length);
        for (int i = 0; i < count; i++)
            quickItemSlots[i].UpdateQuickSlotUI(quickItems[i]);
    }

    public void OpenQuickItemOptions(UI_QuickItemSlot quickItemSlot, RectTransform targetRect)
    {
        if (quickItemOptions == null)
            quickItemOptions = quickItemOptionsParent.GetComponentsInChildren<UI_QuickItemSlotOption>(true);

        List<Inventory_Item> consumables = inventory.itemList.FindAll(item => item.itemData.itemType == ItemType.Consumable);

        for (int i = 0; i < quickItemOptions.Length; i++)
        {
            if (i < consumables.Count)
            {
                quickItemOptions[i].gameObject.SetActive(true);
                quickItemOptions[i].SetupOption(quickItemSlot, consumables[i]);
            }
            else
                quickItemOptions[i].gameObject.SetActive(false);
        }

        quickItemOptionsParent.position = targetRect.position + Vector3.up * yOffsetQuickItemParent;
    }

    public void HideQuickItemOptions() => quickItemOptionsParent.position = new Vector3(0, 9999);


    public UI_SkillSlot GetSkillSlot(SkillType skillType)
    {
        foreach (var slot in skillSlots)
        {
            if (slot.skillType == skillType)
            {

                slot.gameObject.SetActive(true); // Ensure the skill slot is active
                return slot; // Return the skill slot that matches the requested skill type
            }

        }
        return null; // Return null if no matching skill slot is found
    }

    private void UpdateHealthBar()
    {
        float currentHealth = Mathf.RoundToInt (player.health.GetCurrentHealth());
        float maxHealth = player.stats.GetMaxHealth();

        healthText.text = currentHealth + " / " + maxHealth; // display current health and max health text
        healthSlider.value = player.health.GetHealthPercent(); // update health slider value
    }

    private void UpdateManaBar()
    {
        float currentMana = Mathf.RoundToInt(player.mana.GetCurrentMana());
        float maxMana = player.stats.GetMaxMana();
        manaText.text = currentMana + " / " + maxMana; // display current mana and max mana text
        manaSlider.value = player.mana.GetManaPercent(); // update mana slider value
    }
}

