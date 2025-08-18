using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SexyTimeStateMachine))]
public class SexyTimeLogic : MonoBehaviour
{
    // 🔑 Static reference to the currently active instance
    public static SexyTimeLogic Current { get; private set; }

    [SerializeField] private Player_SkillManager skillManager;

    [Header("Core")]
    [SerializeField] private SexyTimeUIController ui;
    [SerializeField] private SexyTimeInputRouter inputRouter;

    [Header("Config")]
    [SerializeField] private bool autoStart = false;
    [SerializeField] private float barDecayPerSecond = 0f;
    [SerializeField] private float arousalPerStroke = 3f;
    [SerializeField] private float strokeMultiplier = 1f;
    [SerializeField] private float playerBarValueDeplete = 10f;

    [Header("Entities")]
    public Entity_Stats playerStats;
    public Entity_Stats partnerStats;

    [Header("NPC Attack skill")]
    [SerializeField] private float pussySqueeze = 10f;
    [SerializeField] private float pussySqueezeCooldown = 5f;
    private float npcAttackBarFillTimestamp;
    private float originalPussySqueeze;
    private float originalPussySqueezeCooldown;

    // --- Add near your other serialized fields ---
    [Header("Skills / Debug")]
    [SerializeField] private bool autoUnlockDeepBreath = false;   // turn on in Inspector to sanity-check

    [Tooltip("Press this key to trigger Deep Breath directly (testing).")]
    [SerializeField] private KeyCode debugDeepBreathKey = KeyCode.B; // NEW

    [Header("Climax")]
    public float cumDuration = 5f;
    public bool cumReached = false;
    public float cumTimeElapsed;

    [Header("Sex EXP Rewards")]
    [SerializeField] private int blueBarExp = 50;
    [SerializeField] private int pinkBarExp = 100;

    [Header("Quest Events")]
    public UnityEvent OnPlayerBarFull = new UnityEvent();
    public UnityEvent OnPartnerBarFull = new UnityEvent();

    public static bool isSexyTimeGoingOn = false;
    public bool playerBarReachedOnce = false;
    public bool partnerBarReachedOnce = false;
    public bool isFucking { get; private set; }
    public bool shouldPause { get; set; }
    public float timeBetweenStrokes { get; set; } = 1f;
    public float deepBreatheCooldown { get; set; } = 5f;
    public float deepBreatheTimestamp { get; set; } = 0f;
    public float lastStrokeTimestamp { get; private set; } = 0f;

    public SexyTimeUIController UI => ui;
    public float ArousalPerStroke => arousalPerStroke;
    public float DeepBreathDepleteAmount => playerBarValueDeplete;

    private SexyTimeStateMachine stateMachine;
    public Animator anim { get; private set; }
    private bool isCoroutineRunning = false;
    private Player cachedPlayer;

    private enum FinishWinner { None, PlayerBlue, PartnerPink }
    private FinishWinner winner = FinishWinner.None;
    private bool expGranted = false;

    private void OnEnable()
    {
        Current = this; // 🔑 mark this as the active instance

        OnPlayerBarFull.RemoveListener(OnBlueWinsFirst);
        OnPartnerBarFull.RemoveListener(OnPinkWinsFirst);
        OnPlayerBarFull.AddListener(OnBlueWinsFirst);
        OnPartnerBarFull.AddListener(OnPinkWinsFirst);
    }

    private void OnDisable()
    {
        if (Current == this) Current = null; // 🔑 clear if disabled

        OnPlayerBarFull.RemoveListener(OnBlueWinsFirst);
        OnPartnerBarFull.RemoveListener(OnPinkWinsFirst);
    }

    private void Awake()
    {
        stateMachine = GetComponent<SexyTimeStateMachine>();
        anim = GetComponent<Animator>();

        if (inputRouter == null)
            inputRouter = GetComponent<SexyTimeInputRouter>();

        if (inputRouter != null)
            inputRouter.Init();

        if (skillManager == null)
            skillManager = FindFirstObjectByType<Player_SkillManager>(FindObjectsInactive.Include);

        ResolveSkillManager();
    }

    private void Start()
    {
        cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (autoStart)
            StartSexyTime();
    }

    private void Update()
    {
        if (shouldPause) return;

        if (!isSexyTimeGoingOn && inputRouter != null && inputRouter.StartPressed())
            StartSexyTime();

        // Debug key to test Deep Breath quickly (optional)
        if (debugDeepBreathKey != KeyCode.None && Input.GetKeyDown(debugDeepBreathKey))
            CastDeepBreathe();

        if (!isSexyTimeGoingOn) return;

        HandleInput();
        UpdateNPCAttack();
        UpdateUI();
        HandleClimaxTimer();
        DepletePlayerBar();
    }

    public void StartSexyTime()
    {
        if (ui == null)
        {
            ui = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogError("[SexyTimeLogic] UI Controller not assigned!");
                return;
            }
        }

        // Player/skills may be spawned now - resolve again
        ResolveSkillManager();

        // Make sure Deep Breath exists & is unlocked (auto-create/unlock via manager if needed)
        skillManager?.EnsureDeepBreathReady(true);

        // ✅ Allow immediate use of Deep Breath on a fresh start
        deepBreatheTimestamp = Time.time - deepBreatheCooldown;

        // (Optional: your debug toggle still works if you kept it)
        Current = this;

        inputRouter?.EnableSexyTimeMaps();

        winner = FinishWinner.None;
        expGranted = false;
        isSexyTimeGoingOn = true;
        gameObject.SetActive(true);

        float playerMax = playerStats != null ? playerStats.sex.maxArousal.GetValue() : 100f;
        float partnerMax = partnerStats != null ? partnerStats.sex.maxArousal.GetValue() : 100f;
        ui.InitBars(playerMax, partnerMax);
        ui.UpdatePower(arousalPerStroke, arousalPerStroke);
        ui.Show();

        stateMachine.logic = this;
        stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));

        if (!isCoroutineRunning)
        {
            if (partnerStats != null)
            {
                bool isCrit;
                float partnerSexualDamage = partnerStats.GetSexualDamage(out isCrit);
                pussySqueeze = Mathf.Clamp(partnerSexualDamage * 0.25f, 2f, 15f);
            }

            originalPussySqueeze = pussySqueeze;
            originalPussySqueezeCooldown = pussySqueezeCooldown;

            ResetNPCAttack();
            isCoroutineRunning = true;
        }
    }


    private bool ResolveSkillManager()
    {
        if (skillManager != null) return true;

        // 1) Try the cached player first
        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (cachedPlayer != null)
            skillManager = cachedPlayer.GetComponent<Player_SkillManager>();

        // 2) Try GameManager's player reference if you have one
        if (skillManager == null)
        {
            var gmPlayer = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (gmPlayer != null)
                skillManager = gmPlayer.GetComponent<Player_SkillManager>();
        }

        // 3) Fallback: search scene (include inactive, covers prefabs activated later)
        if (skillManager == null)
            skillManager = FindFirstObjectByType<Player_SkillManager>(FindObjectsInactive.Include);

        if (skillManager == null)
        {
            Debug.LogWarning("[SexyTimeLogic] Could not resolve Player_SkillManager. Deep Breath will be unavailable.");
            return false;
        }

        return true;
    }

    public void ResetSexyTime()
    {
        GrantSexExpIfNeeded();

        inputRouter?.DisableSexyTimeMaps();

        isSexyTimeGoingOn = false;
        isCoroutineRunning = false;
        playerBarReachedOnce = false;
        partnerBarReachedOnce = false;

        ResetNPCAttack();
        pussySqueeze = originalPussySqueeze;
        pussySqueezeCooldown = originalPussySqueezeCooldown;

        cumReached = false;
        cumTimeElapsed = 0f;
        isFucking = false;

        if (anim != null)
            anim.Play("idle", 0, 0f);

        ui.Hide();

        gameObject.SetActive(false);
    }

    private void HandleInput()
    {
        if (inputRouter == null) return;

        if (inputRouter.StrokePressed())
            stateMachine.Stroke();

        if (inputRouter.DeepBreathePressed())
            CastDeepBreathe();

        if (inputRouter.PausePressed())
            stateMachine.PauseForDialogue();
    }

    private void UpdateNPCAttack()
    {
        if (Time.time < npcAttackBarFillTimestamp || shouldPause) return;

        float reduction = playerStats != null ? playerStats.GetResilienceReductionMultiplier() : 1f;

        float newPlayerValue = Mathf.Clamp(ui.PlayerBarValue + (pussySqueeze * reduction), 0f, ui.PlayerBarMax);
        ui.UpdateBars(newPlayerValue, ui.PlayerBarMax, ui.PartnerBarValue, ui.PartnerBarMax);

        npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

        CheckBarsForClimaxAndEvents();
    }

    private void UpdateUI()
    {
        float remainingCooldown = Mathf.Max(0, npcAttackBarFillTimestamp - Time.time);
        ui.UpdateCooldown(remainingCooldown);
    }

    private void HandleClimaxTimer()
    {
        if (!cumReached) return;

        cumTimeElapsed += Time.deltaTime;
        if (cumTimeElapsed >= cumDuration)
        {
            stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));
            ResetSexyTime();
        }
    }

    private void DepletePlayerBar()
    {
        if (cumReached || isFucking || barDecayPerSecond <= 0f) return;

        float oldVal = ui.PlayerBarValue;
        float newVal = Mathf.Max(0f, oldVal - barDecayPerSecond * Time.deltaTime);
        if (Mathf.Abs(newVal - oldVal) > Mathf.Epsilon)
            ui.UpdateBars(newVal, ui.PlayerBarMax, ui.PartnerBarValue, ui.PartnerBarMax);
    }

    private void CheckBarsForClimaxAndEvents()
    {
        if (ui.PlayerBarValue >= ui.PlayerBarMax && !playerBarReachedOnce)
        {
            playerBarReachedOnce = true;
            OnPlayerBarFull?.Invoke();
        }

        if (ui.PartnerBarValue >= ui.PartnerBarMax && !partnerBarReachedOnce)
        {
            partnerBarReachedOnce = true;
            OnPartnerBarFull?.Invoke();
        }

        if (ui.PlayerBarValue >= ui.PlayerBarMax || ui.PartnerBarValue >= ui.PartnerBarMax)
        {
            cumReached = true;
            cumTimeElapsed = 0f;
            stateMachine.ChangeState(new Sex_ClimaxState(this, stateMachine));
        }
    }

    private void OnBlueWinsFirst()
    {
        if (winner == FinishWinner.None)
            winner = FinishWinner.PlayerBlue;
    }

    private void OnPinkWinsFirst()
    {
        if (winner == FinishWinner.None)
            winner = FinishWinner.PartnerPink;
    }

    public void ShowCritFeedback() => ui.ShowCrit();
    public void PauseSexyTimeForDialogue() => stateMachine.PauseForDialogue();
    public void ResumeSexyTimeAfterDialogue() => stateMachine.ResumeAfterDialogue();
    public void SetIsFuckingFalse() => isFucking = false;
    public void ResetNPCAttack() => npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

    public bool IsDeepBreathUnlocked()
    {
        if (!ResolveSkillManager())
            return false;

        var deepBreath = skillManager.deepBreath;
        if (deepBreath == null)
        {
            // Try to create/resolve it and re-check
            skillManager.EnsureDeepBreathReady(true);   // NEW
            deepBreath = skillManager.deepBreath;
            if (deepBreath == null)
            {
                Debug.LogWarning("[SexyTimeLogic] skillManager exists but 'deepBreath' is NULL. Assign or create it on Player_SkillManager.");
                return false;
            }
        }

        bool unlocked = deepBreath.Unlocked(SkillUpgradeType.DeepBreath);
        if (!unlocked)
            Debug.Log("Deep Breathe is locked. Unlock via SetSkillUpgrade(...) or toggle 'autoUnlockDeepBreath' for a quick test.");

        return unlocked;
    }

    public void CastDeepBreathe()
    {
        if (!ResolveSkillManager()) { Debug.Log("Deep Breathe blocked: no SkillManager."); return; }

        // Ensure the component exists/unlocked as needed
        if (!IsDeepBreathUnlocked())
            return;

        // Delegate to the skill (handles cooldown + UI updates)
        skillManager.deepBreath.TryUseSkill(); // NEW
    }

    private void GrantSexExpIfNeeded()
    {
        if (expGranted) return;

        int amount = 0;
        switch (winner)
        {
            case FinishWinner.PlayerBlue: amount = blueBarExp; break;
            case FinishWinner.PartnerPink: amount = pinkBarExp; break;
        }

        if (amount > 0)
        {
            if (cachedPlayer == null) cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

            if (cachedPlayer != null)
            {
                cachedPlayer.GainSexEXP(amount);
                Debug.Log($"[SexyTime] Granted {amount} Sex EXP via Player.GainSexEXP ({winner})");
            }
            else
            {
                Debug.LogWarning($"[SexyTime] No Player found to award Sex EXP ({amount}).");
            }
        }

        expGranted = true;
    }
}
