using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Collections;

public class CompanionHUDSlot : MonoBehaviour
{
    private GameObject _root;

    [Header("UI")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    [Header("MP")]
    [SerializeField] private Slider mpSlider;
    [SerializeField] private TMP_Text mpText;

    [Header("XP")]
    [SerializeField] private Slider xpSlider;     // normalized 0..1
    [SerializeField] private TMP_Text xpText;     // "cur/next" (optional)

    [Header("Fallback")]
    [SerializeField] private float pollSeconds = 0.25f;

    private GameObject target;
    private Component health;
    private Component mana;
    private Component stats;    // <-- needed for XP

    // cached reflection
    private MethodInfo m_GetCurHP, m_GetMaxHP, m_GetHPpct;
    private MethodInfo m_GetCurMP, m_GetMaxMP, m_GetMPpct;
    private MethodInfo m_GetCurXP, m_GetNextXP, m_GetXPpct;

    private EventInfo e_OnHP, e_OnMP, e_OnXP;

    private Delegate hpDel, mpDel, xpDel;
    private Coroutine pollCo;

    private void Awake()
    {
        _root = gameObject;
        SetRootActive(false); // hidden until something binds
    }

    private void SetRootActive(bool on)
    {
        if (_root && _root.activeSelf != on) _root.SetActive(on);
    }

    public void Bind(GameObject companion, Sprite portrait, string displayName)
    {
        Unbind();
        SetRootActive(true);

        target = companion;
        if (portraitImage) portraitImage.sprite = portrait;
        if (nameText) nameText.text = string.IsNullOrEmpty(displayName) ? companion.name : displayName;

        health = FindByNames(companion, "Entity_Health", "Health", "Player_Health", "HP", "PlayerHealth");
        mana = FindByNames(companion, "Entity_Mana", "Mana", "Player_Mana", "MP", "PlayerMana");

        // 🔧 ADD THIS:
        stats = FindByNames(companion, "Companion_Stats", "Player_Stats", "Entity_Stats");

        CacheMethodsAndEvents();
        TrySubscribe();

        RefreshHP();
        RefreshMP();
        RefreshXP();
        pollCo = StartCoroutine(PollLoop());
    }


    public void Unbind()
    {
        if (pollCo != null) { StopCoroutine(pollCo); pollCo = null; }
        TryUnsubscribe();

        target = null;
        health = null;
        mana = null;
        stats = null;

        if (portraitImage) portraitImage.sprite = null;
        if (nameText) nameText.text = string.Empty;
        if (hpSlider) hpSlider.value = 0f;
        if (mpSlider) mpSlider.value = 0f;
        if (xpSlider) xpSlider.value = 0f;
        if (hpText) hpText.text = string.Empty;
        if (mpText) mpText.text = string.Empty;
        if (xpText) xpText.text = string.Empty;

        SetRootActive(false);
    }

    // ---------- internals ----------

    private IEnumerator PollLoop()
    {
        var wait = new WaitForSecondsRealtime(pollSeconds);
        while (target)
        {
            RefreshHP();
            RefreshMP();
            RefreshXP();
            yield return wait;
        }
    }

    private void RefreshHP()
    {
        if (!health) { SetHP(0, 1); return; }

        float cur = CallF(m_GetCurHP, health);
        float max = CallF(m_GetMaxHP, health);
        float pct = SafePct(cur, max, CallF(m_GetHPpct, health));

        if (hpSlider) hpSlider.value = pct;
        if (hpText) hpText.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(max)}";
    }

    private void RefreshMP()
    {
        if (!mana) { SetMP(0, 1); return; }

        float cur = CallF(m_GetCurMP, mana);
        float max = CallF(m_GetMaxMP, mana);
        float pct = SafePct(cur, max, CallF(m_GetMPpct, mana));

        if (mpSlider) mpSlider.value = pct;
        if (mpText) mpText.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(max)}";
    }

    private void RefreshXP()
    {
        if (!stats)
        {
            if (xpSlider) xpSlider.value = 0f;
            if (xpText) xpText.text = string.Empty;
            return;
        }

        float cur = CallF(m_GetCurXP, stats);
        float nxt = CallF(m_GetNextXP, stats);
        float pct = SafePct(cur, nxt, CallF(m_GetXPpct, stats));

        if (xpSlider) xpSlider.value = pct;

        if (xpText)
        {
            int ic = Mathf.Max(0, Mathf.RoundToInt(cur));
            int inx = Mathf.Max(1, Mathf.RoundToInt(nxt));
            xpText.text = $"{ic}/{inx}";
        }
    }

    private void SetHP(float cur, float max)
    {
        if (hpSlider) hpSlider.value = SafePct(cur, max, 0f);
        if (hpText) hpText.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(max)}";
    }

    private void SetMP(float cur, float max)
    {
        if (mpSlider) mpSlider.value = SafePct(cur, max, 0f);
        if (mpText) mpText.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(max)}";
    }

    // reflection helpers
    private static Component FindByNames(GameObject go, params string[] names)
    {
        foreach (var n in names)
        {
            var c = go.GetComponent(n);
            if (c) return c;
        }
        return null;
    }

    private void CacheMethodsAndEvents()
    {
        if (health)
        {
            var t = health.GetType();
            m_GetCurHP = t.GetMethod("GetCurrentHealth") ?? t.GetMethod("get_CurrentHealth");
            m_GetMaxHP = t.GetMethod("GetMaxHealth") ?? t.GetMethod("get_MaxHealth");
            m_GetHPpct = t.GetMethod("GetHealthPercent");
            e_OnHP = t.GetEvent("OnHealthUpdate") ?? t.GetEvent("OnHealthChanged");
        }

        if (mana)
        {
            var t = mana.GetType();
            m_GetCurMP = t.GetMethod("GetCurrentMana") ?? t.GetMethod("get_CurrentMana");
            m_GetMaxMP = t.GetMethod("GetMaxMana") ?? t.GetMethod("get_MaxMana");
            m_GetMPpct = t.GetMethod("GetManaPercent");
            e_OnMP = t.GetEvent("OnManaUpdate") ?? t.GetEvent("OnManaChanged");
        }

        if (stats)
        {
            var t = stats.GetType();

            // Current / Next / Percent (support property-getter names too)
            m_GetCurXP = t.GetMethod("get_CurrentEXP") ?? t.GetMethod("GetCurrentEXP");
            m_GetNextXP = t.GetMethod("GetNextLevelRequirement") ?? t.GetMethod("get_NextEXP");
            m_GetXPpct = t.GetMethod("GetExpPercent") ?? t.GetMethod("GetEXPPercent");

            // Common event names for EXP updates
            e_OnXP = t.GetEvent("OnExpChanged") ?? t.GetEvent("OnEXPChanged");
        }
    }

    private void TrySubscribe()
    {
        try
        {
            if (health && e_OnHP != null)
            {
                hpDel = Delegate.CreateDelegate(e_OnHP.EventHandlerType, this, nameof(OnHPEvent));
                e_OnHP.AddEventHandler(health, hpDel);
            }
            if (mana && e_OnMP != null)
            {
                mpDel = Delegate.CreateDelegate(e_OnMP.EventHandlerType, this, nameof(OnMPEvent));
                e_OnMP.AddEventHandler(mana, mpDel);
            }
            if (stats && e_OnXP != null)
            {
                // 🔧 Adapt to Action(), Action<T>, or Action<T1,T2>
                var handlerType = e_OnXP.EventHandlerType;
                var invoke = handlerType.GetMethod("Invoke");
                int pcount = invoke != null ? invoke.GetParameters().Length : 0;

                if (pcount == 0)
                    xpDel = Delegate.CreateDelegate(handlerType, this, nameof(OnXPEvent));
                else if (pcount == 1)
                    xpDel = Delegate.CreateDelegate(handlerType, this, nameof(OnXPEvent1));
                else
                    xpDel = Delegate.CreateDelegate(handlerType, this, nameof(OnXPEvent2));

                e_OnXP.AddEventHandler(stats, xpDel);
            }
        }
        catch
        {
            // fall back to polling only
        }
    }

    private void TryUnsubscribe()
    {
        try
        {
            if (health && e_OnHP != null && hpDel != null) e_OnHP.RemoveEventHandler(health, hpDel);
            if (mana && e_OnMP != null && mpDel != null) e_OnMP.RemoveEventHandler(mana, mpDel);
            if (stats && e_OnXP != null && xpDel != null) e_OnXP.RemoveEventHandler(stats, xpDel);
        }
        catch { }
        hpDel = mpDel = xpDel = null;
    }

    // compatible with () / (args…) events
    private void OnHPEvent() { RefreshHP(); }
    private void OnMPEvent() { RefreshMP(); }

    // XP event adapters (ignore any args; we just repaint)
    private void OnXPEvent() { RefreshXP(); }
    private void OnXPEvent1(object _) { RefreshXP(); }
    private void OnXPEvent2(object _, object __) { RefreshXP(); }

    private static float CallF(MethodInfo mi, object inst)
    {
        if (mi == null || inst == null) return -1f;
        try
        {
            var v = mi.Invoke(inst, null);
            if (v is float f) return f;
            if (v is int i) return i;
            if (v is double d) return (float)d;
        }
        catch { }
        return -1f;
    }

    private static float SafePct(float cur, float max, float direct)
    {
        if (direct >= 0f) return Mathf.Clamp01(direct);
        if (max <= 0.0001f) return 0f;
        return Mathf.Clamp01(cur / max);
    }
}
