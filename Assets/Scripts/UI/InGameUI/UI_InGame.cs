using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rewired;
using System.Linq;

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

    [Header("Combat Hotbar (A/B/C/D)")]
    [SerializeField] private List<UI_SkillSlot> skillSlots = new();  // these must be UISkillCategory.Combat

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

    [Header("Rewired – Quick Items")]
    [SerializeField] private string quickSlot1Action = "QuickSlot1";
    [SerializeField] private string quickSlot2Action = "QuickSlot2";
    [SerializeField] private string quickSlot3Action = "QuickSlot3";
    [SerializeField] private string quickSlot4Action = "QuickSlot4";

    [Header("Rewired – Ys-style Skills")]
    [Tooltip("Hold this while pressing A/B/C/D")]
    [SerializeField] private string skillModifierAction = "SkillModifier";
    [SerializeField] private string skillSlotAAction = "SkillSlotA";
    [SerializeField] private string skillSlotBAction = "SkillSlotB";
    [SerializeField] private string skillSlotCAction = "SkillSlotC";
    [SerializeField] private string skillSlotDAction = "SkillSlotD";

    [Header("Default Slot Assignments (Combat only)")]
    [SerializeField] private bool applyDefaultAssignmentsOnStart = true;
    [System.Serializable]
    public struct DefaultSlotAssignment
    {
        public UISkillSlotId slotId;
        public Skill_DataSO skill; // must be category=Combat
    }
    [SerializeField] private List<DefaultSlotAssignment> defaultAssignments = new();

    [HideInInspector] public Inventory_Player playerInventory;

    private Player _subscribedPlayer;
    private Player_Stats _subscribedStats;

    // ==== Item Pickup Toasts ====
    [Header("Item Pickup Toasts")]
    [SerializeField] private RectTransform pickupToastParent;
    [SerializeField] private UI_ItemPickupToast toastPrefab;
    [SerializeField] private AudioClip itemPickupClip;
    [SerializeField] private float itemPickupVolume = 1f;

    [Header("Toast Queue Settings")]
    [SerializeField] private float toastLifetimeOverride = 0f;
    [SerializeField] private int maxQueue = 50;
    [SerializeField] private float lifetimePadding = 0.05f;

    private struct ToastRequest { public Sprite icon; public string name; public int amount; }
    private readonly Queue<ToastRequest> _toastQueue = new();
    private bool _isPlayingQueue = false;
    private float _cachedToastLifetime = -1f;

    // ================= Unity =================

    private void Awake()
    {
        rplayer = ReInput.players.GetPlayer(playerID);

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

        // Example: A/B/C/D in a row
        var selects = skillSlots
            .Where(s => s != null)
            .Select(s => s.GetComponent<Selectable>())
            .Where(s => s != null)
            .ToArray();
        UI ui = UI.Instance;
        if (ui != null) UI.WireLinearNav(selects, horizontal: true);


        StartCoroutine(ForceOneMorePaintNextFrame());
    }

    private IEnumerator ForceOneMorePaintNextFrame()
    {
        yield return null;
        ForceRefreshFromCurrentState();
        ApplyDefaultAssignmentsIfEmpty();     // Combat only
        RefreshAllSkillCostsAndAfford();
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

    // ============== Hook / Unhook ==============

    private void HookPlayer(Player p)
    {
        if (p == _subscribedPlayer && p != null) return;

        UnhookPlayer();
        _subscribedPlayer = p;
        if (_subscribedPlayer == null) return;

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

        if (_subscribedPlayer.health != null) _subscribedPlayer.health.OnHealthUpdate += UpdateHealthBar;
        if (_subscribedPlayer.mana != null) _subscribedPlayer.mana.OnManaUpdate += OnManaChanged;

        _subscribedStats = _subscribedPlayer.stats;
        if (_subscribedStats != null)
        {
            _subscribedStats.OnExpChanged += OnExpChanged;
            _subscribedStats.OnSexExpChanged += OnSexExpChanged;

            OnExpChanged(_subscribedStats.CurrentEXP, _subscribedStats.GetNextLevelRequirement());
            OnSexExpChanged(_subscribedStats.CurrentSexEXP,
                            _subscribedStats.GetNextSexLevelRequirement(),
                            _subscribedStats.CurrentSexLevel);
        }

        player = _subscribedPlayer;

        UpdateQuickSlots();
        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
        UpdateHealthBar();
        UpdateManaBar();
        RefreshAllSkillCostsAndAfford();
    }

    private void UnhookPlayer()
    {
        if (_subscribedPlayer != null)
        {
            if (_subscribedPlayer.health != null)
                _subscribedPlayer.health.OnHealthUpdate -= UpdateHealthBar;
            if (_subscribedPlayer.mana != null)
                _subscribedPlayer.mana.OnManaUpdate -= OnManaChanged;
        }

        if (_subscribedStats != null)
        {
            _subscribedStats.OnExpChanged -= OnExpChanged;
            _subscribedStats.OnSexExpChanged -= OnSexExpChanged;
            _subscribedStats = null;
        }

        _subscribedPlayer = null;
    }

    // ============== Input ==============

    private void Update()
    {
        if (playerInventory == null || rplayer == null) return;

        // Quick items
        if (rplayer.GetButtonDown(quickSlot1Action)) playerInventory.TryUseQuickItemInSlot(1);
        if (rplayer.GetButtonDown(quickSlot2Action)) playerInventory.TryUseQuickItemInSlot(2);
        if (rplayer.GetButtonDown(quickSlot3Action)) playerInventory.TryUseQuickItemInSlot(3);
        if (rplayer.GetButtonDown(quickSlot4Action)) playerInventory.TryUseQuickItemInSlot(4);

        // Ys-style skills via modifier
        bool mod = rplayer.GetButton(skillModifierAction);
        if (!mod) return;

        // Determine if SexyTime UI is visible (if so, prefer sex hotbar first)
        var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
        bool preferSex = sexUI != null && sexUI.IsVisible;

        if (rplayer.GetButtonDown(skillSlotAAction)) HandleSkillHotkeys(0, UISkillSlotId.SlotA, preferSex);
        if (rplayer.GetButtonDown(skillSlotBAction)) HandleSkillHotkeys(1, UISkillSlotId.SlotB, preferSex);
        if (rplayer.GetButtonDown(skillSlotCAction)) HandleSkillHotkeys(2, UISkillSlotId.SlotC, preferSex);
        if (rplayer.GetButtonDown(skillSlotDAction)) HandleSkillHotkeys(3, UISkillSlotId.SlotD, preferSex);
    }

    private void HandleSkillHotkeys(int sexIndex, UISkillSlotId combatId, bool preferSex)
    {
        if (preferSex)
        {
            // Try Sex first; then fall back to Combat with pulse
            if (!TryUseSexSkillFromIndex(sexIndex))
                TryUseCombatSkillFromSlotSmart(combatId, pulseOnFail: true);
        }
        else
        {
            // Try Combat first (no pulse yet); then Sex (which pulses on its own failures)
            if (!TryUseCombatSkillFromSlotSmart(combatId, pulseOnFail: false))
                TryUseSexSkillFromIndex(sexIndex);
        }
    }

    /// <summary>
    /// New: Use a combat skill from bar, returning success. Optionally pulse on failure.
    /// Keeps original behavior intact elsewhere.
    /// </summary>
    private bool TryUseCombatSkillFromSlotSmart(UISkillSlotId id, bool pulseOnFail)
    {
        var slot = FindSlotById(id);
        if (slot == null) return false;
        if (!slot.HasSkill) { if (pulseOnFail) slot.PulseConflict(0.2f); return false; }
        if (slot.slotCategory != UISkillCategory.Combat) { if (pulseOnFail) slot.PulseConflict(0.2f); return false; }

        var data = slot.Data;
        if (player == null || player.skillManager == null || data == null) { if (pulseOnFail) slot.PulseConflict(0.2f); return false; }

        var sm = player.skillManager;

        // Route stateful skills to states — do not manually Commit here.
        switch (data.skillType)
        {
            case SkillType.Dash:
                if (sm.dash != null && sm.dash.CanUseSkillCheck(out _))
                {
                    player.stateMachine.ChangeState(player.dashState);
                    return true;
                }
                else
                {
                    if (pulseOnFail) slot.PulseConflict(0.2f);
                    return false;
                }

            case SkillType.Thrust:
                if (sm.thrust != null && sm.thrust.CanUseSkillCheck(out _))
                {
                    player.stateMachine.ChangeState(player.thrustState);
                    return true;
                }
                else
                {
                    if (pulseOnFail) slot.PulseConflict(0.2f);
                    return false;
                }
        }

        // Instant skills (projectiles, heals, etc.)
        var runtime = sm.GetSkillByType(data.skillType);
        if (runtime == null) { if (pulseOnFail) slot.PulseConflict(0.2f); return false; }

        // TryUseSkill usually does its own gating + commit
        runtime.TryUseSkill();

        // UI feedback
        slot.StartCooldown(slot.CooldownSeconds);
        slot.RefreshText(sm);
        slot.UpdateAffordability(player.mana);

        return true;
    }

    /// <summary>
    /// Original method (kept intact). Now unused by Update(), but preserved as requested.
    /// </summary>
    private void TryUseSkillFromSlot(UISkillSlotId id)
    {
        var slot = FindSlotById(id);
        if (slot == null || !slot.HasSkill) { slot?.PulseConflict(0.2f); return; }

        // Safety: Combat hotbar ONLY
        if (slot.slotCategory != UISkillCategory.Combat) { slot.PulseConflict(0.2f); return; }

        var data = slot.Data;
        if (player == null || player.skillManager == null || data == null) { slot.PulseConflict(0.2f); return; }

        var sm = player.skillManager;

        // Route stateful skills to their states — do not manually Commit here.
        switch (data.skillType)
        {
            case SkillType.Dash:
                if (sm.dash != null && sm.dash.CanUseSkillCheck(out _))
                    player.stateMachine.ChangeState(player.dashState);
                else
                    slot.PulseConflict(0.2f);
                return;

            case SkillType.Thrust:
                if (sm.thrust != null && sm.thrust.CanUseSkillCheck(out _))
                    player.stateMachine.ChangeState(player.thrustState);
                else
                    slot.PulseConflict(0.2f);
                return;
        }

        // Instant skills (projectiles, heals, etc.)
        var runtime = sm.GetSkillByType(data.skillType);
        if (runtime == null) { slot.PulseConflict(0.2f); return; }
        runtime.TryUseSkill(); // does its own checks+commit if implemented
    }

    // New: Try to use a Sex skill from SexyTime hotbar at given index.
    private bool TryUseSexSkillFromIndex(int index)
    {
        var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
        if (sexUI == null) { PulseSexConflict(index, 0.2f); return false; }

        var hotbar = sexUI.Hotbar;
        if (hotbar == null || hotbar.Slots == null) { PulseSexConflict(index, 0.2f); return false; }
        if (index < 0 || index >= hotbar.Slots.Length) { PulseSexConflict(index, 0.2f); return false; }

        var slot = hotbar.Slots[index];
        if (slot == null || !slot.HasSkill) { PulseSexConflict(index, 0.2f); return false; }
        if (slot.slotCategory != UISkillCategory.Sex) { PulseSexConflict(index, 0.2f); return false; }

        var data = slot.Data;
        if (player == null || player.skillManager == null || data == null) { PulseSexConflict(index, 0.2f); return false; }

        var sm = player.skillManager;
        var runtime = sm.GetSkillByType(data.skillType);
        if (runtime == null) { PulseSexConflict(index, 0.2f); return false; }

        // Use the skill (runtime handles gating/commit). Then reflect UI (cooldown/affordability).
        runtime.TryUseSkill();

        slot.StartCooldown(slot.CooldownSeconds);
        slot.RefreshText(sm);
        sexUI.RefreshAffordability(player.mana);

        return true;
    }

    private void PulseSexConflict(int index, float seconds)
    {
        var sexUI = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
        var hotbar = sexUI != null ? sexUI.Hotbar : null;

        if (hotbar != null && hotbar.Slots != null && index >= 0 && index < hotbar.Slots.Length)
        {
            var s = hotbar.Slots[index];
            if (s != null) { s.PulseConflict(seconds); return; }
        }
    }

    // ============== GOLD UI ==============

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

    // ============== HEALTH & MANA UI ==============

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

    private void OnManaChanged()
    {
        UpdateManaBar();
        RefreshAllSkillCostsAndAfford(); // reflect unaffordable overlay immediately
    }

    // ============== EXP UI ==============

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

    // ============== QUICK ITEM UI ==============

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

    // ====== Skill assignment (Combat only) ======

    /// <summary>
    /// Called from the tree when a skill is unlocked. Combat-only in this HUD.
    /// Sex skills are ignored here (they belong to SexyTimeHotbar).
    /// </summary>
    public void NotifySkillUnlocked(SkillType type, Skill_DataSO data, bool preferDefaults = true)
    {
        if (data == null) return;

        // ONLY Combat
        if (data.category != SkillCategory.Combat) return;

        // Already placed? done
        if (FindSlotByType(type) != null) return;

        // Prefer inspector defaults if they match this SO
        if (preferDefaults && defaultAssignments != null)
        {
            foreach (var def in defaultAssignments)
            {
                if (def.skill == data)
                {
                    AssignSkillToSlot(data, def.slotId);
                    RefreshAllSkillCostsAndAfford();
                    return;
                }
            }
        }

        var empty = FirstEmptyCombatSlot();
        if (empty != null)
        {
            AssignSkillToSlot(data, empty.slotId);
            RefreshAllSkillCostsAndAfford();
        }
    }

    private UI_SkillSlot FirstEmptyCombatSlot()
    {
        foreach (var s in skillSlots)
            if (s != null && s.slotCategory == UISkillCategory.Combat && !s.HasSkill) return s;
        return null;
    }

    private UI_SkillSlot FindSlotById(UISkillSlotId id)
    {
        foreach (var s in skillSlots)
            if (s != null && s.slotId == id) return s;
        return null;
    }

    private UI_SkillSlot FindSlotByType(SkillType type)
    {
        foreach (var s in skillSlots)
            if (s != null && s.HasSkill && s.Data.skillType == type) return s;
        return null;
    }

    /// <summary>Assign a specific skill to a combat slot (replaces anything there).</summary>
    public void AssignSkillToSlot(Skill_DataSO data, UISkillSlotId id)
    {
        if (data == null || data.category != SkillCategory.Combat) return;

        // keep only one slot per SkillType
        var dupe = FindSlotByType(data.skillType);
        if (dupe != null && dupe.slotId != id)
            dupe.ClearSlot();

        var slot = FindSlotById(id);
        if (slot == null || slot.slotCategory != UISkillCategory.Combat) return;

        slot.SetupSkillSlot(data);
        slot.RefreshText(player != null ? player.skillManager : null);
        slot.UpdateAffordability(player != null ? player.mana : null);
    }

    public void ClearSlot(UISkillSlotId id)
    {
        var slot = FindSlotById(id);
        if (slot != null) slot.ClearSlot();
    }

    public void ClearAllSlots()
    {
        foreach (var s in skillSlots)
            if (s != null) s.ClearSlot();
    }

    /// <summary>
    /// Only fills empty combat slots with defaults and only if the skill is unlocked.
    /// </summary>
    public void ApplyDefaultAssignmentsIfEmpty()
    {
        if (!applyDefaultAssignmentsOnStart || defaultAssignments == null) return;
        if (player == null) player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player == null || player.skillManager == null) return;

        foreach (var def in defaultAssignments)
        {
            if (def.skill == null || def.skill.category != SkillCategory.Combat) continue;

            var runtime = player.skillManager.GetSkillByType(def.skill.skillType);
            if (runtime == null || !runtime.IsUnlocked()) continue;

            var slot = FindSlotById(def.slotId);
            if (slot == null || slot.slotCategory != UISkillCategory.Combat) continue;

            if (!slot.HasSkill)
                AssignSkillToSlot(def.skill, def.slotId);
        }
    }

    /// <summary>Compatibility with saver/UI: populates combat bar from unlocked tree skills.</summary>
    public void RefreshSkillSlotsFromTree(UI_SkillTree tree)
    {
        if (tree == null) return;

        ClearAllSlots();

        var nodes = tree.GetComponentsInChildren<UI_TreeNode>(true);
        if (nodes == null || nodes.Length == 0) return;

        foreach (var n in nodes)
        {
            if (n == null || !n.isUnlocked || n.skillData == null) continue;
            if (n.skillData.category != SkillCategory.Combat) continue; // ignore Sex

            // Already placed this type?
            if (FindSlotByType(n.skillData.skillType) != null) continue;

            // Find first empty combat slot
            UI_SkillSlot empty = null;
            foreach (var s in skillSlots)
            {
                if (s != null && s.slotCategory == UISkillCategory.Combat && !s.HasSkill)
                {
                    empty = s; break;
                }
            }
            if (empty == null) break;

            AssignSkillToSlot(n.skillData, empty.slotId);
        }

        RefreshAllSkillCostsAndAfford();
    }

    // ===== Helpers =====

    private void RefreshAllSkillCostsAndAfford()
    {
        var sm = player != null ? player.skillManager : null;
        var mana = player != null ? player.mana : null;

        foreach (var s in skillSlots)
        {
            if (s == null) continue;
            if (!s.HasSkill) { s.RefreshText(sm); s.UpdateAffordability(mana); continue; }

            // Only combat bar – but harmless if sex slot sneaks in
            s.RefreshText(sm);
            s.UpdateAffordability(mana);
        }
    }

    public void ForceRefreshFromCurrentState()
    {
        if (player == null)
            HookPlayer(FindFirstObjectByType<Player>(FindObjectsInactive.Include));
        else
            HookPlayer(player);

        UpdateQuickSlots();
        UpdateGoldDisplay(playerInventory != null ? playerInventory.gold : 0);
        UpdateExpBar();
        UpdateSexExpBar();
        UpdateHealthBar();
        UpdateManaBar();
        RefreshAllSkillCostsAndAfford();
    }

    // UI_InGame.cs  (add inside the class, e.g. near other helpers)
    public UI_SkillSlot GetSkillSlot(SkillType type)
    {
        // Combat bar only: looks up by skill type among the combat slots
        return FindSlotByType(type);
    }

    // ============== ITEM PICKUP TOASTS ==============

    public void ShowItemPickup(Sprite icon, string itemName, int amount = 1)
    {
        if (toastPrefab == null || pickupToastParent == null) return;

        if (_toastQueue.Count >= maxQueue) _toastQueue.Dequeue();

        _toastQueue.Enqueue(new ToastRequest { icon = icon, name = itemName, amount = amount });

        if (!_isPlayingQueue) StartCoroutine(ProcessToastQueue());
    }

    private IEnumerator ProcessToastQueue()
    {
        _isPlayingQueue = true;

        float lifetime = GetToastLifetime();

        while (_toastQueue.Count > 0)
        {
            var req = _toastQueue.Dequeue();

            var toast = Instantiate(toastPrefab, pickupToastParent);
            toast.gameObject.SetActive(true);
            toast.Setup(req.icon, req.name, req.amount);

            if (itemPickupClip != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(itemPickupClip, Camera.main.transform.position, itemPickupVolume);

            yield return new WaitForSecondsRealtime(lifetime + lifetimePadding);
        }

        _isPlayingQueue = false;
    }

    private float GetToastLifetime()
    {
        if (toastLifetimeOverride > 0f) return toastLifetimeOverride;
        if (_cachedToastLifetime > 0f) return _cachedToastLifetime;

        const float fallback = 1.75f;
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
        catch { return fallback; }
    }
}
