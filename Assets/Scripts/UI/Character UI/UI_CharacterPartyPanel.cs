using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterPartyPanel : MonoBehaviour
{
    [Header("Card holders under CharacterPartyPanel (left -> right)")]
    [SerializeField] private GameObject[] cardHolders = new GameObject[3];

    [Header("Binders (same order as holders)")]
    [SerializeField] private UI_CompanionHPMPBinder[] cards = new UI_CompanionHPMPBinder[3];

    [Header("Optional portrait resolver")]
    [SerializeField] private CompanionPortraitMap portraitMap; // optional

    [Header("Layout")]
    [SerializeField] private HorizontalLayoutGroup layout;
    [SerializeField] private TextAnchor alignWhenSingle = TextAnchor.UpperCenter;
    [SerializeField] private TextAnchor alignWhenMulti = TextAnchor.UpperLeft;
    [SerializeField] private float spacingSingle = 0f;
    [SerializeField] private float spacingMulti = 12f;

    private void OnEnable()
    {
        CompanionPartyManager.OnRecruited += HandleRecruited;
        CompanionPartyManager.OnDismissed += HandleDismissed;
        CompanionPartyManager.OnPartyChanged += HandlePartyChanged;
        CompanionPartyManager.OnPlayerResolved += HandlePlayerResolved;

        RefreshNow();
    }

    private void OnDisable()
    {
        CompanionPartyManager.OnRecruited -= HandleRecruited;
        CompanionPartyManager.OnDismissed -= HandleDismissed;
        CompanionPartyManager.OnPartyChanged -= HandlePartyChanged;
        CompanionPartyManager.OnPlayerResolved -= HandlePlayerResolved;

        UnbindAll();
        SetVisibleCardCount(0);
    }

    private void HandleRecruited(string id, GameObject go) => RefreshNow();
    private void HandleDismissed(string id) => RefreshNow();
    private void HandlePartyChanged() => RefreshNow();
    private void HandlePlayerResolved(Transform player) => RefreshNow();

    public void RefreshNow()
    {
        if (cards == null || cards.Length == 0) return;

        var pm = CompanionPartyManager.Instance;

        // --- 1) Count companions (excluding player) ---
        int companionCount = 0;
        if (pm != null)
        {
            foreach (var id in pm.ActiveIds())
            {
                var go = pm.FindActiveInstance(id);
                if (!go || !go.activeInHierarchy) continue;
                if (go.CompareTag("Player")) continue;
                companionCount++;
            }
        }

        // --- 2) Show the correct number of holders ---
        int desiredCards = Mathf.Clamp(1 + companionCount, 1, cards.Length);
        SetVisibleCardCount(desiredCards);

        // --- 3) Bind Player to slot 0 ---
        var player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (cards[0] != null)
        {
            if (player != null)
            {
                var pGO = player.gameObject;
                cards[0].Bind(pGO, ResolvePortrait(pGO), "Player");
            }
            else
            {
                cards[0].Unbind();
            }
        }

        // --- 4) Bind companions to slots 1.. ---
        int outIndex = 1;
        if (pm != null)
        {
            foreach (var id in pm.ActiveIds())
            {
                if (outIndex >= desiredCards) break;

                var go = pm.FindActiveInstance(id);
                if (!go || !go.activeInHierarchy) continue;
                if (go.CompareTag("Player")) continue;

                cards[outIndex]?.Bind(go, ResolvePortrait(go), go.name);
                outIndex++;
            }
        }

        // --- 5) Clear leftover *visible* slots ---
        for (int i = outIndex; i < desiredCards; i++)
            cards[i]?.Unbind();
    }

    private void SetVisibleCardCount(int count)
    {
        for (int i = 0; i < cardHolders.Length; i++)
        {
            bool show = i < count;

            if (cardHolders[i] != null && cardHolders[i].activeSelf != show)
                cardHolders[i].SetActive(show);

            if (!show && i < cards.Length && cards[i] != null)
                cards[i].Unbind();
        }

        ApplyCardLayout(count);

        if (layout != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout.transform as RectTransform);
    }

    private void ApplyCardLayout(int visibleCardCount)
    {
        if (layout == null) return;

        bool single = visibleCardCount <= 1;
        layout.childAlignment = single ? alignWhenSingle : alignWhenMulti;
        layout.spacing = single ? spacingSingle : spacingMulti;
    }

    private void UnbindAll()
    {
        if (cards == null) return;
        foreach (var c in cards) if (c) c.Unbind();
    }

    private Sprite ResolvePortrait(GameObject go)
    {
        if (!go) return null;

        // a) Scriptable map
        var s = FromPortraitMap(go);
        if (s) return s;

        // b) CharacterProfileRef.profile.portrait (reflection)
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
                    if (piPortrait != null)
                    {
                        var spr = piPortrait.GetValue(profileObj) as Sprite;
                        if (spr) return spr;
                    }
                }
            }
            catch { }
        }

        // c) SpriteRenderer fallback
        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr && sr.sprite) return sr.sprite;

        return null;
    }

    private Sprite FromPortraitMap(GameObject go)
    {
        if (!portraitMap || !go) return null;

        string id = null;
        var ci = go.GetComponent<CompanionIdentity>();
        if (ci != null) id = ci.id;

        try
        {
            var getById = portraitMap.GetType().GetMethod("Get", new[] { typeof(string) });
            if (getById != null && !string.IsNullOrEmpty(id))
            {
                var spr = getById.Invoke(portraitMap, new object[] { id }) as Sprite;
                if (spr) return spr;
            }

            var getByGO = portraitMap.GetType().GetMethod("Get", new[] { typeof(GameObject) });
            if (getByGO != null)
            {
                var spr = getByGO.Invoke(portraitMap, new object[] { go }) as Sprite;
                if (spr) return spr;
            }
        }
        catch { }

        return null;
    }
}
