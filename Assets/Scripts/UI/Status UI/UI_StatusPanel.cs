using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rewired;

public class UI_StatusPanel : UI_Panel
{
    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    [SerializeField] private string nextCharacterAction = "UIRight";
    [SerializeField] private string previousCharacterAction = "UILeft";
    private Rewired.Player rPlayer;

    private float inputCooldown = 0f;
    private const float inputCooldownDuration = 0.2f;

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    [Header("Basic Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI classText;
    public Image portraitImage;

    [Header("Normal EXP")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI currentExpText;
    public TextMeshProUGUI nextLevelExpText;

    [Header("Sex EXP")]
    [SerializeField] private GameObject sexExpSectionRoot;
    public TextMeshProUGUI sexLevelText;
    public TextMeshProUGUI currentSexExpText;
    public TextMeshProUGUI nextSexExpText;

    [Header("Stats")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;
    public TextMeshProUGUI bioText;

    [Header("Stat Slots")]
    [SerializeField] private UI_StatSlot[] statSlots;

    [Header("Optional Portrait Resolver")]
    [SerializeField] private CompanionPortraitMap portraitMap;

    // Current viewed actor
    private GameObject currentCharacter;
    private Player currentPlayer;
    private Companion currentCompanion;

    private Entity_Stats currentStats;
    private Player_Stats currentPlayerStats;
    private Companion_Stats currentCompanionStats;

    private Entity_Health currentHealth;
    private Entity_Mana currentMana;

    // Party list
    private readonly List<GameObject> partyMembers = new List<GameObject>();
    private int currentIndex = 0;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
    }

    private void OnEnable()
    {
        isOpen = true;
        CompanionPartyManager.OnPartyChanged += HandlePartyChanged;
        CompanionPartyManager.OnRecruited += HandleCompanionRecruited;
        CompanionPartyManager.OnDismissed += HandleCompanionDismissed;
    }

    private void OnDisable()
    {
        isOpen = false;
        CompanionPartyManager.OnPartyChanged -= HandlePartyChanged;
        CompanionPartyManager.OnRecruited -= HandleCompanionRecruited;
        CompanionPartyManager.OnDismissed -= HandleCompanionDismissed;
        UnsubscribeFromStats();
    }

    private void OnDestroy()
    {
        CompanionPartyManager.OnPartyChanged -= HandlePartyChanged;
        CompanionPartyManager.OnRecruited -= HandleCompanionRecruited;
        CompanionPartyManager.OnDismissed -= HandleCompanionDismissed;
        UnsubscribeFromStats();
    }

    private void Update()
    {
        if (!isOpen || rPlayer == null) return;

        if (inputCooldown > 0f)
            inputCooldown -= Time.deltaTime;

        if (inputCooldown > 0f) return;

        if (rPlayer.GetButtonDown(cancelAction))
        {
            HandleCancel();
            inputCooldown = inputCooldownDuration;
            return;
        }

        if (rPlayer.GetButtonDown(nextCharacterAction))
        {
            ShowNextCharacter();
            inputCooldown = inputCooldownDuration;
            return;
        }

        if (rPlayer.GetButtonDown(previousCharacterAction))
        {
            ShowPreviousCharacter();
            inputCooldown = inputCooldownDuration;
            return;
        }
    }

    // --------------------------------------------------
    // Open / Close
    // --------------------------------------------------

    public void OpenPanel(Player startingPlayer)
    {
        if (startingPlayer == null) return;

        gameObject.SetActive(true);
        isOpen = true;

        BuildPartyList(startingPlayer.gameObject);
        ShowCurrentIndex();
    }

    public void OpenPanel(GameObject startingCharacter)
    {
        if (startingCharacter == null) return;

        gameObject.SetActive(true);
        isOpen = true;

        BuildPartyList(startingCharacter);
        ShowCurrentIndex();
    }

    public void ClosePanel()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }

    // --------------------------------------------------
    // Public button hooks
    // --------------------------------------------------

    public void OnNextCharacterButton()
    {
        ShowNextCharacter();
    }

    public void OnPreviousCharacterButton()
    {
        ShowPreviousCharacter();
    }

    // --------------------------------------------------
    // Party list
    // --------------------------------------------------

    private void BuildPartyList(GameObject preferredCharacter = null)
    {
        partyMembers.Clear();

        // Add main player first
        Player mainPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (mainPlayer != null && mainPlayer.gameObject != null)
            partyMembers.Add(mainPlayer.gameObject);

        // Add active companions
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

    private void ShowCurrentIndex()
    {
        if (partyMembers.Count == 0)
        {
            ClearDisplay();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, partyMembers.Count - 1);
        BindCharacter(partyMembers[currentIndex]);
        RefreshAll();
    }

    public void ShowNextCharacter()
    {
        if (partyMembers.Count == 0) return;

        currentIndex++;
        if (currentIndex >= partyMembers.Count)
            currentIndex = 0;

        ShowCurrentIndex();
    }

    public void ShowPreviousCharacter()
    {
        if (partyMembers.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = partyMembers.Count - 1;

        ShowCurrentIndex();
    }

    // --------------------------------------------------
    // Binding
    // --------------------------------------------------

    private void BindCharacter(GameObject character)
    {
        if (character == null) return;

        UnsubscribeFromStats();

        currentCharacter = character;
        currentPlayer = character.GetComponent<Player>();
        currentCompanion = character.GetComponent<Companion>();

        currentStats = character.GetComponent<Entity_Stats>();
        currentPlayerStats = character.GetComponent<Player_Stats>();
        currentCompanionStats = character.GetComponent<Companion_Stats>();

        currentHealth = character.GetComponent<Entity_Health>();
        currentMana = character.GetComponent<Entity_Mana>();

        SubscribeToStats();

        if (statSlots != null)
        {
            foreach (var slot in statSlots)
            {
                if (slot == null) continue;
                slot.Setup(currentStats);
            }
        }
    }

    private void SubscribeToStats()
    {
        if (currentPlayerStats != null)
        {
            currentPlayerStats.OnExpChanged += HandlePlayerExpChanged;
            currentPlayerStats.OnSexExpChanged += HandleSexExpChanged;
            currentPlayerStats.OnLevelChanged += HandleLevelChanged;
            currentPlayerStats.OnStatsChanged += HandleStatsChanged;
        }

        if (currentCompanionStats != null)
        {
            currentCompanionStats.OnExpChanged += HandleCompanionExpChanged;
            currentCompanionStats.OnLevelChanged += HandleLevelChanged;

            
        }
    }

    private void UnsubscribeFromStats()
    {
        if (currentPlayerStats != null)
        {
            currentPlayerStats.OnExpChanged -= HandlePlayerExpChanged;
            currentPlayerStats.OnSexExpChanged -= HandleSexExpChanged;
            currentPlayerStats.OnLevelChanged -= HandleLevelChanged;
            currentPlayerStats.OnStatsChanged -= HandleStatsChanged;
        }

        if (currentCompanionStats != null)
        {
            currentCompanionStats.OnExpChanged -= HandleCompanionExpChanged;
            currentCompanionStats.OnLevelChanged -= HandleLevelChanged;
        }

        currentCharacter = null;
        currentPlayer = null;
        currentCompanion = null;
        currentStats = null;
        currentPlayerStats = null;
        currentCompanionStats = null;
        currentHealth = null;
        currentMana = null;
    }

    // --------------------------------------------------
    // Refresh
    // --------------------------------------------------

    private void RefreshAll()
    {
        RefreshHeader();
        RefreshExpTexts();
        RefreshSexTexts();
        RefreshHPMP();
        RefreshAllStatSlots();
    }

    private void RefreshHeader()
    {
        if (currentCharacter == null) return;

        if (nameText != null)
            nameText.text = currentCharacter.name;

        if (portraitImage != null)
        {
            Sprite portrait = null;

            if (currentPlayer != null)
            {
                portrait = currentPlayer.Portrait;
            }
            else if (currentCompanion != null)
            {
                // 1) direct portrait on Companion
                portrait = currentCompanion.Portrait;

                // 2) fallback to portrait map by CompanionIdentity id
                if (portrait == null && portraitMap != null)
                {
                    var identity = currentCompanion.GetComponent<CompanionIdentity>();
                    if (identity != null && !string.IsNullOrWhiteSpace(identity.id))
                        portrait = portraitMap.Get(identity.id);
                }

                // 3) optional fallback to SpriteRenderer
                if (portrait == null)
                {
                    var sr = currentCompanion.GetComponentInChildren<SpriteRenderer>(true);
                    if (sr != null)
                        portrait = sr.sprite;
                }
            }

            portraitImage.sprite = portrait;
            portraitImage.enabled = (portrait != null);
        }

        if (classText != null)
        {
            if (currentPlayer != null)
                classText.text = currentPlayer.GetType().Name;
            else if (currentCompanion != null)
                classText.text = string.IsNullOrWhiteSpace(currentCompanion.companionClassName)
                    ? "Companion"
                    : currentCompanion.companionClassName;
            else
                classText.text = "";
        }

        if (levelText != null)
        {
            if (currentPlayerStats != null)
                levelText.text = "Lv " + currentPlayerStats.CurrentLevel;
            else if (currentCompanionStats != null)
                levelText.text = "Lv " + currentCompanionStats.CurrentLevel;
            else
                levelText.text = "Lv --";
        }

        if (bioText != null)
        {
            if (currentPlayer != null)
                bioText.text = currentPlayer.Bio;
            else if (currentCompanion != null)
                bioText.text = currentCompanion.Bio;
            else
                bioText.text = "";
        }
    }

    private void RefreshExpTexts()
    {
        if (currentExpText == null || nextLevelExpText == null) return;

        if (currentPlayerStats != null)
        {
            currentExpText.text = Mathf.FloorToInt(currentPlayerStats.CurrentEXP).ToString("N0");
            nextLevelExpText.text = Mathf.CeilToInt(currentPlayerStats.GetNextLevelRequirement()).ToString("N0");
        }
        else if (currentCompanionStats != null)
        {
            currentExpText.text = Mathf.FloorToInt(currentCompanionStats.CurrentEXP).ToString("N0");
            nextLevelExpText.text = Mathf.CeilToInt(currentCompanionStats.GetNextLevelRequirement()).ToString("N0");
        }
        else
        {
            currentExpText.text = "--";
            nextLevelExpText.text = "--";
        }
    }

    private void RefreshSexTexts()
    {
        bool showSexStats = currentPlayerStats != null;

        if (sexExpSectionRoot != null)
            sexExpSectionRoot.SetActive(showSexStats);

        if (!showSexStats) return;

        if (sexLevelText != null)
            sexLevelText.text = "Sex Lv " + currentPlayerStats.CurrentSexLevel;

        if (currentSexExpText != null)
            currentSexExpText.text = Mathf.FloorToInt(currentPlayerStats.CurrentSexEXP).ToString("N0");

        if (nextSexExpText != null)
            nextSexExpText.text = Mathf.CeilToInt(currentPlayerStats.GetNextSexLevelRequirement()).ToString("N0");
    }

    private void RefreshHPMP()
    {
        if (currentHealth == null || currentMana == null || currentStats == null)
        {
            if (hpText != null) hpText.text = "--";
            if (mpText != null) mpText.text = "--";
            return;
        }

        if (hpText != null)
            hpText.text = $"{currentHealth.GetCurrentHealth()} / {currentStats.GetMaxHealth()}";

        if (mpText != null)
            mpText.text = $"{currentMana.GetCurrentMana()} / {currentStats.GetMaxMana()}";
    }

    private void RefreshAllStatSlots()
    {
        if (statSlots == null) return;

        foreach (var slot in statSlots)
        {
            if (slot == null) continue;

            if (currentStats != null)
                slot.Setup(currentStats);
            else
                slot.Clear();
        }
    }

    public void RefreshCurrentCharacter()
    {
        if (!isOpen) return;

        if (currentCharacter == null)
        {
            ShowCurrentIndex();
            return;
        }

        // Rebind in case components were rebuilt/reloaded
        BindCharacter(currentCharacter);
        RefreshAll();
    }

    public void RefreshCharacter(GameObject character)
    {
        Debug.Log(
            $"[StatusPanel] RefreshCharacter called. " +
            $"character={(character != null ? character.name : "NULL")}, " +
            $"isOpen={isOpen}"
        );

        if (!isOpen || character == null)
        {
            Debug.Log("[StatusPanel] Early return: panel not open or character null.");
            return;
        }

        BuildPartyList(character);

        int found = partyMembers.IndexOf(character);
        Debug.Log($"[StatusPanel] Character index in party list = {found}, party count = {partyMembers.Count}");

        if (found >= 0)
            currentIndex = found;

        ShowCurrentIndex();
    }

    public void RefreshCharacter(Player player)
    {
        if (player == null) return;
        RefreshCharacter(player.gameObject);
    }

    private void ClearDisplay()
    {
        if (nameText != null) nameText.text = "";
        if (classText != null) classText.text = "";
        if (levelText != null) levelText.text = "Lv --";
        if (currentExpText != null) currentExpText.text = "--";
        if (nextLevelExpText != null) nextLevelExpText.text = "--";
        if (hpText != null) hpText.text = "--";
        if (mpText != null) mpText.text = "--";
        if (bioText != null) bioText.text = "";

        if (sexExpSectionRoot != null)
            sexExpSectionRoot.SetActive(false);

        if (statSlots != null)
        {
            foreach (var slot in statSlots)
            {
                if (slot == null) continue;
                slot.Clear();
            }
        }
    }

    // --------------------------------------------------
    // Stat event handlers
    // --------------------------------------------------

    private void HandlePlayerExpChanged(float cur, float next)
    {
        if (!isOpen || currentPlayerStats == null) return;

        if (currentExpText != null)
            currentExpText.text = Mathf.FloorToInt(cur).ToString("N0");

        if (nextLevelExpText != null)
            nextLevelExpText.text = Mathf.CeilToInt(next).ToString("N0");
    }

    private void HandleCompanionExpChanged(float cur, float next)
    {
        if (!isOpen || currentCompanionStats == null) return;

        if (currentExpText != null)
            currentExpText.text = Mathf.FloorToInt(cur).ToString("N0");

        if (nextLevelExpText != null)
            nextLevelExpText.text = Mathf.CeilToInt(next).ToString("N0");
    }

    private void HandleSexExpChanged(float cur, float next, int level)
    {
        if (!isOpen || currentPlayerStats == null) return;

        if (sexLevelText != null)
            sexLevelText.text = "Sex Lv " + level;

        if (currentSexExpText != null)
            currentSexExpText.text = Mathf.FloorToInt(cur).ToString("N0");

        if (nextSexExpText != null)
            nextSexExpText.text = Mathf.CeilToInt(next).ToString("N0");
    }

    private void HandleLevelChanged(int newLevel)
    {
        if (!isOpen) return;

        if (levelText != null)
            levelText.text = "Lv " + newLevel;

        RefreshHPMP();
        RefreshAllStatSlots();
    }

    private void HandleStatsChanged()
    {
        if (!isOpen) return;

        RefreshHPMP();
        RefreshAllStatSlots();
    }

    // --------------------------------------------------
    // Party change handlers
    // --------------------------------------------------

    private void HandlePartyChanged()
    {
        if (!isOpen) return;

        GameObject current = currentCharacter;
        BuildPartyList(current);

        if (partyMembers.Count == 0)
        {
            ClearDisplay();
            return;
        }

        if (current != null)
        {
            int found = partyMembers.IndexOf(current);
            if (found >= 0)
                currentIndex = found;
            else
                currentIndex = Mathf.Clamp(currentIndex, 0, partyMembers.Count - 1);
        }

        ShowCurrentIndex();
    }

    private void HandleCompanionRecruited(string id, GameObject recruitedObject)
    {
        if (!isOpen) return;
        HandlePartyChanged();
    }

    private void HandleCompanionDismissed(string id)
    {
        if (!isOpen) return;
        HandlePartyChanged();
    }

    // --------------------------------------------------
    // Cancel
    // --------------------------------------------------

    public override bool HandleCancel()
    {
        if (UI.Instance != null)
            UI.Instance.CloseStatusPanel();
        else
            ClosePanel();

        return true;
    }
}