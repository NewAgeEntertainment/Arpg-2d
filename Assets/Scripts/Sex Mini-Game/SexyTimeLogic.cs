using Rewired;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialogueTriggerProperties
{
    [HideInInspector] public bool isAlreadyTriggered;
    public float valueToTriggerAt;
    public NPC_Dialogue dialogueToTrigger;
}

[RequireComponent(typeof(SexyTimeStateMachine))]
public class SexyTimeLogic : MonoBehaviour
{
    public static SexyTimeLogic Current { get; private set; }

    [SerializeField] private Player_SkillManager skillManager;

    [Header("Core")]
    [SerializeField] private SexyTimeUIController ui;
    [SerializeField] private SexyTimeInputRouter inputRouter;

    [Header("Config")]
    [SerializeField] private bool autoStart = false;
    [SerializeField] private float blueDecayPerSecond = 0f;
    [SerializeField] private float pinkDecayPerSecond = 0f;
    [SerializeField] private float maxRestraintForDecay = 3f;
    [SerializeField] private float arousalPerStroke = 3f;
    [SerializeField] private float strokeMultiplier = 1f;
    [SerializeField] private float playerBarValueDeplete = 10f;

    [Header("Mini-Game Music")]
    [SerializeField] private MiniGameMusicPlayer miniGameMusicPlayer;

    [Header("SexyTime Audio")]
    [SerializeField] private string strokeBodySfx = "SexStrokeBody";
    [SerializeField] private string strokeVoiceSfx = "SexStrokeVoice";
    [SerializeField, Range(0f, 1f)] private float strokeVoiceChanceAtLowPink = 0.10f;
    [SerializeField, Range(0f, 1f)] private float strokeVoiceChanceAtHighPink = 0.85f;
    [SerializeField] private AnimationCurve strokeVoiceChanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool debugStrokeVoiceChance = false;
    [SerializeField] private string climaxSfx = "SexClimax";
    [SerializeField] private string npcAttackSfx = "SexNpcAttack";
    [SerializeField] private string critStrokeSfx = "SexCritStroke";
    [SerializeField] private string deepBreathSfx = "SexDeepBreath";
   

    [Header("Dialogue Triggers (bar thresholds)")]
    [SerializeField] private DialogueTypewriter typewriter;
    [SerializeField] private List<DialogueTriggerProperties> blueBarDialogues = new();
    [SerializeField] private List<DialogueTriggerProperties> pinkBarDialogues = new();

    [Header("Entities")]
    public Entity_Stats playerStats;
    public Entity_Stats partnerStats;

    [Header("NPC Attack skill")]
    [SerializeField] private float pussySqueeze = 10f;
    [SerializeField] private float pussySqueezeCooldown = 5f;
    private float npcAttackBarFillTimestamp;
    private float originalPussySqueeze;
    private float originalPussySqueezeCooldown;

    [Header("Partner Attack Scaling")]
    [SerializeField] private float npcAttackDamageMult = 0.25f;
    [SerializeField] private float npcAttackFlatBonus = 0f;
    [SerializeField] private float npcAttackMin = 2f;
    [SerializeField] private float npcAttackMax = 15f;

    [SerializeField] private float npcAttackCooldownMult = 1f;
    [SerializeField] private float npcAttackCooldownMin = 1f;
    [SerializeField] private float npcAttackCooldownMax = 8f;

    [Header("Partner Attack (scaled by partner level)")]
    [SerializeField] private int partnerBaseLevel = 1;

    [SerializeField] private float pussySqueezeBase = 4f;
    [SerializeField] private float pussySqueezePerLevel = 0.75f;
    [SerializeField] private float pussySqueezeMin = 2f;
    [SerializeField] private float pussySqueezeMax = 25f;

    [SerializeField] private float pussySqueezeCooldownBase = 5f;
    [SerializeField] private float cooldownReductionPerLevel = 0.03f;
    [SerializeField] private float cooldownMin = 1.25f;
    [SerializeField] private float cooldownMax = 8f;

    [Header("Skills / Debug")]
    [SerializeField] private bool autoUnlockDeepBreath = false;
    [SerializeField] private KeyCode debugDeepBreathKey = KeyCode.B;

    [Header("Intro Dialogue")]
    [SerializeField] private NPC_Dialogue startDialogue;

    [Header("Post-Climax Dialogue")]
    [SerializeField] private NPC_Dialogue blueWinFinishDialogue;
    [SerializeField] private NPC_Dialogue pinkWinFinishDialogue;

    [Header("Post-Climax Dialogue")]
    public NPC_Dialogue finishDialogue;

    [Header("Optional Intro Dialogue")]
    [SerializeField] private bool useStartDialogue = true;

    [Header("Climax")]
    public float cumDuration = 5f;
    public bool cumReached = false;
    public float cumTimeElapsed;

    [Header("Sex EXP Rewards")]
    [SerializeField] private int blueBarExp = 50;
    [SerializeField] private int pinkBarExp = 100;

    [Header("Affection Rewards / Penalties")]
    [SerializeField] private int blueBarAffectionAdd = 5;
    [SerializeField] private int blueBarAffectionSubtract = 0;
    [SerializeField] private int pinkBarAffectionAdd = 10;
    [SerializeField] private int pinkBarAffectionSubtract = 0;

    private int _suppressStrokeFrame = -999;

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
    private bool affectionGranted = false;
    private bool climaxSfxPlayed = false;

    [Header("Placement (Snap GameObject To Camera)")]
    [SerializeField] private bool snapObjectToCameraOnStart = true;
    [SerializeField] private bool followCameraWithObject = true;
    [SerializeField] private Vector3 cameraLocalObjectOffset = new Vector3(0f, 0f, 10f);
    [SerializeField] private bool lockObjectZToStartPlane = true;
    [SerializeField] private bool rotateObjectToCamera = false;

    private float _objectStartZ;
    private bool _mapsSwapped;

    private void OnEnable()
    {
        Current = this;

        OnPlayerBarFull.RemoveListener(OnBlueWinsFirst);
        OnPartnerBarFull.RemoveListener(OnPinkWinsFirst);
        OnPlayerBarFull.AddListener(OnBlueWinsFirst);
        OnPartnerBarFull.AddListener(OnPinkWinsFirst);
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;

        if (_mapsSwapped && inputRouter != null)
        {
            inputRouter.DisableSexyTimeMaps();
            _mapsSwapped = false;
        }

        OnPlayerBarFull.RemoveListener(OnBlueWinsFirst);
        OnPartnerBarFull.RemoveListener(OnPinkWinsFirst);
    }

    private void OnDestroy()
    {
        if (_mapsSwapped && inputRouter != null)
        {
            inputRouter.DisableSexyTimeMaps();
            _mapsSwapped = false;
        }
    }

    private void Awake()
    {
        stateMachine = GetComponent<SexyTimeStateMachine>();
        anim = GetComponent<Animator>();

        if (miniGameMusicPlayer == null)
            miniGameMusicPlayer = GetComponent<MiniGameMusicPlayer>();

        if (inputRouter == null)
            inputRouter = GetComponent<SexyTimeInputRouter>();
        if (inputRouter != null)
            inputRouter.Init();

        if (skillManager == null)
            skillManager = FindFirstObjectByType<Player_SkillManager>(FindObjectsInactive.Include);

        ResolveSkillManager();

        _objectStartZ = transform.position.z;
    }

    private void Start()
    {
        cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (autoStart) StartSexyTime();
    }

    private void Update()
    {
        if (shouldPause) return;

        if (!isSexyTimeGoingOn && inputRouter != null && inputRouter.StartPressed())
            StartSexyTime();

        

        if (!isSexyTimeGoingOn) return;

        if (followCameraWithObject)
            SnapObjectToCamera();

        HandleInput();
        UpdateNPCAttack();
        UpdateUI();
        HandleClimaxTimer();
        DepleteBars();
    }

    private void LateUpdate()
    {
        if (shouldPause) return;
        if (!isSexyTimeGoingOn) return;
        if (inputRouter == null) return;

        if (ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(inputRouter != null ? 0 : 0);
            if (rp != null && rp.GetButton("SkillModifier"))
                return;
        }

        if (IsStrokeSuppressedThisFrame) return;

        if (inputRouter.StrokePressed())
            stateMachine.Stroke();
    }

    public void PlayStrokeSfx()
    {
        PlaySexySfx(strokeBodySfx);

        if (string.IsNullOrWhiteSpace(strokeVoiceSfx))
            return;

        float chance = GetStrokeVoiceChanceFromPinkBar();

        if (debugStrokeVoiceChance && UI != null)
            Debug.Log($"[SexyTime Audio] Pink voice chance = {chance:P0} | Pink={UI.PartnerBarValue:F1}/{UI.PartnerBarMax:F1}");

        if (Random.value <= chance)
            PlaySexySfx(strokeVoiceSfx);
    }

    private float GetStrokeVoiceChanceFromPinkBar()
    {
        if (UI == null || UI.PartnerBarMax <= 0f)
            return strokeVoiceChanceAtLowPink;

        float pink01 = Mathf.Clamp01(UI.PartnerBarValue / UI.PartnerBarMax);

        // Lets you shape the chance in the Inspector.
        // Example: slow increase early, faster increase near climax.
        float curvedPink = strokeVoiceChanceCurve.Evaluate(pink01);

        return Mathf.Lerp(strokeVoiceChanceAtLowPink, strokeVoiceChanceAtHighPink, curvedPink);
    }
    public void PlayNpcAttackSfx() => PlaySexySfx(npcAttackSfx);
    public void PlayCritStrokeSfx() => PlaySexySfx(critStrokeSfx);
    public void PlayDeepBreathSfx() => PlaySexySfx(deepBreathSfx);

    public void PlayClimaxSfx()
    {
        if (climaxSfxPlayed) return;
        climaxSfxPlayed = true;
        PlaySexySfx(climaxSfx);
    }

    private void PlaySexySfx(string soundName)
    {
        if (AudioManager.instance == null) return;
        if (string.IsNullOrWhiteSpace(soundName)) return;

        AudioManager.instance.PlayGlobalSFX(soundName);
    }

    public void StartSexyTime()
    {
        if (isSexyTimeGoingOn)
        {
            Debug.Log("[SexyTime] StartSexyTime ignored because sexy time is already active.");
            return;
        }

        if (ui == null)
        {
            ui = FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);
            if (ui == null) { Debug.LogError("[SexyTimeLogic] UI Controller not assigned!"); return; }
        }

        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        ResolveSkillManager();
        BindToActivePlayerStats();
        ApplyPartnerFixedLevelScaling();
        RecalculatePartnerAttack();
        ResetNPCAttack();

        originalPussySqueeze = pussySqueeze;
        originalPussySqueezeCooldown = pussySqueezeCooldown;

        if (inputRouter != null && !_mapsSwapped)
        {
            inputRouter.EnableSexyTimeMaps();
            _mapsSwapped = true;
        }

        winner = FinishWinner.None;
        expGranted = false;
        affectionGranted = false;
        climaxSfxPlayed = false;
        ResetDialogueTriggers();

        deepBreatheTimestamp = Time.time - deepBreatheCooldown;
        Current = this;

        isSexyTimeGoingOn = true;
        gameObject.SetActive(true);

        miniGameMusicPlayer?.StartMiniGameMusic();

        

        float playerMax = playerStats != null ? playerStats.sex.maxArousal.GetValue() : 100f;
        float partnerMax = partnerStats != null ? partnerStats.sex.maxArousal.GetValue() : 100f;

        ui.InitBars(playerMax, partnerMax);
        ui.UpdatePower(arousalPerStroke, arousalPerStroke);
        ui.Show();
        ui.RefreshSexHotbarFromPlayer(skillManager, cachedPlayer != null ? cachedPlayer.mana : null);

        

        if (snapObjectToCameraOnStart)
            SnapObjectToCamera();

        stateMachine.logic = this;

        if (useStartDialogue && startDialogue != null && DialogueTypewriter.Instance != null)
            stateMachine.ChangeState(new Sex_StartingState(this, stateMachine, startDialogue));
        else
            stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));
    }

    private void ResetDialogueTriggers()
    {
        foreach (var t in blueBarDialogues) if (t != null) t.isAlreadyTriggered = false;
        foreach (var t in pinkBarDialogues) if (t != null) t.isAlreadyTriggered = false;
    }

    private void BindToActivePlayerStats()
    {
        if (playerStats != null) return;

        var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (p != null) playerStats = p.GetComponent<Player_Stats>();
    }

    public void SuppressStrokeThisFrame()
    {
        _suppressStrokeFrame = Time.frameCount;
    }

    public bool IsStrokeSuppressedThisFrame => _suppressStrokeFrame == Time.frameCount;

    private bool ResolveSkillManager()
    {
        if (skillManager != null) return true;

        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (cachedPlayer != null)
            skillManager = cachedPlayer.GetComponent<Player_SkillManager>();

        if (skillManager == null)
        {
            var gmPlayer = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (gmPlayer != null)
                skillManager = gmPlayer.GetComponent<Player_SkillManager>();
        }

        if (skillManager == null)
            skillManager = FindFirstObjectByType<Player_SkillManager>(FindObjectsInactive.Include);

        if (skillManager == null)
        {
            Debug.LogWarning("[SexyTimeLogic] Could not resolve Player_SkillManager. Deep Breath will be unavailable.");
            return false;
        }

        return true;
    }

    public bool CanUseSexSkill()
    {
        if (!isSexyTimeGoingOn)
            return false;

        if (cumReached)
            return false;

        if (shouldPause)
            return false;

        return true;
    }

    public void ResetSexyTime()
    {
        GrantSexExpIfNeeded();
        GrantAffectionIfNeeded();

        miniGameMusicPlayer?.StopMiniGameMusic();

       

        if (_mapsSwapped && inputRouter != null)
        {
            inputRouter.DisableSexyTimeMaps();
            _mapsSwapped = false;
        }

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
        climaxSfxPlayed = false;

        if (anim != null)
            anim.Play("idle", 0, 0f);

        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (cachedPlayer != null)
            cachedPlayer.SetInputEnabled(true);

        ui?.Show();
        ui.Hide();
        gameObject.SetActive(false);
    }

    private void HandleInput()
    {
        if (inputRouter == null) return;

        if (inputRouter.PausePressed())
            stateMachine.PauseForDialogue();
    }

    private void UpdateNPCAttack()
    {
        if (Time.time < npcAttackBarFillTimestamp || shouldPause) return;

        float reduction = playerStats != null ? playerStats.GetResilienceReductionMultiplier() : 1f;

        float newPlayerValue = Mathf.Clamp(UI.PlayerBarValue + (pussySqueeze * reduction), 0f, UI.PlayerBarMax);
        UI.UpdateBars(newPlayerValue, UI.PlayerBarMax, UI.PartnerBarValue, UI.PartnerBarMax);

        PlayNpcAttackSfx();

        npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

        CheckBarsForClimaxAndEvents();
    }

    private void RecalculatePartnerAttack()
    {
        if (partnerStats == null) return;

        float stroke = partnerStats.sex.stroke.GetValue();

        pussySqueeze = stroke * npcAttackDamageMult + npcAttackFlatBonus;
        pussySqueeze = Mathf.Clamp(pussySqueeze, npcAttackMin, npcAttackMax);

        float denom = 1f + Mathf.Max(0f, stroke) * npcAttackCooldownMult;
        float cd = pussySqueezeCooldownBase / denom;
        pussySqueezeCooldown = Mathf.Clamp(cd, npcAttackCooldownMin, npcAttackCooldownMax);

        Debug.Log($"[SexyTime] Stroke-scaled: stroke={stroke:F2} -> squeeze={pussySqueeze:F2}, cd={pussySqueezeCooldown:F2}");
    }

    private void UpdateUI()
    {
        float remainingCooldown = Mathf.Max(0, npcAttackBarFillTimestamp - Time.time);
        UI.UpdateCooldown(remainingCooldown);
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

    private void DepleteBars()
    {
        if (stateMachine == null) return;
        if (stateMachine.CurrentState is not Sex_IdleState) return;
        if (cumReached || isFucking) return;
        if (blueDecayPerSecond <= 0f && pinkDecayPerSecond <= 0f) return;

        float blueRestraint = (playerStats != null) ? playerStats.sex.sexualRestraint.GetValue() : 1f;
        float pinkRestraint = (partnerStats != null) ? partnerStats.sex.sexualRestraint.GetValue() : 1f;

        blueRestraint = Mathf.Clamp(blueRestraint, 1f, maxRestraintForDecay);
        pinkRestraint = Mathf.Clamp(pinkRestraint, 1f, maxRestraintForDecay);

        float blueOld = UI.PlayerBarValue;
        float pinkOld = UI.PartnerBarValue;

        float blueNew = Mathf.Max(0f, blueOld - (blueDecayPerSecond * blueRestraint) * Time.deltaTime);
        float pinkNew = Mathf.Max(0f, pinkOld - (pinkDecayPerSecond * pinkRestraint) * Time.deltaTime);

        if (!Mathf.Approximately(blueNew, blueOld) || !Mathf.Approximately(pinkNew, pinkOld))
            UI.UpdateBars(blueNew, UI.PlayerBarMax, pinkNew, UI.PartnerBarMax);
    }

    private void CheckBarsForClimaxAndEvents()
    {
        if (UI.PlayerBarValue >= UI.PlayerBarMax && !playerBarReachedOnce)
        {
            playerBarReachedOnce = true;
            OnPlayerBarFull?.Invoke();
        }

        if (UI.PartnerBarValue >= UI.PartnerBarMax && !partnerBarReachedOnce)
        {
            partnerBarReachedOnce = true;
            OnPartnerBarFull?.Invoke();
        }

        if (UI.PlayerBarValue >= UI.PlayerBarMax || UI.PartnerBarValue >= UI.PartnerBarMax)
        {
            cumReached = true;
            cumTimeElapsed = 0f;
            PlayClimaxSfx();
            stateMachine.ChangeState(new Sex_ClimaxState(this, stateMachine));
        }
    }

    private void TryTriggerDialogueFromBars()
    {
        if (shouldPause) return;
        if (typewriter == null) typewriter = DialogueTypewriter.Instance;
        if (typewriter == null) return;

        for (int i = 0; i < blueBarDialogues.Count; i++)
        {
            var t = blueBarDialogues[i];
            if (t == null || t.isAlreadyTriggered || t.dialogueToTrigger == null) continue;

            if (UI.PlayerBarValue >= t.valueToTriggerAt)
            {
                t.isAlreadyTriggered = true;
                TriggerDialogue(t.dialogueToTrigger);
                return;
            }
        }

        for (int i = 0; i < pinkBarDialogues.Count; i++)
        {
            var t = pinkBarDialogues[i];
            if (t == null || t.isAlreadyTriggered || t.dialogueToTrigger == null) continue;

            if (UI.PartnerBarValue >= t.valueToTriggerAt)
            {
                t.isAlreadyTriggered = true;
                TriggerDialogue(t.dialogueToTrigger);
                return;
            }
        }
    }

    private void OnEndSexyDialogue()
    {
        shouldPause = false;
        stateMachine.ResumeAfterDialogue();
    }

    public NPC_Dialogue GetFinishDialogueForWinner()
    {
        switch (winner)
        {
            case FinishWinner.PlayerBlue: return blueWinFinishDialogue;
            case FinishWinner.PartnerPink: return pinkWinFinishDialogue;
            default: return null;
        }
    }

    private void TriggerDialogue(NPC_Dialogue dialogue)
    {
        if (dialogue == null) return;

        shouldPause = true;
        stateMachine.PauseForDialogue();

        if (typewriter == null) typewriter = DialogueTypewriter.Instance;

        typewriter.StartDialogue(dialogue, () =>
        {
            shouldPause = false;
            stateMachine.ResumeAfterDialogue();
        });
    }

    public void CheckDialogueTriggersAfterBars(float blueValue, float pinkValue)
    {
        if (cumReached) return;
        if (shouldPause) return;

        for (int i = 0; i < blueBarDialogues.Count; i++)
        {
            var t = blueBarDialogues[i];
            if (t == null || t.isAlreadyTriggered || t.dialogueToTrigger == null) continue;

            if (blueValue >= t.valueToTriggerAt)
            {
                t.isAlreadyTriggered = true;
                TriggerDialogue(t.dialogueToTrigger);
                return;
            }
        }

        for (int i = 0; i < pinkBarDialogues.Count; i++)
        {
            var t = pinkBarDialogues[i];
            if (t == null || t.isAlreadyTriggered || t.dialogueToTrigger == null) continue;

            if (pinkValue >= t.valueToTriggerAt)
            {
                t.isAlreadyTriggered = true;
                TriggerDialogue(t.dialogueToTrigger);
                return;
            }
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

    public void ShowCritFeedback() => UI.ShowCrit();
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
            skillManager.EnsureDeepBreathReady(true);
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
        if (!IsDeepBreathUnlocked()) return;

        PlayDeepBreathSfx();
        skillManager.deepBreath.TryUseSkill();
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

    private void GrantAffectionIfNeeded()
    {
        if (affectionGranted) return;

        int delta = 0;
        switch (winner)
        {
            case FinishWinner.PlayerBlue:
                delta = blueBarAffectionAdd - Mathf.Abs(blueBarAffectionSubtract);
                break;
            case FinishWinner.PartnerPink:
                delta = pinkBarAffectionAdd - Mathf.Abs(pinkBarAffectionSubtract);
                break;
        }

        var profile = FindPartnerProfile();
        if (profile != null && delta != 0)
        {
            ConquestRosterManager.Instance?.AddAffection(profile, delta);
            Debug.Log($"[SexyTime] Affection delta {delta:+#;-#;0} applied to {profile.name} ({winner}).");
        }
        else if (profile == null)
        {
            Debug.LogWarning("[SexyTime] No CharacterProfileRef on partner; cannot modify affection.");
        }

        affectionGranted = true;
    }

    private void ApplyPartnerFixedLevelScaling()
    {
        if (partnerStats == null) return;

        var scaler =
            partnerStats.GetComponent<PartnerSexLevelScaler>() ??
            partnerStats.GetComponentInParent<PartnerSexLevelScaler>(true) ??
            partnerStats.GetComponentInChildren<PartnerSexLevelScaler>(true);

        if (scaler == null)
        {
            Debug.Log("[SexyTime] No PartnerSexLevelScaler found on partner.");
            return;
        }

        Debug.Log($"[SexyTime] Partner BEFORE scaling: " +
                  $"LvlScaler={scaler.name}, " +
                  $"maxArousal={partnerStats.sex.maxArousal.GetValue()}, " +
                  $"sexualDamage={partnerStats.sex.sexualDamage.GetValue()}, " +
                  $"resilience={partnerStats.sex.resilience.GetValue()}, " +
                  $"restraint={partnerStats.sex.sexualRestraint.GetValue()}, " +
                  $"stroke={partnerStats.sex.stroke.GetValue()}");

        scaler.ApplyScaling();

        Debug.Log($"[SexyTime] Partner AFTER scaling: " +
                  $"maxArousal={partnerStats.sex.maxArousal.GetValue()}, " +
                  $"sexualDamage={partnerStats.sex.sexualDamage.GetValue()}, " +
                  $"resilience={partnerStats.sex.resilience.GetValue()}, " +
                  $"restraint={partnerStats.sex.sexualRestraint.GetValue()}, " +
                  $"stroke={partnerStats.sex.stroke.GetValue()}");
    }

    private CharacterProfileSO FindPartnerProfile()
    {
        if (partnerStats == null) return null;
        var r = partnerStats.GetComponent<CharacterProfileRef>()
             ?? partnerStats.GetComponentInParent<CharacterProfileRef>(true)
             ?? partnerStats.GetComponentInChildren<CharacterProfileRef>(true);
        return r ? r.profile : null;
    }

    private Camera ResolveGameCamera() => Camera.main;

    private void SnapObjectToCamera()
    {
        var cam = ResolveGameCamera();
        if (cam == null) return;

        Vector3 worldPos = cam.transform.TransformPoint(cameraLocalObjectOffset);

        if (lockObjectZToStartPlane)
            worldPos.z = _objectStartZ;

        transform.position = worldPos;

        if (rotateObjectToCamera)
            transform.rotation = cam.transform.rotation;
    }
}