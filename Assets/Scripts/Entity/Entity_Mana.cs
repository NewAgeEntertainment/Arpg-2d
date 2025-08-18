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
    private Skill_Base skill;

    private bool miniManaBarActive;
    [SerializeField] protected float currentMana;
    [SerializeField] protected bool isDead;

    [SerializeField] protected float manaCost;
    [SerializeField] protected bool isManaDepleted;
    [SerializeField] protected bool useMana;

    [Header("Mana regen")]
    [SerializeField] private float manaRegenInterval = 1f;
    [SerializeField] private bool canRegenerateMana = true;

    protected virtual void Awake()
    {
        skill = GetComponent<Skill_Base>();
        entity = GetComponent<Entity>();
        entityStats = GetComponent<Entity_Stats>();
        currentMana = entityStats.GetMaxMana();
        OnManaUpdate += UpdateManaBar;

        UpdateManaBar();
        Debug.Log("🧠 Mana script instance: " + gameObject.name);
    }

    public virtual bool UseMana(float manaCost)
    {
        if (isDead || isManaDepleted) return false;

        if (currentMana >= manaCost)
        {
            currentMana -= manaCost;
            Debug.Log("🟣 Mana used: " + manaCost);
            OnManaUpdate?.Invoke();
            return true;
        }

        return false;
    }

    public void RestoreManaOnHit(float amount)
    {
        if (isDead) return;
        IncreaseMana(amount);
    }

    public void RestoreManaOnHitWithScaling(int level)
    {
        int recovery = Mathf.Min(2 + ((level / 10) * 2), 8);
        IncreaseMana(recovery);
        Debug.Log($"🔋 Recovered {recovery} MP on hit (Level {level})");
    }

    public void IncreaseMana(float manaRecoveredAmount)
    {
        if (isDead) return;

        float newMana = currentMana + manaRecoveredAmount;
        float maxMana = entityStats.GetMaxMana();
        currentMana = Mathf.Min(newMana, maxMana);
        OnManaUpdate?.Invoke();
    }

    public void SetCurrentMana(float value)
    {
        currentMana = Mathf.Clamp(value, 0, entityStats.GetMaxMana());
        OnManaUpdate?.Invoke();
    }

    public void ReduceMana(float manaCost)
    {
        currentMana = currentMana - manaCost;
        OnManaUpdate?.Invoke();
        if (currentMana < 0) return;
    }

    public float GetManaPercent() => currentMana / entityStats.GetMaxMana();

    public void SetManaToPercent(float percent)
    {
        currentMana = entityStats.GetMaxMana() * Mathf.Clamp01(percent);
        OnManaUpdate?.Invoke();
    }

    public float GetCurrentMana() => currentMana;

    private void UpdateManaBar()
    {
        if (manaBar == null || !manaBar.transform.parent.gameObject.activeSelf) return;
        manaBar.value = currentMana / entityStats.GetMaxMana();
    }

    public void EnableManaBar(bool enable) => manaBar?.transform.parent.gameObject.SetActive(enable);
}
