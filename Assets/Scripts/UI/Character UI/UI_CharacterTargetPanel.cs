using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterTargetPanel : MonoBehaviour
{
    [Header("Card holders under panel (left -> right)")]
    [SerializeField] private GameObject[] cardHolders;

    [Header("Target buttons (same order as holders)")]
    [SerializeField] private UI_CharacterProfileButton[] buttons;

    [Header("Optional portrait resolver")]
    [SerializeField] private CompanionPortraitMap portraitMap;

    [Header("Layout")]
    [SerializeField] private HorizontalLayoutGroup layout;
    [SerializeField] private TextAnchor alignWhenSingle = TextAnchor.UpperCenter;
    [SerializeField] private TextAnchor alignWhenMulti = TextAnchor.UpperLeft;
    [SerializeField] private float spacingSingle = 0f;
    [SerializeField] private float spacingMulti = 12f;

    [Header("Behavior")]
    [SerializeField] private bool hideUnusedSlots = true;

    private Inventory_Item currentItem;
    private Inventory_Player currentInventory;
    private Action<Component> onTargetPicked;
    private Action afterUse;

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

        ClearAll();
        SetVisibleCardCount(0);
    }

    private void HandleRecruited(string id, GameObject go) => RefreshNow();
    private void HandleDismissed(string id) => RefreshNow();
    private void HandlePartyChanged() => RefreshNow();
    private void HandlePlayerResolved(Transform player) => RefreshNow();

    public void Open(Inventory_Item item, Inventory_Player inventory, Action<Component> onPicked = null, Action afterUseCallback = null)
    {
        currentItem = item;
        currentInventory = inventory;
        onTargetPicked = onPicked;
        afterUse = afterUseCallback;

        gameObject.SetActive(true);
        RefreshNow();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void RefreshNow()
    {
        if (buttons == null || buttons.Length == 0)
            return;

        ClearAll();

        List<Component> targets = GetPartyTargets();
        int desiredCards = Mathf.Clamp(targets.Count, 0, buttons.Length);

        SetVisibleCardCount(desiredCards);

        for (int i = 0; i < desiredCards; i++)
        {
            Component target = targets[i];
            UI_CharacterProfileButton btn = buttons[i];

            if (btn == null || target == null)
                continue;

            Sprite portrait = ResolvePortrait(target.gameObject);

            btn.gameObject.SetActive(true);
            btn.linkedCharacter = target;
            btn.Setup(target, HandleTargetPicked);
            btn.SetPortrait(portrait);
            btn.SetUseItemContext(currentItem, currentInventory, () =>
            {
                RefreshVisibleButtons();
                afterUse?.Invoke();
            });

            btn.SetSelected(false);
        }

        for (int i = desiredCards; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].ClearPortrait();
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    private void HandleTargetPicked(Component target)
    {
        foreach (var btn in buttons)
        {
            if (btn == null || !btn.gameObject.activeSelf)
                continue;

            btn.SetSelected(btn.linkedCharacter == target);
        }

        onTargetPicked?.Invoke(target);
    }

    private List<Component> GetPartyTargets()
    {
        var targets = new List<Component>();

        Player player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null)
            targets.Add(player);

        var pm = CompanionPartyManager.Instance;
        if (pm != null)
        {
            var ids = new List<string>(pm.ActiveIds());
            ids.Sort();

            foreach (string id in ids)
            {
                GameObject go = pm.FindActiveInstance(id);
                if (!go || !go.activeInHierarchy) continue;
                if (IsPlayer(go)) continue;

                Companion companion = go.GetComponent<Companion>();
                if (companion == null) continue;
                if (targets.Contains(companion)) continue;

                targets.Add(companion);
            }
        }

        return targets;
    }

    private void SetVisibleCardCount(int count)
    {
        if (cardHolders != null && cardHolders.Length > 0)
        {
            for (int i = 0; i < cardHolders.Length; i++)
            {
                bool show = i < count;

                if (cardHolders[i] != null && cardHolders[i].activeSelf != show)
                    cardHolders[i].SetActive(show);

                if (!show && i < buttons.Length && buttons[i] != null)
                {
                    buttons[i].ClearPortrait();
                    buttons[i].gameObject.SetActive(false);
                }
            }
        }
        else if (hideUnusedSlots)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    bool show = i < count;
                    buttons[i].gameObject.SetActive(show);
                    if (!show) buttons[i].ClearPortrait();
                }
            }
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

    private void RefreshVisibleButtons()
    {
        if (buttons == null) return;

        foreach (var btn in buttons)
        {
            if (btn == null || !btn.gameObject.activeSelf)
                continue;

            btn.RefreshBars();
            btn.SetPortrait(ResolvePortrait(btn.linkedCharacter != null ? btn.linkedCharacter.gameObject : null));
        }
    }

    private void ClearAll()
    {
        if (buttons == null) return;

        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            btn.ClearPortrait();
            btn.gameObject.SetActive(false);
        }
    }

    private static bool IsPlayer(GameObject go)
    {
        if (!go) return false;
        return go.CompareTag("Player") || go.GetComponent<Player>() != null;
    }

    private Sprite ResolvePortrait(GameObject go)
    {
        if (!go) return null;

        // Player portrait is optional; companions come only from the portrait map.
        if (IsPlayer(go))
            return null;

        return FromPortraitMap(go);
    }

    private Sprite FromPortraitMap(GameObject go)
    {
        if (!go)
        {
            Debug.Log("[TargetPanel] FromPortraitMap: go is null");
            return null;
        }

        if (!portraitMap)
        {
            Debug.LogWarning("[TargetPanel] FromPortraitMap: portraitMap is not assigned on UI_CharacterTargetPanel");
            return null;
        }

        var ci = go.GetComponent<CompanionIdentity>();
        if (ci == null)
        {
            Debug.LogWarning($"[TargetPanel] FromPortraitMap: no CompanionIdentity on {go.name}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(ci.id))
        {
            Debug.LogWarning($"[TargetPanel] FromPortraitMap: CompanionIdentity.id is empty on {go.name}");
            return null;
        }

        Sprite spr = portraitMap.Get(ci.id);

        if (spr == null)
        {
            Debug.LogWarning($"[TargetPanel] FromPortraitMap: no portrait found for id '{ci.id}' on {go.name}");
            return null;
        }

        Debug.Log($"[TargetPanel] FromPortraitMap: found portrait for id '{ci.id}' on {go.name}");
        return spr;
    }
}