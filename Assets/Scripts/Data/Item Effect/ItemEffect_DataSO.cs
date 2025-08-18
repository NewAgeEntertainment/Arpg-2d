using UnityEngine;

public abstract class ItemEffect_DataSO : ScriptableObject
{
    [TextArea] public string effectDescription;

    // 🔒 Use-gating fields you can set per effect asset in the Inspector
    [Header("Use Gating")]
    [Tooltip("If true, block use when target HP is full.")]
    [SerializeField] private bool affectsHealth = false;

    [Tooltip("If true, block use when target MP is full.")]
    [SerializeField] private bool affectsMana = false;

    [Tooltip("Seconds to block re-use of this effect while it's 'active' (0 = no lockout).")]
    [Min(0f)][SerializeField] private float reuseLockSeconds = 0f;

    [Tooltip("Optional key used to group lockouts. Leave empty to use this asset's name.")]
    [SerializeField] private string effectKeyOverride = "";

    protected Player player;

    // ✅ Inventory will read these; override if you need dynamic behavior
    public virtual bool AffectsHealth => affectsHealth;
    public virtual bool AffectsMana => affectsMana;
    public virtual float DurationSeconds => Mathf.Max(0f, reuseLockSeconds);
    public virtual string EffectKey => string.IsNullOrEmpty(effectKeyOverride) ? name : effectKeyOverride;

    public virtual bool CanBeUsed(Player player) => true;

    public virtual void ExecuteEffect(Player target)
    {
        this.player = target;
    }

    public virtual void Subscribe(Player player) => this.player = player;
    public virtual void Unsubscribe() => player = null;

    // ✅ If your effect doesn’t need a target, override and return false.
    public virtual bool RequiresTarget => true;
}
