using System.Collections.Generic;
using UnityEngine;

public class UI_CharacterProfilePopup : MonoBehaviour
{
    [Header("Slots left → right (or top → bottom)")]
    [SerializeField] private UI_CharacterProfileButton[] profileButtons;

    [Header("Behavior")]
    [SerializeField] private bool hideUnusedSlots = true;
    [SerializeField] private bool closeWhenOutOfItem = true;

    private Inventory_Item itemToUse;
    private Inventory_Player inventory;

    public void Open(Inventory_Item item, Player player)
    {
        if (item == null || item.itemData == null)
        {
            Debug.LogWarning("[CharacterProfilePopup] Open: missing item.");
            return;
        }

        inventory = null;

        if (player != null && player.gameObject.scene.IsValid())
            inventory = player.GetComponent<Inventory_Player>();

        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);

        if (inventory == null)
        {
            Debug.LogWarning("[CharacterProfilePopup] Open: missing inventory.");
            return;
        }

        itemToUse = item;
        gameObject.SetActive(true);

        RebuildFromParty();
    }

    public void RebuildFromParty()
    {
        if (profileButtons == null || profileButtons.Length == 0)
            return;

        // Clear/hide all first
        for (int i = 0; i < profileButtons.Length; i++)
        {
            if (profileButtons[i] == null) continue;
            profileButtons[i].gameObject.SetActive(false);
        }

        List<Component> targets = GetPartyTargets();

        int bound = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (bound >= profileButtons.Length)
                break;

            Component target = targets[i];
            if (target == null)
                continue;

            UI_CharacterProfileButton button = profileButtons[bound];
            if (button == null)
                continue;

            button.gameObject.SetActive(true);
            button.linkedCharacter = target;
            button.Setup(target, null);
            button.SetUseItemContext(itemToUse, inventory, () =>
            {
                RefreshVisibleSlots();

                Player mainPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
                mainPlayer?.ui?.inGameUI?.ForceRefreshFromCurrentState();

                if (closeWhenOutOfItem && inventory != null && itemToUse != null && itemToUse.itemData != null)
                {
                    if (inventory.FindItem(itemToUse.itemData) == null)
                        Close();
                }
            });

            button.SetSelected(false);
            bound++;
        }

        HideUnused(bound);
    }

    private void HideUnused(int fromIndex)
    {
        if (!hideUnusedSlots || profileButtons == null)
            return;

        for (int i = fromIndex; i < profileButtons.Length; i++)
        {
            if (profileButtons[i] != null)
                profileButtons[i].gameObject.SetActive(false);
        }
    }

    private void RefreshVisibleSlots()
    {
        if (profileButtons == null) return;

        for (int i = 0; i < profileButtons.Length; i++)
        {
            if (profileButtons[i] == null || !profileButtons[i].gameObject.activeSelf)
                continue;

            profileButtons[i].RefreshBars();
        }
    }

    private List<Component> GetPartyTargets()
    {
        var targets = new List<Component>();

        // Main player first
        Player mainPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (mainPlayer != null)
            targets.Add(mainPlayer);

        var pm = CompanionPartyManager.Instance;
        if (pm != null)
        {
            var ids = new List<string>(pm.ActiveIds());
            ids.Sort();

            foreach (string id in ids)
            {
                GameObject go = pm.FindActiveInstance(id);
                if (go == null || !go.activeInHierarchy)
                    continue;

                Companion companion = go.GetComponent<Companion>();
                if (companion == null)
                    continue;

                if (targets.Contains(companion))
                    continue;

                targets.Add(companion);
            }
        }
        else
        {
            // Optional fallback if manager is missing
            GameObject[] taggedCompanions = GameObject.FindGameObjectsWithTag("Companion");
            foreach (GameObject go in taggedCompanions)
            {
                if (go == null || !go.activeInHierarchy)
                    continue;

                Companion companion = go.GetComponent<Companion>();
                if (companion == null || !companion.InParty)
                    continue;

                if (targets.Contains(companion))
                    continue;

                targets.Add(companion);
            }
        }

        return targets;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}