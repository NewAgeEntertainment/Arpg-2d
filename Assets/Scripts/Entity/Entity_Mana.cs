// Assets/Scripts/Entity/Entity_Mana.cs
using System;
using UnityEngine;
using UnityEngine.UI;

public class Entity_Mana : MonoBehaviour
{
    [SerializeField] private Slider manaBar;
    public event Action OnManaUpdate;

    private Entity entity;
    private Entity_Stats entityStats;

    [Header("Runtime")]
    [SerializeField] protected float currentMana;
    [SerializeField] protected bool isDead;

    [Header("Flags")]
    [SerializeField] protected bool isManaDepleted;

    [Header("Mana regen")]
    [SerializeField] private float manaRegenInterval = 1f;
    [SerializeField] private bool canRegenerateMana = true;
    [SerializeField] private float regenPerTick = 0f; // optional flat regen; leave 0 to rely on stats if you have one

    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        entityStats = GetComponent<Entity_Stats>();

        // Initialize to full if not already set by a loader
        if (currentMana <= 0f)
            currentMana = GetMaxMana();

        OnManaUpdate += UpdateManaBar;
        UpdateManaBar();
        // Debug.Log("🧠 Mana script instance: " + gameObject.name);
    }

    private void OnEnable()
    {
        // paint once more (in case bar just got enabled)
        OnManaUpdate?.Invoke();

        // start regen loop if configured
        if (manaRegenInterval > 0f)
            InvokeRepeating(nameof(RegenerateMana), manaRegenInterval, manaRegenInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RegenerateMana));
    }

    // -------- Public API --------

    public virtual bool UseMana(float manaCost)
    {
        if (isDead || isManaDepleted) return false;

        if (currentMana >= manaCost)
        {
            currentMana -= manaCost;
            if (currentMana <= 0f) { currentMana = 0f; isManaDepleted = true; }
            OnManaUpdate?.Invoke();
            return true;
        }

        return false;
    }

    public void IncreaseMana(float amount)
    {
        if (isDead) return;
        float max = GetMaxMana();
        currentMana = Mathf.Clamp(currentMana + Mathf.Max(0f, amount), 0f, max);
        if (currentMana > 0f) isManaDepleted = false;
        OnManaUpdate?.Invoke();
    }

    public void SetCurrentMana(float value)
    {
        currentMana = Mathf.Clamp(value, 0, GetMaxMana());
        if (currentMana > 0f) isManaDepleted = false;
        OnManaUpdate?.Invoke();
    }

    public void ReduceMana(float amount)
    {
        currentMana = Mathf.Max(0f, currentMana - Mathf.Max(0f, amount));
        if (currentMana <= 0f) isManaDepleted = true;
        OnManaUpdate?.Invoke();
    }

    public float GetManaPercent() => GetMaxMana() > 0f ? currentMana / GetMaxMana() : 0f;

    public void SetManaToPercent(float percent)
    {
        currentMana = GetMaxMana() * Mathf.Clamp01(percent);
        if (currentMana > 0f) isManaDepleted = false;
        OnManaUpdate?.Invoke();
    }

    public float GetCurrentMana() => currentMana;

    public float GetMaxMana() => (entityStats != null) ? entityStats.GetMaxMana() : 0f;

    public bool CanAfford(float cost)
    {
        return !isDead && currentMana >= Mathf.Max(0f, cost);
    }


    public void EnableManaBar(bool enable)
    {
        if (manaBar == null) return;
        var root = manaBar.transform.parent ? manaBar.transform.parent.gameObject : null;
        if (root != null) root.SetActive(enable);
        if (enable) UpdateManaBar();
    }

    // -------- Internals --------

    private void RegenerateMana()
    {
        if (isDead || !canRegenerateMana) return;

        // Prefer a value from stats if you have one (e.g., stats.resources.manaRegen.GetValue())
        float tick = regenPerTick;
        if (tick <= 0f && entityStats != null && entityStats.resources != null && entityStats.resources.manaRegen != null)
            tick = entityStats.resources.manaRegen.GetValue();

        if (tick > 0f)
            IncreaseMana(tick);
    }

    private void UpdateManaBar()
    {
        if (manaBar == null) return;
        var parent = manaBar.transform.parent ? manaBar.transform.parent.gameObject : null;
        if (parent != null && !parent.activeInHierarchy) return;

        float max = GetMaxMana();
        manaBar.value = (max > 0f) ? currentMana / max : 0f;
    }


}
