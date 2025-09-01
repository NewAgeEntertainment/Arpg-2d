using System.Collections;
using System.Collections.Generic;
using System.Reflection;          // <— for reading toast timings from the prefab
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

    private Player _subscribedPlayer;
    private Player_Stats _subscribedStats;

    // ==== Item Pickup Toasts (SEQUENTIAL) ====
    [Header("Item Pickup Toasts")]
    [Tooltip("Anchor (RectTransform) where the toast appears. Place it anywhere on your HUD.")]
    [SerializeField] private RectTransform pickupToastParent;
    [Tooltip("Prefab with UI_ItemPickupToast on the root.")]
    [SerializeField] private UI_ItemPickupToast toastPrefab;
    [Tooltip("SFX to play each time a toast is shown.")]
    [SerializeField] private AudioClip itemPickupClip;
    [SerializeField] private float itemPickupVolume = 1f;

    [Header("Toast Queue Settings")]
    [Tooltip("If > 0, overrides toast duration (seconds). Otherwise we read fadeIn+hold+fadeOut from the prefab via reflection.")]
    [SerializeField] private float toastLifetimeOverride = 0f;
    [Tooltip("Max queued toasts to avoid runaway enqueue when looting huge piles.")]
    [SerializeField] private int maxQueue = 50;
    [Tooltip("Small padding added to the computed lifetime so back-to-back toasts never overlap.")]
    [SerializeField] private float lifetimePadding = 0.05f;

    // Queue internals
    private struct ToastRequest
    {
        public Sprite icon;
        public string name;
        public int amount;
    }

    private readonly Queue<ToastRequest> _toastQueue = new();
    private bool _isPlayingQueue = false;
    private float _cachedToastLifetime = -1f;

    private void Awake()
    {
        rplayer = ReInput.players.GetPlayer(playerID);

        // Try initial inventory (may be rehooked later)
        playerInventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChange += UpdateQuickSlots;
            playerInventory.OnGoldChanged += UpdateGoldDisplay;
        }
    }

    private void Start()
    {
        HookPlayer(FindFirstObjectByType<Player>(FindObjectsInactive.Include));

        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
        UpdateHealthBar();
        UpdateManaBar();
        UpdateQuickSlots();

        StartCoroutine(ForceOneMorePaintNextFrame());
    }

    private IEnumerator ForceOneMorePaintNextFrame()
    {
        yield return null;
        ForceRefreshFromCurrentState();
    }

    private void OnEnable()
    {
        if (_subscribedPlayer == null)
            HookPlayer(FindFirstObjectByType<Player>(FindObjectsInactive.Include));
    }

    private void OnDisable()
    {
        UnhookPlayer();
    }

    private void OnDestroy()
    {
        UnhookPlayer();

        if (playerInventory != null)
        {
            playerInventory.OnGoldChanged -= UpdateGoldDisplay;
            playerInventory.OnInventoryChange -= UpdateQuickSlots;
        }
    }

    // ========== Hook / Unhook ==========

    private void HookPlayer(Player p)
    {
        if (p == _subscribedPlayer && p != null) return;

        UnhookPlayer();
        _subscribedPlayer = p;

        if (_subscribedPlayer == null) return;

        // Cache inventory & subscribe
        playerInventory = _subscribedPlayer.GetComponent<Inventory_Player>();
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChange -= UpdateQuickSlots;
            playerInventory.OnGoldChanged -= UpdateGoldDisplay;
            playerInventory.OnInventoryChange += UpdateQuickSlots;
            playerInventory.OnGoldChanged += UpdateGoldDisplay;
        }

        // HP/MP events
        if (_subscribedPlayer.health != null) _subscribedPlayer.health.OnHealthUpdate += UpdateHealthBar;
        if (_subscribedPlayer.mana != null) _subscribedPlayer.mana.OnManaUpdate += UpdateManaBar;

        // EXP events from STATS
        _subscribedStats = _subscribedPlayer.stats;
        if (_subscribedStats != null)
        {
            _subscribedStats.OnExpChanged += OnExpChanged;
            _subscribedStats.OnSexExpChanged += OnSexExpChanged;

            // Immediate paint from stats
            OnExpChanged(_subscribedStats.CurrentEXP, _subscribedStats.GetNextLevelRequirement());
            OnSexExpChanged(_subscribedStats.CurrentSexEXP,
                            _subscribedStats.GetNextSexLevelRequirement(),
                            _subscribedStats.CurrentSexLevel);
        }

        // Also store a direct reference for convenience in other methods
        player = _subscribedPlayer;

        // Paint other bits
        UpdateQuickSlots();
        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
        UpdateHealthBar();
        UpdateManaBar();
    }

    private void UnhookPlayer()
    {
        if (_subscribedPlayer != null)
        {
            if (_subscribedPlayer.health != null)
                _subscribedPlayer.health.OnHealthUpdate -= UpdateHealthBar;
            if (_subscribedPlayer.mana != null)
                _subscribedPlayer.mana.OnManaUpdate -= UpdateManaBar;
        }

        if (_subscribedStats != null)
        {
            _subscribedStats.OnExpChanged -= OnExpChanged;
            _subscribedStats.OnSexExpChanged -= OnSexExpChanged;
            _subscribedStats = null;
        }

        _subscribedPlayer = null;
    }

    // ========== Input for quick slots ==========

    private void Update()
    {
        if (playerInventory == null || rplayer == null) return;

        if (rplayer.GetButtonDown(quickSlot1Action)) playerInventory.TryUseQuickItemInSlot(1);
        if (rplayer.GetButtonDown(quickSlot2Action)) playerInventory.TryUseQuickItemInSlot(2);
        if (rplayer.GetButtonDown(quickSlot3Action)) playerInventory.TryUseQuickItemInSlot(3);
        if (rplayer.GetButtonDown(quickSlot4Action)) playerInventory.TryUseQuickItemInSlot(4);
    }

    // ========== GOLD UI ==========

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

    // ========== HEALTH & MANA ==========

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

    // ========== EXP & SEX EXP (events) ==========

    private void OnExpChanged(float current, float next)
    {
        float ratio = next > 0.0001f ? Mathf.Clamp01(current / next) : 0f;

        if (expSlider != null)
            expSlider.value = ratio;

        if (expText != null)
            expText.text = $"EXP: {current:F0} / {next:F0}";
    }

    private void OnSexExpChanged(float current, float next, int level)
    {
        float ratio = next > 0.0001f ? Mathf.Clamp01(current / next) : 0f;

        if (sexExpSlider != null)
            sexExpSlider.value = ratio;

        if (sexExpText != null)
            sexExpText.text = $"Sex Lv {level}  {current:F0}/{next:F0}";
    }

    // Legacy/direct refresh entry points (safe no-ops if events already fired)
    public void UpdateExpBar()
    {
        if (player == null || player.stats == null) return;
        OnExpChanged(player.stats.CurrentEXP, player.stats.GetNextLevelRequirement());
    }

    public void UpdateSexExpBar()
    {
        if (player == null || player.stats == null) return;
        OnSexExpChanged(player.stats.CurrentSexEXP, player.stats.GetNextSexLevelRequirement(), player.stats.CurrentSexLevel);
    }

    // ========== QUICK SLOT UI ==========

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

    public UI_SkillSlot GetSkillSlot(SkillType type)
    {
        foreach (var slot in skillSlots)
            if (slot != null && slot.skillType == type) return slot;

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
            if (slot != null) slot.SetupSkillSlot(n.skillData);
        }
    }

    // ========== Bootstrap / resiliency ==========

    public void ForceRefreshFromCurrentState()
    {
        if (player == null)
            HookPlayer(FindFirstObjectByType<Player>(FindObjectsInactive.Include));
        else
            HookPlayer(player); // ensures events are hooked

        UpdateQuickSlots();
        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
        UpdateExpBar();
        UpdateSexExpBar();
        UpdateHealthBar();
        UpdateManaBar();
    }

    // ========== ITEM PICKUP TOASTS (SEQUENTIAL) ==========

    /// <summary>
    /// Public entry point: enqueue a toast. Only one toast is shown at a time.
    /// </summary>
    public void ShowItemPickup(Sprite icon, string itemName, int amount = 1)
    {
        if (toastPrefab == null || pickupToastParent == null) return;

        // cap queue size
        if (_toastQueue.Count >= maxQueue) _toastQueue.Dequeue();

        _toastQueue.Enqueue(new ToastRequest
        {
            icon = icon,
            name = itemName,
            amount = amount
        });

        if (!_isPlayingQueue) StartCoroutine(ProcessToastQueue());
    }

    private IEnumerator ProcessToastQueue()
    {
        _isPlayingQueue = true;

        float lifetime = GetToastLifetime(); // unscaled seconds

        while (_toastQueue.Count > 0)
        {
            var req = _toastQueue.Dequeue();

            // Spawn + setup
            var toast = Instantiate(toastPrefab, pickupToastParent);
            toast.gameObject.SetActive(true);
            toast.Setup(req.icon, req.name, req.amount);

            // SFX
            if (itemPickupClip != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(itemPickupClip, Camera.main.transform.position, itemPickupVolume);

            // Wait for this toast to finish (the toast anim uses unscaled time)
            yield return new WaitForSecondsRealtime(lifetime + lifetimePadding);

            // (toast deactivates/destroys itself in its own script)
        }

        _isPlayingQueue = false;
    }

    /// <summary>
    /// Reads fadeIn + hold + fadeOut from the toast prefab via reflection (private fields),
    /// unless an explicit override is provided. Fallback = 1.75s.
    /// </summary>
    private float GetToastLifetime()
    {
        if (toastLifetimeOverride > 0f) return toastLifetimeOverride;

        if (_cachedToastLifetime > 0f) return _cachedToastLifetime;

        const float fallback = 1.75f; // 0.15 + 1.25 + 0.35 (defaults from the sample)
        if (toastPrefab == null) return fallback;

        try
        {
            var t = toastPrefab.GetType();
            var fIn = t.GetField("fadeIn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fHold = t.GetField("hold", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fOut = t.GetField("fadeOut", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            float vin = fIn != null ? (float)fIn.GetValue(toastPrefab) : 0.15f;
            float vhold = fHold != null ? (float)fHold.GetValue(toastPrefab) : 1.25f;
            float vout = fOut != null ? (float)fOut.GetValue(toastPrefab) : 0.35f;

            _cachedToastLifetime = Mathf.Max(0.1f, vin + vhold + vout);
            return _cachedToastLifetime;
        }
        catch
        {
            return fallback;
        }
    }
}
