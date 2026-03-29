using UnityEngine;

public abstract class ItemEffect_DataSO : ScriptableObject
{
    [TextArea] public string effectDescription;

    [Header("Use Gating")]
    [Tooltip("If true, block use when target HP is full.")]
    [SerializeField] private bool affectsHealth = false;

    [Tooltip("If true, block use when target MP is full.")]
    [SerializeField] private bool affectsMana = false;

    [Tooltip("Seconds to block re-use of this effect while it's 'active' (0 = no lockout).")]
    [Min(0f)][SerializeField] private float reuseLockSeconds = 0f;

    [Tooltip("Optional key used to group lockouts. Leave empty to use this asset's name.")]
    [SerializeField] private string effectKeyOverride = "";

    protected Component targetCharacter;

    public virtual bool AffectsHealth => affectsHealth;
    public virtual bool AffectsMana => affectsMana;
    public virtual float DurationSeconds => Mathf.Max(0f, reuseLockSeconds);
    public virtual string EffectKey => string.IsNullOrEmpty(effectKeyOverride) ? name : effectKeyOverride;

    public virtual bool CanBeUsed(Component target) => true;

    public virtual void ExecuteEffect(Component target)
    {
        targetCharacter = target;
    }

    public virtual void Subscribe(Component target) => targetCharacter = target;
    public virtual void Unsubscribe() => targetCharacter = null;

    public virtual bool RequiresTarget => true;
}