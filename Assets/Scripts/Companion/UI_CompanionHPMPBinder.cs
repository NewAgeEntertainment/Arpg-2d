using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CompanionHPMPBinder : MonoBehaviour
{
    [Header("Portrait + Name")]
    [SerializeField] private Image portraitImage;          // drag the portrait Image on the card
    [SerializeField] private TMP_Text nameText;            // optional display name

    [Header("HP")]
    [SerializeField] private Slider hpSlider;              // 0..1 expected
    [SerializeField] private TMP_Text hpText;              // "cur/max" (optional)

    [Header("MP")]
    [SerializeField] private Slider mpSlider;              // 0..1 expected
    [SerializeField] private TMP_Text mpText;              // "cur/max" (optional)

    [Header("Fallback")]
    [SerializeField] private float pollSeconds = 0.25f;    // polling if no events available

    private GameObject target;
    private Component health;
    private Component mana;

    // reflection cache
    private MethodInfo m_GetCurHP, m_GetMaxHP, m_GetHPpct;
    private MethodInfo m_GetCurMP, m_GetMaxMP, m_GetMPpct;
    private EventInfo e_OnHP, e_OnMP;
    private Delegate hpDel, mpDel;
    private Coroutine pollCo;

    private void OnDisable() => Unbind();

    // ---------- Public API ----------
    /// <summary>
    /// Bind this card to a character GameObject.
    /// Optionally override portrait/name for the card (useful for player slot).
    /// </summary>
    public void Bind(GameObject go, Sprite portraitOverride = null, string nameOverride = null)
    {
        Unbind();

        target = go;

        // Portrait & name
        ApplyPortraitAndName(go, portraitOverride, nameOverride);

        // Find health/mana-like components from your project
        health = FindByNames(go, "Entity_Health", "Health", "Player_Health", "HP", "PlayerHealth");
        mana = FindByNames(go, "Entity_Mana", "Mana", "Player_Mana", "MP", "PlayerMana");

        CacheMethodsAndEvents();
        TrySubscribe();

        RefreshHP();
        RefreshMP();

        // Start fallback polling
        if (pollCo != null) StopCoroutine(pollCo);
        pollCo = StartCoroutine(PollLoop());
    }

    /// <summary>Clear UI and detach from the current target.</summary>
    public void Unbind()
    {
        if (pollCo != null) { StopCoroutine(pollCo); pollCo = null; }
        TryUnsubscribe();

        target = null; health = null; mana = null;

        if (portraitImage) { portraitImage.sprite = null; portraitImage.enabled = false; }
        if (nameText) nameText.text = string.Empty;

        if (hpSlider) hpSlider.value = 0f;
        if (mpSlider) mpSlider.value = 0f;
        if (hpText) hpText.text = string.Empty;
        if (mpText) mpText.text = string.Empty;
    }

    // ---------- internals ----------
    private IEnumerator PollLoop()
    {
        var wait = new WaitForSecondsRealtime(pollSeconds);
        while (target)
        {
            RefreshHP();
            RefreshMP();
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
        }
        catch { /* fail-soft: polling still works */ }
    }

    private void TryUnsubscribe()
    {
        try
        {
            if (health && e_OnHP != null && hpDel != null) e_OnHP.RemoveEventHandler(health, hpDel);
            if (mana && e_OnMP != null && mpDel != null) e_OnMP.RemoveEventHandler(mana, mpDel);
        }
        catch { }
        hpDel = mpDel = null;
    }

    // compatible with () and (args…) events
    private void OnHPEvent() => RefreshHP();
    private void OnMPEvent() => RefreshMP();

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

    private void ApplyPortraitAndName(GameObject go, Sprite portraitOverride, string nameOverride)
    {
        // Name
        if (nameText)
            nameText.text = !string.IsNullOrEmpty(nameOverride) ? nameOverride : (go ? go.name : "");

        // Portrait
        Sprite chosen = portraitOverride;

        if (chosen == null && go != null)
        {
            // Try a CharacterProfileRef.profile.portrait if you use that in your project
            var profRef = go.GetComponent("CharacterProfileRef");
            if (profRef != null)
            {
                try
                {
                    var piProfile = profRef.GetType().GetProperty("profile");
                    var profileObj = piProfile != null ? piProfile.GetValue(profRef) : null;
                    if (profileObj != null)
                    {
                        var piPortrait = profileObj.GetType().GetProperty("portrait");
                        chosen = piPortrait != null ? (Sprite)piPortrait.GetValue(profileObj) : null;
                    }
                }
                catch { /* ignore */ }
            }

            // Fallback: sprite from a SpriteRenderer on the target (common for 2D)
            if (chosen == null)
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) chosen = sr.sprite;
            }
        }

        if (portraitImage)
        {
            portraitImage.sprite = chosen;
            portraitImage.enabled = (chosen != null);
            portraitImage.preserveAspect = true;
        }
    }
}
