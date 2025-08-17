using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Rewired;

public class UI_InGame : MonoBehaviour
{
    private Player player;
    private Rewired.Player rplayer;

    [Header("Gold UI")]
    [SerializeField] private TextMeshProUGUI goldTotalText;
    [SerializeField] private GameObject goldDisplayRoot;
    [SerializeField] private TextMeshProUGUI goldGainText;
    [SerializeField] private float goldDisplayDuration = 2.0f;

    private Coroutine goldGainRoutine;

    [SerializeField] private AudioClip goldPickupClip;
    [SerializeField] private float goldPickupVolume = 1f;

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

    [Header("EXP Bar (Normal Level)")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Sex EXP Bar")]
    [SerializeField] private Slider sexExpSlider;
    [SerializeField] private TextMeshProUGUI sexExpText;

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
        {
            playerInventory.OnInventoryChange += UpdateQuickSlots;
            playerInventory.OnGoldChanged += UpdateGoldDisplay;
        }
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            player.health.OnHealthUpdate += UpdateHealthBar;
            player.mana.OnManaUpdate += UpdateManaBar;
        }

        UpdateHealthBar();
        UpdateManaBar();
        UpdateQuickSlots();
        UpdateExpBar();
        UpdateSexExpBar();

        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
    }

    private void Update()
    {
        if (playerInventory == null || rplayer == null) return;

        if (rplayer.GetButtonDown(quickSlot1Action)) playerInventory.TryUseQuickItemInSlot(1);
        if (rplayer.GetButtonDown(quickSlot2Action)) playerInventory.TryUseQuickItemInSlot(2);
        if (rplayer.GetButtonDown(quickSlot3Action)) playerInventory.TryUseQuickItemInSlot(3);
        if (rplayer.GetButtonDown(quickSlot4Action)) playerInventory.TryUseQuickItemInSlot(4);
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnGoldChanged -= UpdateGoldDisplay;
            playerInventory.OnInventoryChange -= UpdateQuickSlots;
        }

        if (player != null)
        {
            if (player.health != null) player.health.OnHealthUpdate -= UpdateHealthBar;
            if (player.mana != null) player.mana.OnManaUpdate -= UpdateManaBar;
        }
    }

    // ------------------------------
    // GOLD UI
    // ------------------------------
    public void UpdateGoldDisplay(int currentGold)
    {
        if (goldTotalText != null)
            goldTotalText.text = $"{currentGold:N0} G";
    }

    public void ShowGoldPickup(int goldAmount)
    {
        if (goldGainRoutine != null)
            StopCoroutine(goldGainRoutine);

        if (goldGainText != null)
            goldGainText.text = $"+{goldAmount:N0} G";

        if (goldDisplayRoot != null)
            goldDisplayRoot.SetActive(true);

        if (goldPickupClip != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(goldPickupClip, Camera.main.transform.position, goldPickupVolume);

        goldGainRoutine = StartCoroutine(HideGoldGainAfterDelay());
    }

    private IEnumerator HideGoldGainAfterDelay()
    {
        yield return new WaitForSeconds(goldDisplayDuration);
        if (goldDisplayRoot != null)
            goldDisplayRoot.SetActive(false);
    }

    // ------------------------------
    // HEALTH & MANA
    // ------------------------------
    private void UpdateHealthBar()
    {
        if (player == null || player.stats == null || player.health == null) return;

        float currentHealth = Mathf.RoundToInt(player.health.GetCurrentHealth());
        float maxHealth = player.stats.GetMaxHealth();

        if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";
        if (healthSlider != null) healthSlider.value = player.health.GetHealthPercent();
    }

    private void UpdateManaBar()
    {
        if (player == null || player.stats == null || player.mana == null) return;

        if (manaText != null)
            manaText.text = $"{Mathf.RoundToInt(player.mana.GetCurrentMana())}/{player.stats.GetMaxMana()}";

        if (manaSlider != null)
            manaSlider.value = player.mana.GetManaPercent();
    }

    // ------------------------------
    // EXP BAR (NORMAL)
    // ------------------------------
    public void UpdateExpBar()
    {
        if (player == null) return;

        float currentExp = player.CurrentExp;
        float nextLevelExp = player.NextLevelExp;

        if (expSlider != null) expSlider.value = nextLevelExp > 0 ? currentExp / nextLevelExp : 0f;
        if (expText != null) expText.text = $"EXP: {currentExp:F0} / {nextLevelExp:F0}";
    }

    public void UpdateExpBar(float currentExp, float nextLevelExp)
    {
        if (expSlider != null) expSlider.value = nextLevelExp > 0 ? currentExp / nextLevelExp : 0f;
        if (expText != null) expText.text = $"EXP: {currentExp:F0} / {nextLevelExp:F0}";
    }

    // ------------------------------
    // SEX EXP BAR
    // ------------------------------
    public void UpdateSexExpBar()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null) return;
        }

        float currentSexExp = player.CurrentSexExp;
        float nextSexExp = player.NextSexLevelSexExp;
        int sexLevel = player.SexLevel;

        if (sexExpSlider != null)
            sexExpSlider.value = nextSexExp > 0 ? currentSexExp / nextSexExp : 0f;

        if (sexExpText != null)
            sexExpText.text = $"Sex Lv {sexLevel}  {currentSexExp:F0}/{nextSexExp:F0}";
    }

    // ------------------------------
    // QUICK SLOT UI
    // ------------------------------
    public void UpdateQuickSlots()
    {
        if (playerInventory == null) return;

        if (playerInventory.quickSlots.Length < 4)
        {
            Debug.LogError("[UI_InGame] quickSlots does not have length 4!");
            return;
        }

        if (quickSlot1) quickSlot1.UpdateQuickSlotUI(playerInventory.quickSlots[0]);
        if (quickSlot2) quickSlot2.UpdateQuickSlotUI(playerInventory.quickSlots[1]);
        if (quickSlot3) quickSlot3.UpdateQuickSlotUI(playerInventory.quickSlots[2]);
        if (quickSlot4) quickSlot4.UpdateQuickSlotUI(playerInventory.quickSlots[3]);
    }

    // ------------------------------
    // SKILL SLOT ACCESS
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

    public void RefreshSkillSlotsFromTree(UI_SkillTree tree)
    {
        if (tree == null) return;

        var nodes = tree.GetComponentsInChildren<UI_TreeNode>(true);
        if (nodes == null) return;

        foreach (var n in nodes)
        {
            if (n == null || !n.isUnlocked || n.skillData == null) continue;

            var slot = GetSkillSlot(n.skillData.skillType);
            if (slot != null)
                slot.SetupSkillSlot(n.skillData);
        }
    }

    // ------------------------------
    // ONE-SHOT BOOTSTRAP
    // ------------------------------
    public void ForceRefreshFromCurrentState()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (playerInventory == null)
        {
            if (player != null) playerInventory = player.GetComponent<Inventory_Player>();
            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChange -= UpdateQuickSlots;
                playerInventory.OnGoldChanged -= UpdateGoldDisplay;
                playerInventory.OnInventoryChange += UpdateQuickSlots;
                playerInventory.OnGoldChanged += UpdateGoldDisplay;
            }
        }

        UpdateQuickSlots();
        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
        UpdateExpBar();
        UpdateSexExpBar();
        UpdateHealthBar();
        UpdateManaBar();
    }
}
