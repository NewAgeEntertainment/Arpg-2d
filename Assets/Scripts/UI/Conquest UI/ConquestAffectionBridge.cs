using System.Collections;
using UnityEngine;

public class ConquestAffectionBridge : MonoBehaviour
{
    [Header("How much affection to apply per call")]
    [SerializeField] private int addAmount = 1;
    [SerializeField] private int subtractAmount = 0;

    [Header("Optional explicit target")]
    [SerializeField] private CharacterProfileSO explicitProfile;       // if set, use this
    [SerializeField] private CharacterProfileRef explicitProfileRef;   // else try this
    [SerializeField] private Entity_Stats explicitStats;               // or derive from stats

    private SexyTimeLogic boundTo;

    private void OnEnable()
    {
        // optional: auto-bind to the active SexyTimeLogic to handle events
        StartCoroutine(BindRoutine());
    }

    private IEnumerator BindRoutine()
    {
        while (boundTo == null)
        {
            if (SexyTimeLogic.Current != null)
            {
                boundTo = SexyTimeLogic.Current;
                boundTo.OnPlayerBarFull.AddListener(OnAnySexEvent_Add);     // BLUE first
                boundTo.OnPartnerBarFull.AddListener(OnAnySexEvent_Add);    // PINK first
            }
            yield return null;
        }
    }

    private void OnDisable()
    {
        if (boundTo != null)
        {
            boundTo.OnPlayerBarFull.RemoveListener(OnAnySexEvent_Add);
            boundTo.OnPartnerBarFull.RemoveListener(OnAnySexEvent_Add);
            boundTo = null;
        }
    }

    // ---------- Public API you can call from Dialogue/Buttons ----------

    public void AddForCurrent(int amount) => ApplyDelta(ResolveProfile(), Mathf.Abs(amount));
    public void SubtractForCurrent(int amount) => ApplyDelta(ResolveProfile(), -Mathf.Abs(amount));

    public void AddForName(string profileName, int amount)
    {
        var p = ConquestRosterManager.Instance?.FindProfileByName(profileName);
        ApplyDelta(p, Mathf.Abs(amount));
    }

    public void SubtractForName(string profileName, int amount)
    {
        var p = ConquestRosterManager.Instance?.FindProfileByName(profileName);
        ApplyDelta(p, -Mathf.Abs(amount));
    }

    // Callbacks wired in the coroutine above (you can also wire them in the Inspector)
    public void OnAnySexEvent_Add()
    {
        // Default behavior: just add addAmount
        ApplyDelta(ResolveProfile(), Mathf.Abs(addAmount));
    }

    public void OnAnySexEvent_Subtract()
    {
        ApplyDelta(ResolveProfile(), -Mathf.Abs(subtractAmount));
    }

    // ---------- Internals ----------

    private void ApplyDelta(CharacterProfileSO profile, int delta)
    {
        if (profile == null || delta == 0) return;
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null) return;

        mgr.AddAffection(profile, delta); // negative deltas are fine
        // UI_Conquest will update via OnAffectionChanged subscription.
        Debug.Log($"[ConquestAffectionBridge] Affection delta {delta:+#;-#;0} -> {profile.name}");
    }

    private CharacterProfileSO ResolveProfile()
    {
        // 1) explicit asset
        if (explicitProfile != null) return explicitProfile;

        // 2) explicit ref
        if (explicitProfileRef != null && explicitProfileRef.profile != null)
            return explicitProfileRef.profile;

        // 3) from explicit stats
        if (explicitStats != null)
        {
            var r = explicitStats.GetComponent<CharacterProfileRef>()
                 ?? explicitStats.GetComponentInParent<CharacterProfileRef>(true)
                 ?? explicitStats.GetComponentInChildren<CharacterProfileRef>(true);
            if (r != null && r.profile != null) return r.profile;
        }

        // 4) current mini-game partner
        var st = SexyTimeLogic.Current;
        if (st != null && st.partnerStats != null)
        {
            var r = st.partnerStats.GetComponent<CharacterProfileRef>()
                 ?? st.partnerStats.GetComponentInParent<CharacterProfileRef>(true)
                 ?? st.partnerStats.GetComponentInChildren<CharacterProfileRef>(true);
            if (r != null && r.profile != null) return r.profile;
        }

        return null;
    }
}
