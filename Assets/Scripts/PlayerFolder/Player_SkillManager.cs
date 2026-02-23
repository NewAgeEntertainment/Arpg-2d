using UnityEngine;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Thrust thrust { get; private set; }
    public SexSkill_DeepBreath deepBreath { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_Sword swordSpin { get; private set; }

    [Header("Skill Data References")]
    [Tooltip("Skill_DataSO for Deep Breath (upgradeType must be DeepBreath).")]
    [SerializeField] private Skill_DataSO deepBreathData;

    [Header("Refs")]
    [SerializeField] private Player_Stats stats;

    [Header("Overrides (optional)")]
    [Tooltip("If set, this explicit reference will be used instead of auto-finding/creating.")]
    [SerializeField] private SexSkill_DeepBreath deepBreathOverride;

    [Header("Debug / Safety")]
    [Tooltip("Create 'Skill - Deep Breath' under the Player at runtime if missing.")]
    [SerializeField] private bool createDeepBreathIfMissingAtRuntime = true;

    [Tooltip("If true, will Unlock Deep Breath if data is missing (useful for sanity checks).")]
    [SerializeField] private bool autoUnlockDeepBreathIfDataMissing = false;

    [SerializeField] private bool verboseLogs = true;

    private bool _loggedDeepBreathInitOnce = false;
    public Skill_DataSO DeepBreathData => deepBreathData;

    private void Awake()
    {
        dash = SafeFind(dash);
        thrust = SafeFind(thrust);
        shard = SafeFind(shard);
        swordSpin = SafeFind(swordSpin);
        deepBreath = ResolveDeepBreath();

        if (stats == null)
            stats = GetComponentInParent<Player_Stats>();

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
    }

    // ---------- Public API ----------

    public Skill_Base GetSkillByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Thrust: return thrust;
            case SkillType.Dash: return dash;
            case SkillType.TimeShard: return shard;
            case SkillType.SwordSpin: return swordSpin;
            case SkillType.DeepBreath: return deepBreath;
            default:
                Debug.LogError($"[SkillManager] Unknown skill type: {skillType}");
                return null;
        }
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

    /// Ensures the Deep Breath component exists & is unlocked (if allowed).
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

    public void EnsureInitialized()
    {
        dash = SafeFind(dash);
        thrust = SafeFind(thrust);
        shard = SafeFind(shard);
        swordSpin = SafeFind(swordSpin);
        deepBreath = ResolveDeepBreath();

        if (stats == null)
            stats = GetComponentInParent<Player_Stats>();
    }

    // ---------- Private helpers ----------

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

        // 1) Prefer a child of the Player
        var d = GetComponentInChildren<SexSkill_DeepBreath>(true);
        if (d != null) return d;

        // 2) Parent (rare)
        d = GetComponentInParent<SexSkill_DeepBreath>(true);
        if (d != null) return d;

        // 3) Global fallback
        d = FindFirstObjectByType<SexSkill_DeepBreath>(FindObjectsInactive.Include);
        if (d != null) return d;

        // 4) Create at runtime if allowed
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
