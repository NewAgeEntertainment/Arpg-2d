using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterPartyPanel : MonoBehaviour
{
    [Header("Card widgets under CharacterPartyPanel (left -> right)")]
    [SerializeField] private UI_CompanionHPMPBinder[] cards = new UI_CompanionHPMPBinder[3];

    [Header("Optional portrait resolver")]
    [SerializeField] private CompanionPortraitMap portraitMap; // if you have one

    private void Awake()
    {
        // allow wiring from inspector; nothing else needed here
    }

    private void OnEnable()
    {
        // SUBSCRIBE with matching signatures
        CompanionPartyManager.OnRecruited += HandleRecruited;     // (string id, GameObject go)
        CompanionPartyManager.OnDismissed += HandleDismissed;     // (string id)
        CompanionPartyManager.OnPartyChanged += HandlePartyChanged;  // ()
        CompanionPartyManager.OnPlayerResolved += HandlePlayerResolved;// (Transform)

        RefreshNow();
    }

    private void OnDisable()
    {
        // UNSUBSCRIBE with matching signatures
        CompanionPartyManager.OnRecruited -= HandleRecruited;
        CompanionPartyManager.OnDismissed -= HandleDismissed;
        CompanionPartyManager.OnPartyChanged -= HandlePartyChanged;
        CompanionPartyManager.OnPlayerResolved -= HandlePlayerResolved;

        // optional: clear to avoid stale UI when panel is hidden
        UnbindAll();
    }

    // -------- Event handlers (correct signatures) --------
    private void HandleRecruited(string id, GameObject go) => RefreshNow();
    private void HandleDismissed(string id) => RefreshNow();
    private void HandlePartyChanged() => RefreshNow();
    private void HandlePlayerResolved(Transform player) => RefreshNow();

    // -------- Public API --------
    public void RefreshNow()
    {
        if (cards == null || cards.Length == 0) return;

        // 1) Player in slot 0
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

        // 2) Companions into the remaining slots (skip player)
        var pm = CompanionPartyManager.Instance;
        int outIndex = 1;

        if (pm != null)
        {
            foreach (var id in pm.ActiveIds())
            {
                if (outIndex >= cards.Length) break;

                var go = pm.FindActiveInstance(id);
                if (!go || !go.activeInHierarchy) continue;
                if (go.CompareTag("Player")) continue;

                var portrait = ResolvePortrait(go);
                var display = go.name;
                cards[outIndex]?.Bind(go, portrait, display);
                outIndex++;
            }
        }

        // 3) Clear any leftover slots
        for (int i = outIndex; i < cards.Length; i++)
            cards[i]?.Unbind();
    }

    // -------- Internals --------
    private void UnbindAll()
    {
        if (cards == null) return;
        foreach (var c in cards) if (c) c.Unbind();
    }
    private Sprite ResolvePortrait(GameObject go)
    {
        if (!go) return null;

        // a) Scriptable map (works with either Get(string) or Get(GameObject) if present)
        var s = FromPortraitMap(go);
        if (s) return s;

        // b) CharacterProfileRef.profile.portrait (common setup)
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

        // c) Fallback: SpriteRenderer sprite (2D)
        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr && sr.sprite) return sr.sprite;

        return null;
    }


    private Sprite FromPortraitMap(GameObject go)
    {
        if (!portraitMap || !go) return null;

        // Try to find an id on the object (CompanionIdentity.id)
        string id = null;
        var ci = go.GetComponent<CompanionIdentity>();
        if (ci != null) id = ci.id;

        try
        {
            // Preferred: Get(string)
            var getById = portraitMap.GetType().GetMethod("Get", new[] { typeof(string) });
            if (getById != null && !string.IsNullOrEmpty(id))
            {
                var spr = getById.Invoke(portraitMap, new object[] { id }) as Sprite;
                if (spr) return spr;
            }

            // Fallback: Get(GameObject) if your map defines it
            var getByGO = portraitMap.GetType().GetMethod("Get", new[] { typeof(GameObject) });
            if (getByGO != null)
            {
                var spr = getByGO.Invoke(portraitMap, new object[] { go }) as Sprite;
                if (spr) return spr;
            }
        }
        catch { /* best-effort */ }

        return null;
    }




}
