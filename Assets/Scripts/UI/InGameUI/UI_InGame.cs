using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Rewired;

public class UI_InGame : MonoBehaviour
{
    private Player player;
    private Rewired.Player rplayer;

    [Header("Quick Slots")]
    [SerializeField] private UI_QuickItemSlot quickSlot1;
    [SerializeField] private UI_QuickItemSlot quickSlot2;
    [SerializeField] private UI_QuickItemSlot quickSlot3;
    [SerializeField] private UI_QuickItemSlot quickSlot4;

    [Header("Skill Slots")]
    [SerializeField] private List<UI_SkillSlot> skillSlots = new();

    [Header("Health & Mana")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("EXP Bar")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Rewired")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string quickSlot1Action = "QuickSlot1";
    [SerializeField] private string quickSlot2Action = "QuickSlot2";
    [SerializeField] private string quickSlot3Action = "QuickSlot3";
    [SerializeField] private string quickSlot4Action = "QuickSlot4";

    [HideInInspector] public Inventory_Player playerInventory;

    private void Awake()
    {
        rplayer = ReInput.players.GetPlayer(playerID);
        playerInventory = FindFirstObjectByType<Inventory_Player>();

        if (playerInventory != null)
            playerInventory.OnInventoryChange += UpdateQuickSlots;
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();

        player.health.OnHealthUpdate += UpdateHealthBar;
        player.mana.OnManaUpdate += UpdateManaBar;

        UpdateHealthBar();
        UpdateManaBar();
        UpdateQuickSlots();
        UpdateExpBar();
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
    // 📌 HEALTH & MANA
    // ------------------------------
    private void UpdateHealthBar()
    {
        float currentHealth = Mathf.RoundToInt(player.health.GetCurrentHealth());
        float maxHealth = player.stats.GetMaxHealth();

        healthText.text = $"{currentHealth}/{maxHealth}";
        healthSlider.value = player.health.GetHealthPercent();
    }

    private void UpdateManaBar()
    {
        float currentMana = Mathf.RoundToInt(player.mana.GetCurrentMana());
        float maxMana = player.stats.GetMaxMana();

        manaText.text = $"{currentMana}/{maxMana}";
        manaSlider.value = player.mana.GetManaPercent();
    }

    // ------------------------------
    // 📌 EXP BAR
    // ------------------------------
    public void UpdateExpBar()
    {
        float currentExp = player.CurrentExp;
        float nextLevelExp = player.NextLevelExp;

        expSlider.value = currentExp / nextLevelExp;
        expText.text = $"EXP: {currentExp:F0} / {nextLevelExp:F0}";
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
