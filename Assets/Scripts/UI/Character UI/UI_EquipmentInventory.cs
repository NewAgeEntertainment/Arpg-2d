using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Rewired;
using TMPro;

public class UI_EquipmentInventory : UI_Panel
{
    [Header("Fallback References")]
    [SerializeField] private Inventory_Equipment equipmentInventory;
    [SerializeField] private Inventory_Player playerInventory;

    [Header("Unequipped Slots")]
    [SerializeField] private Transform equipmentSlotPanel;
    [SerializeField] private UI_EquipmentSlot slotPrefab;

    [Header("Equipped Slots Parent")]
    [SerializeField] private UI_EquipSlotParent equippedSlotsPanel;

    [Header("UI Toggle Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Dedicated Equipment ToolTip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;
    [SerializeField] private UI_PlayerStats playerStatsPanel;
    [SerializeField] private Button removeButton;

    [Header("Character Header")]
    [SerializeField] private Image characterPortraitImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private CompanionPortraitMap portraitMap;

    [Header("Audio")]
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    [SerializeField] private string nextCharacterAction = "UIRight";
    [SerializeField] private string previousCharacterAction = "UILeft";

    private Rewired.Player rPlayer;
    private readonly List<UI_EquipmentSlot> uiSlots = new List<UI_EquipmentSlot>();

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    public UI_EquipSlotParent EquippedSlotsPanel => equippedSlotsPanel;

    private GameObject currentCharacter;
    private Player currentPlayer;
    private Companion currentCompanion;
    private CharacterEquipmentProfile currentEquipmentProfile;

    private readonly List<GameObject> partyMembers = new List<GameObject>();
    private int currentIndex = 0;


    private enum PanelState { None, EquippedPanel, ItemList }
    private PanelState currentState = PanelState.None;

    private EquipmentSlotType? currentTargetSlot = null;
    private ItemType? currentFilter = null;
    private bool subscribed = false;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);

        if (equipmentInventory == null) equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory_Player>();

        if (equipmentSlotPanel != null)
            uiSlots.AddRange(equipmentSlotPanel.GetComponentsInChildren<UI_EquipmentSlot>(true));

        foreach (var slot in uiSlots)
        {
            slot.SetEquipmentToolTip(equipmentToolTip);
            slot.SetSelectable(true);
        }

        Close();
    }

    private void Update()
    {
        if (!isOpen || rPlayer == null)
            return;

        if (rPlayer.GetButtonDown(nextCharacterAction))
        {
            ShowNextCharacter();
            return;
        }

        if (rPlayer.GetButtonDown(previousCharacterAction))
        {
            ShowPreviousCharacter();
            return;
        }

        //if (rPlayer.GetButtonDown(cancelAction))
        //{
        //    HandleCancel();
        //}
    }

    private void OnEnable()
    {
        HookEvents();
        ForceRefresh();
    }

    private void OnDisable() => UnhookEvents();
    private void OnDestroy() => UnhookEvents();

    private void HookEvents()
    {
        if (subscribed) return;

        if (equipmentInventory != null)
            equipmentInventory.OnInventoryChange += UpdateUI;

        if (playerInventory != null)
            playerInventory.OnInventoryChange += UpdateUI;

        subscribed = true;
    }

    private void UnhookEvents()
    {
        if (!subscribed) return;

        if (equipmentInventory != null)
            equipmentInventory.OnInventoryChange -= UpdateUI;

        if (playerInventory != null)
            playerInventory.OnInventoryChange -= UpdateUI;

        subscribed = false;
    }



    public void Open()
    {
        isOpen = true;
        panelRoot?.SetActive(true);
        ShowEquippedPanel();
        UpdateUI();
    }

    public void Close()
    {
        isOpen = false;
        panelRoot?.SetActive(false);
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
        currentState = PanelState.None;
    }

    public void RefreshPanels()
    {
        UpdateEquippedSlots();
        UpdateUnequippedItemList();
    }

    public void ForceRefresh()
    {
        if (isOpen) UpdateUI();
        else
        {
            UpdateEquippedSlots();
            UpdateUnequippedItemList();
        }
    }

    public void OpenForCharacter(GameObject character)
    {
        BuildPartyList(character);

        int found = partyMembers.IndexOf(character);
        currentIndex = found >= 0 ? found : 0;

        ShowCurrentCharacter();
        Open();
    }

    private void BuildPartyList(GameObject preferredCharacter = null)
    {
        partyMembers.Clear();

        Player mainPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (mainPlayer != null)
            partyMembers.Add(mainPlayer.gameObject);

        if (CompanionPartyManager.Instance != null)
        {
            foreach (string id in CompanionPartyManager.Instance.ActiveIds())
            {
                GameObject go = CompanionPartyManager.Instance.FindActiveInstance(id);
                if (go != null && !partyMembers.Contains(go))
                    partyMembers.Add(go);
            }
        }

        currentIndex = 0;

        if (preferredCharacter != null)
        {
            int found = partyMembers.IndexOf(preferredCharacter);
            if (found >= 0)
                currentIndex = found;
        }
    }

    private void ShowCurrentCharacter()
    {
        if (partyMembers.Count == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, partyMembers.Count - 1);

        currentCharacter = partyMembers[currentIndex];
        currentPlayer = currentCharacter != null ? currentCharacter.GetComponent<Player>() : null;
        currentCompanion = currentCharacter != null ? currentCharacter.GetComponent<Companion>() : null;
        currentEquipmentProfile = currentCharacter != null ? currentCharacter.GetComponent<CharacterEquipmentProfile>() : null;

        if (equipmentToolTip != null)
            equipmentToolTip.SetCurrentCharacter(currentCharacter);

        RefreshCharacterHeader();
        RebindFallbackReferencesForCurrentCharacter();
        UpdateUI();
    }

    private void RebindFallbackReferencesForCurrentCharacter()
    {
        if (currentPlayer != null)
        {
            if (playerInventory == null)
                playerInventory = currentPlayer.GetComponent<Inventory_Player>();

            if (equipmentInventory == null && playerInventory != null)
                equipmentInventory = playerInventory.equipmentInventory;
        }
    }

    public void ShowNextCharacter()
    {
        if (partyMembers.Count == 0) return;

        currentIndex++;
        if (currentIndex >= partyMembers.Count)
            currentIndex = 0;

        ShowCurrentCharacter();
    }

    public void ShowPreviousCharacter()
    {
        if (partyMembers.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = partyMembers.Count - 1;

        ShowCurrentCharacter();
    }

    public void OnNextCharacterButton()
    {
        ShowNextCharacter();
    }

    public void OnPreviousCharacterButton()
    {
        ShowPreviousCharacter();
    }

    private void RefreshCharacterHeader()
    {
        if (characterNameText != null)
        {
            if (currentPlayer != null)
                characterNameText.text = currentPlayer.name;
            else if (currentCompanion != null)
                characterNameText.text = currentCompanion.name;
            else
                characterNameText.text = "";
        }

        if (characterPortraitImage != null)
        {
            Sprite portrait = null;

            if (currentPlayer != null)
            {
                portrait = currentPlayer.Portrait;
            }
            else if (currentCompanion != null)
            {
                portrait = currentCompanion.Portrait;

                if (portrait == null && portraitMap != null)
                {
                    var identity = currentCompanion.GetComponent<CompanionIdentity>();
                    if (identity != null && !string.IsNullOrWhiteSpace(identity.id))
                        portrait = portraitMap.Get(identity.id);
                }

                if (portrait == null)
                {
                    var sr = currentCompanion.GetComponentInChildren<SpriteRenderer>(true);
                    if (sr != null)
                        portrait = sr.sprite;
                }
            }

            characterPortraitImage.sprite = portrait;
            characterPortraitImage.enabled = (portrait != null);
        }
    }

    public void UpdateUI()
    {
        if (!isOpen) return;
        UpdateUnequippedItemList();
        UpdateEquippedSlots();
    }

    private Inventory_Equipment GetCurrentEquipmentInventory()
    {
        if (currentEquipmentProfile != null && currentEquipmentProfile.EquipmentInventory != null)
            return currentEquipmentProfile.EquipmentInventory;

        return equipmentInventory;
    }

    private List<Inventory_Equipped> GetCurrentEquipList()
    {
        if (currentEquipmentProfile != null && currentEquipmentProfile.EquipList != null)
            return currentEquipmentProfile.EquipList;

        if (playerInventory != null)
            return playerInventory.equipList;

        return null;
    }

    private void UpdateUnequippedItemList()
    {
        Inventory_Equipment activeInventory = GetCurrentEquipmentInventory();
        if (activeInventory == null || equipmentSlotPanel == null) return;

        var items = activeInventory.itemList;
        List<Inventory_Item> filteredItems = new List<Inventory_Item>();

        foreach (var item in items)
        {
            if (item == null || item.itemData == null)
                continue;

            if (currentFilter == null || item.itemData.itemType == currentFilter)
                filteredItems.Add(item);
        }

        if (slotPrefab != null && uiSlots.Count < filteredItems.Count)
        {
            int toCreate = filteredItems.Count - uiSlots.Count;
            for (int i = 0; i < toCreate; i++)
            {
                var slot = Instantiate(slotPrefab, equipmentSlotPanel);
                slot.SetEquipmentToolTip(equipmentToolTip);
                slot.SetSelectable(true);
                uiSlots.Add(slot);
            }
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < filteredItems.Count)
                uiSlots[i].UpdateSlot(filteredItems[i]);
            else
                uiSlots[i].Clear();
        }
    }

    private void UpdateEquippedSlots()
    {
        var activeEquipList = GetCurrentEquipList();
        if (equippedSlotsPanel == null || activeEquipList == null) return;

        equippedSlotsPanel.UpdateEquipmentSlots(activeEquipList);
    }

    public void ShowEquipmentInventoryPanel(EquipmentSlotType targetSlot, ItemType filterType)
    {
        currentTargetSlot = targetSlot;
        currentFilter = filterType;

        equippedSlotsPanel?.gameObject.SetActive(false);
        equipmentSlotPanel?.gameObject.SetActive(true);
        removeButton?.gameObject.SetActive(true);

        currentState = PanelState.ItemList;
        UpdateUI();
    }

    public void RemoveCurrentlyEquipped()
    {
        if (currentTargetSlot == null)
            return;

        if (currentEquipmentProfile != null)
        {
            currentEquipmentProfile.UnequipItemBySlot(currentTargetSlot.Value);
        }

        ShowEquippedPanel();
        UpdateUI();
    }

    public void SwapEquippedItem(Inventory_Item newItem)
    {
        if (newItem == null || currentTargetSlot == null)
            return;

        if (currentEquipmentProfile != null)
        {
            currentEquipmentProfile.TryEquipFromEquipmentInventory(newItem, currentTargetSlot.Value);
        }
        else if (playerInventory != null)
        {
            playerInventory.TryEquipFromEquipmentInventory(newItem);
        }

        ShowEquippedPanel();
        UpdateUI();
    }



    private void ShowEquippedPanel()
    {
        equipmentSlotPanel?.gameObject.SetActive(false);
        equippedSlotsPanel?.gameObject.SetActive(true);
        removeButton?.gameObject.SetActive(false);
        ResetAllHighlights();
        currentState = PanelState.EquippedPanel;
    }

    public void ResetAllHighlights()
    {
        if (equippedSlotsPanel != null)
        {
            foreach (var equipSlot in equippedSlotsPanel.GetComponentsInChildren<UI_EquippedSlot>(true))
                equipSlot.ResetHighlight();
        }

        foreach (var slot in uiSlots)
            slot.ResetHighlight();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private bool IsOnItemListPanel()
    {
        return equipmentSlotPanel != null && equipmentSlotPanel.gameObject.activeSelf
               && (equippedSlotsPanel == null || !equippedSlotsPanel.gameObject.activeSelf);
    }

    public override bool HandleCancel()
    {
        Debug.Log("[UI_EquipmentInventory] HandleCancel() called. Current state: " + currentState);

        // If we're in the unequipped item list, go back to the equipped slots panel first
        if (currentState == PanelState.ItemList || IsOnItemListPanel())
        {
            Debug.Log("[UI_EquipmentInventory] Backing out from item list to equipped slots panel");
            ShowEquippedPanel();
            return true;
        }

        // If we're already on the equipped slots panel, close Equipment and return to main menu
        Debug.Log("[UI_EquipmentInventory] Backing out from equipped slots panel to main menu");

        if (UI.Instance != null)
            UI.Instance.CloseEquipment();
        else
            Close();

        return true;
    }

    public void GoToMainMenuPanel()
    {
        Close();
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }
}