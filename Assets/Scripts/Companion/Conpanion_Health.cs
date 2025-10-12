using UnityEngine;

[DisallowMultipleComponent]
public class Companion_Health : Entity_Health
{
    [Header("Companion Health")]
    [SerializeField] private bool hideWorldspaceBar = true;
    [SerializeField] private bool startFull = true;

    protected override void Awake()
    {
        base.Awake();

        if (startFull) ForceReviveToFull();
        if (hideWorldspaceBar) EnableHealthBar(false);

        // Can't call OnHealthUpdate here (event is in base type).
        // Nudge the base to raise it by re-setting the current value:
        SetCurrentHealth(GetCurrentHealth());
    }

    public void Heal(float amount) => IncreaseHealth(amount);
    public void Damage(float amount) => ReduceHealth(amount);
}
