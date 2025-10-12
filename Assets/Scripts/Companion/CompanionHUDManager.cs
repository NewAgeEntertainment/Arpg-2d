using System.Collections.Generic;
using UnityEngine;

public class CompanionHUDManager : MonoBehaviour
{
    [Header("Slots left → right (or top → bottom)")]
    [SerializeField] private CompanionHUDSlot[] slots;

    [Header("Assets")]
    [SerializeField] private CompanionPortraitMap portraitMap;

    [Header("Behavior")]
    [SerializeField] private bool hideUnusedSlots = true;

    private void OnEnable()
    {
        // Subscribe to STATIC events via type name
        CompanionPartyManager.OnPartyChanged -= HandlePartyChanged;
        CompanionPartyManager.OnRecruited -= HandleRecruited;
        CompanionPartyManager.OnDismissed -= HandleDismissed;

        CompanionPartyManager.OnPartyChanged += HandlePartyChanged;
        CompanionPartyManager.OnRecruited += HandleRecruited;
        CompanionPartyManager.OnDismissed += HandleDismissed;

        RebuildFromParty();
    }

    private void OnDisable()
    {
        CompanionPartyManager.OnPartyChanged -= HandlePartyChanged;
        CompanionPartyManager.OnRecruited -= HandleRecruited;
        CompanionPartyManager.OnDismissed -= HandleDismissed;
    }

    private void HandlePartyChanged() => RebuildFromParty();
    private void HandleRecruited(string id, GameObject go) => RebuildFromParty();
    private void HandleDismissed(string id) => RebuildFromParty();

    public void RebuildFromParty()
    {
        if (slots == null || slots.Length == 0)
            return;

        // Clear all first
        for (int i = 0; i < slots.Length; i++)
            if (slots[i]) slots[i].Unbind();

        var pm = CompanionPartyManager.Instance;
        if (pm == null) { HideUnused(0); return; }

        var ids = new List<string>(pm.ActiveIds());
        ids.Sort(); // simple stable order; swap to your party order if you have one

        int bound = 0;
        foreach (var id in ids)
        {
            if (bound >= slots.Length) break;

            var go = pm.FindActiveInstance(id);
            if (go == null || !go.activeInHierarchy) continue;

            if (IsPlayer(go)) continue; // keep these slots for companions only

            var sprite = portraitMap ? portraitMap.Get(id) : null;
            var label = string.IsNullOrEmpty(id) ? (go ? go.name : "Companion") : id;

            slots[bound].Bind(go, sprite, label);
            bound++;
        }

        HideUnused(bound);
    }

    private void HideUnused(int fromIndex)
    {
        if (!hideUnusedSlots || slots == null) return;
        for (int i = fromIndex; i < slots.Length; i++)
            if (slots[i]) slots[i].Unbind();
    }

    private static bool IsPlayer(GameObject go)
    {
        if (!go) return false;
        return go.CompareTag("Player") || go.GetComponent<Player>() != null;
    }
}
