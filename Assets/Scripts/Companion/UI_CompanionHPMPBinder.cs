using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_CompanionHPMPBinder : MonoBehaviour, IPointerClickHandler
{
    [Header("Portrait + Name")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    [Header("MP")]
    [SerializeField] private Slider mpSlider;
    [SerializeField] private TMP_Text mpText;

    [Header("Fallback")]
    [SerializeField] private float pollSeconds = 0.25f;

    [Header("Optional")]
    [SerializeField] private Button button; // optional: if the card root also has a Button

    private GameObject target;
    private Component health;
    private Component mana;

    // reflection cache
    private MethodInfo m_GetCurHP, m_GetMaxHP, m_GetHPpct;
    private MethodInfo m_GetCurMP, m_GetMaxMP, m_GetMPpct;
    private EventInfo e_OnHP, e_OnMP;
    private Delegate hpDel, mpDel;
    private Coroutine pollCo;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        // These sliders are used like fill bars
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
        }

        if (mpSlider != null)
        {
            mpSlider.minValue = 0f;
            mpSlider.maxValue = 1f;
        }
    }

    private void OnDisable() => Unbind();

    // ---------- Public API ----------
    public void Bind(GameObject go, Sprite portraitOverride = null, string nameOverride = null)
    {
        Unbind();

        target = go;

        ApplyPortraitAndName(go, portraitOverride, nameOverride);

        health = FindByNames(go, "Entity_Health", "Health", "Player_Health", "HP", "PlayerHealth");
        mana = FindByNames(go, "Entity_Mana", "Mana", "Player_Mana", "MP", "PlayerMana");

        CacheMethodsAndEvents();
        TrySubscribe();

        RefreshHP();
        RefreshMP();

        if (pollCo != null) StopCoroutine(pollCo);
        pollCo = StartCoroutine(PollLoop());
    }

    public void Unbind()
    {
        if (pollCo != null)
        {
            StopCoroutine(pollCo);
            pollCo = null;
        }

        TryUnsubscribe();

        target = null;
        health = null;
        mana = null;

        m_GetCurHP = null;
        m_GetMaxHP = null;
        m_GetHPpct = null;
        m_GetCurMP = null;
        m_GetMaxMP = null;
        m_GetMPpct = null;
        e_OnHP = null;
        e_OnMP = null;

        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (nameText) nameText.text = string.Empty;

        if (hpSlider) hpSlider.value = 0f;
        if (mpSlider) mpSlider.value = 0f;
        if (hpText) hpText.text = string.Empty;
        if (mpText) mpText.text = string.Empty;
    }

    // ---------- Click handling ----------
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    private void HandleClick()
    {
        Debug.Log($"[Binder] HandleClick fired on {gameObject.name}. target={(target != null ? target.name : "NULL")}");

        if (target == null || UI.Instance == null)
            return;

        Debug.Log($"[Binder] StatusMode={UI.Instance.IsStatusSelectionModeActive()}, EquipmentMode={UI.Instance.IsEquipmentSelectionModeActive()}");

        if (UI.Instance.IsStatusSelectionModeActive())
        {
            Debug.Log($"[Binder] Routing to SelectStatusCharacter: {target.name}");
            UI.Instance.SelectStatusCharacter(target);
            return;
        }

        if (UI.Instance.IsEquipmentSelectionModeActive())
        {
            Debug.Log($"[Binder] Routing to SelectEquipmentCharacter: {target.name}");
            UI.Instance.SelectEquipmentCharacter(target);
            return;
        }

        Debug.Log("[Binder] No selection mode active.");
    }

    // ---------- Internals ----------
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
        if (!health)
        {
            SetHP(0, 1);
            return;
        }

        float cur = CallF(m_GetCurHP, health);
        float max = CallF(m_GetMaxHP, health);
        float pct = SafePct(cur, max, CallF(m_GetHPpct, health));

        if (hpSlider) hpSlider.value = pct;
        if (hpText) hpText.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(max)}";
    }

    private void RefreshMP()
    {
        if (!mana)
        {
            SetMP(0, 1);
            return;
        }

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
        if (go == null) return null;

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
        catch
        {
            // polling fallback still works
        }
    }

    private void TryUnsubscribe()
    {
        try
        {
            if (health && e_OnHP != null && hpDel != null)
                e_OnHP.RemoveEventHandler(health, hpDel);

            if (mana && e_OnMP != null && mpDel != null)
                e_OnMP.RemoveEventHandler(mana, mpDel);
        }
        catch { }

        hpDel = null;
        mpDel = null;
    }

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
        if (nameText)
            nameText.text = !string.IsNullOrEmpty(nameOverride) ? nameOverride : (go ? go.name : "");

        Sprite chosen = portraitOverride;

        if (chosen == null && go != null)
        {
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
                catch { }
            }

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