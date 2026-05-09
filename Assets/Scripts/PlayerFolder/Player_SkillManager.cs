using UnityEngine;
using Rewired;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Thrust thrust { get; private set; }
    public SexSkill_DeepBreath deepBreath { get; private set; }
    public SexSkill_RapidStroke rapidStroke { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_Sword swordSpin { get; private set; }

    public Skill_AirPunch airPunch { get; private set; }

    [Header("Skill Data References")]
    [Tooltip("Skill_DataSO for Deep Breath (upgradeType must be DeepBreath).")]
    [SerializeField] private Skill_DataSO deepBreathData;

    [Header("Refs")]
    [SerializeField] private Player_Stats stats;

    [Header("Level Unlock Skills")]
    [SerializeField] private Skill_DataSO[] levelUnlockSkills;

    [Tooltip("If true, checks current level on Start and unlocks anything already earned.")]
    [SerializeField] private bool checkLevelUnlocksOnStart = true;

    [SerializeField] private bool verboseLevelUnlockLogs = true;

    [Header("Overrides (optional)")]
    [Tooltip("If set, this explicit reference will be used instead of auto-finding/creating.")]
    [SerializeField] private SexSkill_DeepBreath deepBreathOverride;

    [Header("Slot Input")]
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private bool useRewiredSlotInput = true;
    [SerializeField] private bool blockWhenGameplayInputDisabled = true;
    public int RewiredPlayerId => rewiredPlayerId;

    [SerializeField] private string keyboardSlotAAction = "KeyboardSkillSlotA";
    [SerializeField] private string keyboardSlotBAction = "KeyboardSkillSlotB";
    [SerializeField] private string keyboardSlotCAction = "KeyboardSkillSlotC";
    [SerializeField] private string keyboardSlotDAction = "KeyboardSkillSlotD";

    [SerializeField] private string controllerSlotAAction = "ControllerSkillSlotA";
    [SerializeField] private string controllerSlotBAction = "ControllerSkillSlotB";
    [SerializeField] private string controllerSlotCAction = "ControllerSkillSlotC";
    [SerializeField] private string controllerSlotDAction = "ControllerSkillSlotD";

    [SerializeField] private string skillModifierAction = "SkillModifier";

    [Header("Debug / Safety")]
    [Tooltip("Create 'Skill - Deep Breath' under the Player at runtime if missing.")]
    [SerializeField] private bool createDeepBreathIfMissingAtRuntime = true;

    [Tooltip("If true, will Unlock Deep Breath if data is missing (useful for sanity checks).")]
    [SerializeField] private bool autoUnlockDeepBreathIfDataMissing = false;

    [SerializeField] private bool verboseLogs = true;

    private bool _loggedDeepBreathInitOnce = false;
    public Skill_DataSO DeepBreathData => deepBreathData;

    private Rewired.Player rPlayer;
    private Player player;

    private void Awake()
    {
        dash = SafeFind(dash);
        thrust = SafeFind(thrust);
        shard = SafeFind(shard);
        swordSpin = SafeFind(swordSpin);
        deepBreath = ResolveDeepBreath();
        rapidStroke = SafeFind(rapidStroke);
        airPunch = SafeFind(airPunch);

        player = GetComponent<Player>();

        if (stats == null)
            stats = GetComponentInParent<Player_Stats>();

        TryCacheRewired();

        if (verboseLogs)
        {
            if (dash == null) Debug.LogWarning("[SkillManager] Skill_Dash missing");
            if (thrust == null) Debug.LogWarning("[SkillManager] Skill_Thrust missing");
            if (shard == null) Debug.LogWarning("[SkillManager] Skill_Shard missing");
            if (swordSpin == null) Debug.LogWarning("[SkillManager] Skill_Sword missing");
            if (deepBreath == null) Debug.LogWarning("[SkillManager] SexSkill_DeepBreath missing");
        }
    }

    private void Start()
    {
        InitializeDeepBreathFromData();

        SubscribeToLevelEvents();

        if (checkLevelUnlocksOnStart)
            CheckLevelBasedSkillUnlocks();
    }

    private void Update()
    {
        TryHandleAssignedSlotInput();
    }

    public Skill_Base GetSkillByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Thrust: return thrust;
            case SkillType.Dash: return dash;
            case SkillType.TimeShard: return shard;
            case SkillType.SwordSpin: return swordSpin;
            case SkillType.DeepBreath: return deepBreath;
            case SkillType.RapidStroke: return rapidStroke;
            case SkillType.AirPunch: return airPunch;
            default:
                Debug.LogError($"[SkillManager] Unknown skill type: {skillType}");
                return null;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromLevelEvents();
    }

    public float CalculateSkillDamage(Skill_DataSO skillData)
    {
        if (skillData == null || skillData.upgradeData == null || stats == null)
        {
            Debug.LogWarning("[SkillManager] Missing skill or stats for damage calc.");
            return 0f;
        }

        float basePower = skillData.upgradeData.damageScale.basePower;
        float scalingMultiplier = skillData.upgradeData.damageScale.scalingMultiplier;
        StatType scalingStat = skillData.upgradeData.damageScale.scalingStat;
        float statValue = stats.GetStatByType(scalingStat).GetValue();
        float levelBonus = 1f + (stats.CurrentLevel * 0.05f);

        return Mathf.Floor((basePower + statValue * scalingMultiplier) * levelBonus);
    }

    public bool EnsureDeepBreathReady(bool allowAutoUnlockIfDataMissing = false)
    {
        deepBreath = ResolveDeepBreath();

        if (deepBreath == null)
        {
            Debug.LogWarning("[SkillManager] EnsureDeepBreathReady: DeepBreath component not found on Player.");
            return false;
        }

        if (deepBreath.Unlocked(SkillUpgradeType.DeepBreath))
            return true;

        if (deepBreathData != null &&
            deepBreathData.upgradeData != null &&
            deepBreathData.upgradeData.upgradeType == SkillUpgradeType.DeepBreath)
        {
            deepBreath.SetSkillUpgrade(deepBreathData);
            if (deepBreath.Unlocked(SkillUpgradeType.DeepBreath))
            {
                if (verboseLogs) Debug.Log("[SkillManager] Deep Breath unlocked via data.");
                return true;
            }
        }

        if (allowAutoUnlockIfDataMissing || autoUnlockDeepBreathIfDataMissing)
        {
            deepBreath.Unlock();
            if (verboseLogs) Debug.Log("[SkillManager] Deep Breath hard-unlocked (no data present).");
            return true;
        }

        if (verboseLogs)
            Debug.LogWarning("[SkillManager] Deep Breath remains locked (no valid data and auto-unlock disabled).");

        return false;
    }

    private void SubscribeToLevelEvents()
    {
        if (stats == null)
            stats = GetComponentInParent<Player_Stats>();

        if (stats == null)
            return;

        stats.OnLevelChanged -= OnPlayerLevelChanged;
        stats.OnLevelChanged += OnPlayerLevelChanged;
    }

    private void UnsubscribeFromLevelEvents()
    {
        if (stats != null)
            stats.OnLevelChanged -= OnPlayerLevelChanged;
    }

    private void OnPlayerLevelChanged(int newLevel)
    {
        CheckLevelBasedSkillUnlocks();
    }

    private void CheckLevelBasedSkillUnlocks()
    {
        if (stats == null)
            stats = GetComponentInParent<Player_Stats>();

        if (stats == null)
            return;

        if (levelUnlockSkills == null || levelUnlockSkills.Length == 0)
            return;

        int currentLevel = stats.CurrentLevel;

        foreach (Skill_DataSO skillData in levelUnlockSkills)
        {
            TryUnlockSkillByLevel(skillData, currentLevel);
        }
    }

    private void TryUnlockSkillByLevel(Skill_DataSO skillData, int currentLevel)
    {
        if (skillData == null)
            return;

        if (skillData.upgradeData == null)
        {
            Debug.LogWarning($"[SkillManager] {skillData.name} has no upgradeData.");
            return;
        }

        if (skillData.unlockedByDefault)
            return;

        if (currentLevel < skillData.requiredLevel)
            return;

        Skill_Base runtimeSkill = GetSkillByType(skillData.skillType);

        if (runtimeSkill == null)
        {
            Debug.LogWarning($"[SkillManager] Cannot unlock {skillData.displayName}. Runtime skill missing for type: {skillData.skillType}");
            return;
        }

        SkillUpgradeType newUpgradeType = skillData.upgradeData.upgradeType;

        // Only skip if this exact upgrade is already active.
        // Do NOT skip just because the skill already has SwordSpin.
        if (runtimeSkill.Unlocked(newUpgradeType))
            return;

        runtimeSkill.SetSkillUpgrade(skillData);

        if (verboseLevelUnlockLogs)
            Debug.Log($"[SkillManager] Applied upgrade {newUpgradeType} from {skillData.displayName} at level {currentLevel}.");
    }

    public bool ApplySkillUpgrade(Skill_DataSO skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[SkillManager] ApplySkillUpgrade failed: skillData is null.");
            return false;
        }

        if (skillData.upgradeData == null)
        {
            Debug.LogWarning($"[SkillManager] ApplySkillUpgrade failed: {skillData.name} has no upgradeData.");
            return false;
        }

        Skill_Base runtimeSkill = GetSkillByType(skillData.skillType);

        if (runtimeSkill == null)
        {
            Debug.LogWarning($"[SkillManager] No runtime skill found for {skillData.skillType}.");
            return false;
        }

        SkillUpgradeType newUpgradeType = skillData.upgradeData.upgradeType;

        if (runtimeSkill.Unlocked(newUpgradeType))
        {
            Debug.Log($"[SkillManager] {newUpgradeType} is already applied.");
            return true;
        }

        runtimeSkill.SetSkillUpgrade(skillData);

        Debug.Log($"[SkillManager] Applied skill upgrade: {skillData.displayName} / {newUpgradeType}");

        return true;
    }

    public void EnsureInitialized()
    {
        dash = SafeFind(dash);
        thrust = SafeFind(thrust);
        shard = SafeFind(shard);
        swordSpin = SafeFind(swordSpin);
        deepBreath = ResolveDeepBreath();
        rapidStroke = SafeFind(rapidStroke);
        airPunch = SafeFind(airPunch);

        player = GetComponent<Player>();

        if (stats == null)
            stats = GetComponentInParent<Player_Stats>();

        TryCacheRewired();
    }

    private void TryHandleAssignedSlotInput()
    {
        // Allow sex skills during SexyTime even if normal player input is disabled.
        if (!SexyTimeLogic.isSexyTimeGoingOn &&
            blockWhenGameplayInputDisabled &&
            player != null &&
            !player.InputEnabled)
        {
            return;
        }

        TryUseAssignedSlot(UISkillSlotId.SlotA, keyboardSlotAAction, controllerSlotAAction);
        TryUseAssignedSlot(UISkillSlotId.SlotB, keyboardSlotBAction, controllerSlotBAction);
        TryUseAssignedSlot(UISkillSlotId.SlotC, keyboardSlotCAction, controllerSlotCAction);
        TryUseAssignedSlot(UISkillSlotId.SlotD, keyboardSlotDAction, controllerSlotDAction);
    }



    private void TryUseAssignedSlot(
    UISkillSlotId slotId,
    string keyboardActionName,
    string controllerActionName)
    {
        bool pressed = false;

        if (rPlayer == null)
            TryCacheRewired();

        if (rPlayer == null)
            return;

        bool keyboardPressed =
            !string.IsNullOrWhiteSpace(keyboardActionName) &&
            rPlayer.GetButtonDown(keyboardActionName);

        bool controllerPressed =
            !string.IsNullOrWhiteSpace(controllerActionName) &&
            rPlayer.GetButtonDown(controllerActionName);

        bool modifierHeld =
            !string.IsNullOrWhiteSpace(skillModifierAction) &&
            rPlayer.GetButton(skillModifierAction);

        if (SexyTimeLogic.isSexyTimeGoingOn)
        {
            //Debug.Log(
            //    $"[SkillManager] SexyTime input check. Slot={slotId}, " +
            //    $"KeyboardAction={keyboardActionName}, KeyboardPressed={keyboardPressed}, " +
            //    $"ControllerAction={controllerActionName}, ControllerPressed={controllerPressed}, " +
            //    $"ModifierHeld={modifierHeld}"
            //);
        }

        // Keyboard direct skill use
        if (keyboardPressed)
            pressed = true;

        // Controller modifier skill use
        if (!pressed && modifierHeld && controllerPressed)
            pressed = true;

        if (!pressed)
            return;

        UI_SkillSlot uiSlot = GetUISkillSlotById(slotId);

        if (uiSlot == null)
        {
            Debug.LogWarning($"[SkillManager] No UI slot found for {slotId}.");
            return;
        }

        Debug.Log($"[SkillManager] Found slot {slotId}. HasSkill={uiSlot.HasSkill}, Data={(uiSlot.Data != null ? uiSlot.Data.displayName : "NULL")}");

        if (!uiSlot.HasSkill || uiSlot.Data == null)
            return;

        Skill_Base runtimeSkill = GetSkillByType(uiSlot.Data.skillType);

        if (runtimeSkill == null)
        {
            Debug.LogWarning($"[SkillManager] Runtime skill missing for {uiSlot.Data.skillType}.");
            return;
        }

        Debug.Log($"[SkillManager] Trying to use skill: {uiSlot.Data.displayName}");

        switch (uiSlot.Data.skillType)
        {
            case SkillType.Thrust:
                if (thrust != null && thrust.CanUseSkillCheck(out _))
                    player.stateMachine.ChangeState(player.thrustState);
                break;

            default:
                runtimeSkill.TryUseSkill();
                break;
        }
    }

    private UI_SkillSlot GetUISkillSlotById(UISkillSlotId slotId)
    {
        // During SexyTime, ONLY use the SexyTime hotbar.
        if (SexyTimeLogic.isSexyTimeGoingOn)
        {
            SexyTimeUIController sexUI =
                FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);

            if (sexUI != null && sexUI.Hotbar != null && sexUI.Hotbar.Slots != null)
            {
                int index = SlotIdToIndex(slotId);

                if (index >= 0 && index < sexUI.Hotbar.Slots.Length)
                {
                    UI_SkillSlot sexSlot = sexUI.Hotbar.Slots[index];

                    Debug.Log(
                        $"[SkillManager] SexyTime slot lookup. " +
                        $"SlotId={slotId}, Index={index}, " +
                        $"Slot={(sexSlot != null ? sexSlot.name : "NULL")}, " +
                        $"HasSkill={(sexSlot != null && sexSlot.HasSkill)}, " +
                        $"Data={(sexSlot != null && sexSlot.Data != null ? sexSlot.Data.displayName : "NULL")}"
                    );

                    return sexSlot;
                }
            }

            Debug.LogWarning($"[SkillManager] SexyTime hotbar slot not found for {slotId}.");
            return null;
        }

        // Normal gameplay hotbar.
        if (player == null || player.ui == null || player.ui.inGameUI == null)
            return null;

        UI_SkillSlot[] normalSlots =
            player.ui.inGameUI.GetComponentsInChildren<UI_SkillSlot>(true);

        foreach (UI_SkillSlot slot in normalSlots)
        {
            if (slot != null && slot.slotId == slotId)
                return slot;
        }

        return null;
    }

    private int SlotIdToIndex(UISkillSlotId slotId)
    {
        switch (slotId)
        {
            case UISkillSlotId.SlotA: return 0;
            case UISkillSlotId.SlotB: return 1;
            case UISkillSlotId.SlotC: return 2;
            case UISkillSlotId.SlotD: return 3;
            default: return -1;
        }
    }



    private void TryCacheRewired()
    {
        try
        {
            rPlayer = ReInput.players.GetPlayer(rewiredPlayerId);
        }
        catch
        {
            rPlayer = null;
        }
    }

    

    private T SafeFind<T>(T existing) where T : Component
    {
        if (existing != null) return existing;
        var found = GetComponentInChildren<T>(true);
        if (found == null) found = GetComponentInParent<T>(true);
        return found;
    }

    private SexSkill_DeepBreath ResolveDeepBreath()
    {
        if (deepBreathOverride != null) return deepBreathOverride;

        var d = GetComponentInChildren<SexSkill_DeepBreath>(true);
        if (d != null) return d;

        d = GetComponentInParent<SexSkill_DeepBreath>(true);
        if (d != null) return d;

        d = FindFirstObjectByType<SexSkill_DeepBreath>(FindObjectsInactive.Include);
        if (d != null) return d;

        if (createDeepBreathIfMissingAtRuntime)
        {
            var go = new GameObject("Skill - Deep Breath");
            go.transform.SetParent(transform, false);
            d = go.AddComponent<SexSkill_DeepBreath>();
            if (verboseLogs) Debug.Log("[SkillManager] Created Deep Breath under Player at runtime.");
            return d;
        }

        return null;
    }

    public string GetKeyboardActionNameForSlot(UISkillSlotId slotId)
    {
        switch (slotId)
        {
            case UISkillSlotId.SlotA: return keyboardSlotAAction;
            case UISkillSlotId.SlotB: return keyboardSlotBAction;
            case UISkillSlotId.SlotC: return keyboardSlotCAction;
            case UISkillSlotId.SlotD: return keyboardSlotDAction;
            default: return string.Empty;
        }
    }

    public string GetControllerActionNameForSlot(UISkillSlotId slotId)
    {
        switch (slotId)
        {
            case UISkillSlotId.SlotA: return controllerSlotAAction;
            case UISkillSlotId.SlotB: return controllerSlotBAction;
            case UISkillSlotId.SlotC: return controllerSlotCAction;
            case UISkillSlotId.SlotD: return controllerSlotDAction;
            default: return string.Empty;
        }
    }

    public string SkillModifierActionName => skillModifierAction;


    private void InitializeDeepBreathFromData()
    {
        deepBreath = ResolveDeepBreath();

        if (deepBreath == null)
        {
            if (!_loggedDeepBreathInitOnce)
                Debug.LogWarning("[SkillManager] Deep Breath component is missing; cannot initialize.");
            _loggedDeepBreathInitOnce = true;
            return;
        }

        if (deepBreathData != null)
        {
            if (deepBreathData.upgradeData == null)
                Debug.LogWarning("[SkillManager] Deep Breath data assigned, but upgradeData is NULL.");
            else if (deepBreathData.upgradeData.upgradeType != SkillUpgradeType.DeepBreath)
                Debug.LogWarning($"[SkillManager] Deep Breath data has wrong upgradeType: {deepBreathData.upgradeData.upgradeType}.");
            else
            {
                deepBreath.SetSkillUpgrade(deepBreathData);
                if (verboseLogs && !_loggedDeepBreathInitOnce)
                    Debug.Log("[SkillManager] Deep Breath initialized via data on Start().");
            }
        }
        else if (verboseLogs && !_loggedDeepBreathInitOnce)
        {
            Debug.LogWarning("[SkillManager] Deep Breath data not assigned.");
        }

        _loggedDeepBreathInitOnce = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Skills/Create Deep Breath under Player (Editor)")]
    private void CM_CreateDeepBreathUnderPlayer()
    {
        var existing = GetComponentInChildren<SexSkill_DeepBreath>(true);
        if (existing != null)
        {
            UnityEditor.Selection.activeObject = existing.gameObject;
            Debug.Log("[SkillManager] Deep Breath already exists; selected it in hierarchy.");
            return;
        }

        var go = new GameObject("Skill - Deep Breath");
        go.transform.SetParent(transform, false);
        deepBreath = go.AddComponent<SexSkill_DeepBreath>();
        deepBreathOverride = deepBreath;
        UnityEditor.Selection.activeObject = go;
        Debug.Log("[SkillManager] Created Deep Breath under Player (Editor).");
    }
#endif
}