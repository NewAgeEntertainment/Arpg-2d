using Rewired;
using System.Collections;
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

    [Header("Skills / Debug")]
    [SerializeField] private bool autoUnlockDeepBreath = false;
    [SerializeField] private KeyCode debugDeepBreathKey = KeyCode.B;

    [Header("Climax")]
    public float cumDuration = 5f;
    public bool cumReached = false;
    public float cumTimeElapsed;

    [Header("Sex EXP Rewards")]
    [SerializeField] private int blueBarExp = 50;
    [SerializeField] private int pinkBarExp = 100;

    [Header("Affection Rewards / Penalties")]
    [Tooltip("When BLUE (player) reaches climax first, add this many affection points.")]
    [SerializeField] private int blueBarAffectionAdd = 5;

    [Tooltip("When BLUE wins, subtract this many affection points as a penalty (set 0 to ignore).")]
    [SerializeField] private int blueBarAffectionSubtract = 0;

    [Tooltip("When PINK (partner) reaches climax first, add this many affection points.")]
    [SerializeField] private int pinkBarAffectionAdd = 10;

    [Tooltip("When PINK wins, subtract this many affection points as a penalty (set 0 to ignore).")]
    [SerializeField] private int pinkBarAffectionSubtract = 0;

    // ===== Stroke suppression (prevents stroke when a sex skill uses on same button) =====
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

    // ─────────────────────────────────────────────────────────────
    // Placement: Snap THIS mini-game object to the **Camera**
    [Header("Placement (Snap GameObject To Camera)")]
    [Tooltip("If true, when SexyTime starts, this GameObject snaps to the main camera (with a local offset).")]
    [SerializeField] private bool snapObjectToCameraOnStart = true;

    [Tooltip("If true, while SexyTime is active, this GameObject follows the camera every frame.")]
    [SerializeField] private bool followCameraWithObject = true;

    [Tooltip("Offset in camera LOCAL space (x=right, y=up, z=forward). For 2D, set z ≈ distance from camera to gameplay plane (e.g., 10).")]
    [SerializeField] private Vector3 cameraLocalObjectOffset = new Vector3(0f, 0f, 10f);

    [Tooltip("Keep the object's Z on its original plane (useful for 2D so sorting/layers remain correct).")]
    [SerializeField] private bool lockObjectZToStartPlane = true;

    [Tooltip("If true, rotate this object to face/align with the camera.")]
    [SerializeField] private bool rotateObjectToCamera = false;

    private float _objectStartZ;
    // ─────────────────────────────────────────────────────────────

    // Track map swap to avoid double toggles & ensure restore
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

        // Failsafe: always restore gameplay maps if disabled mid-minigame
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
        // Same failsafe as OnDisable
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

        if (debugDeepBreathKey != KeyCode.None && Input.GetKeyDown(debugDeepBreathKey))
            CastDeepBreathe();

        if (!isSexyTimeGoingOn) return;

        if (followCameraWithObject)
            SnapObjectToCamera();

        HandleInput();
        UpdateNPCAttack();
        UpdateUI();
        HandleClimaxTimer();
        DepletePlayerBar();
    }

    private void LateUpdate()
    {
        if (shouldPause) return;
        if (!isSexyTimeGoingOn) return;
        if (inputRouter == null) return;

        // NEW: while holding SkillModifier, stroke is NOT allowed
        if (ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(0); // or your playerID if you use one here
            if (rp != null && rp.GetButton("SkillModifier"))
                return;
        }

        if (IsStrokeSuppressedThisFrame) return;

        if (inputRouter.StrokePressed())
            stateMachine.Stroke();
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

        ResolveSkillManager();
        skillManager?.EnsureDeepBreathReady(true);
        BindToActivePlayerStats();

        deepBreatheTimestamp = Time.time - deepBreatheCooldown;

        Current = this;

        // 🔁 Switch Rewired maps: Gameplay OFF, SexyTime ON
        if (inputRouter != null && !_mapsSwapped)
        {
            inputRouter.EnableSexyTimeMaps();
            _mapsSwapped = true;
        }



        winner = FinishWinner.None;
        expGranted = false;
        affectionGranted = false;
        isSexyTimeGoingOn = true;
        gameObject.SetActive(true);

        // Init / show UI
        float playerMax = playerStats != null ? playerStats.sex.maxArousal.GetValue() : 100f;
        float partnerMax = partnerStats != null ? partnerStats.sex.maxArousal.GetValue() : 100f;
        ui.InitBars(playerMax, partnerMax);
        ui.UpdatePower(arousalPerStroke, arousalPerStroke);
        ui.Show();

        if (snapObjectToCameraOnStart)
            SnapObjectToCamera();

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

    private void BindToActivePlayerStats()
    {
        var p = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (p != null)
        {
            var ps = p.GetComponent<Player_Stats>();
            if (ps != null && playerStats != ps)
                playerStats = ps;
        }
    }

    

    /// <summary>Call this when a sex hotbar skill successfully fires.</summary>
    public void SuppressStrokeThisFrame()
    {
        _suppressStrokeFrame = Time.frameCount;
    }

    /// <summary>True if stroke should be blocked this frame.</summary>
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

    public void ResetSexyTime()
    {
        GrantSexExpIfNeeded();
        GrantAffectionIfNeeded();

        // Turn SexyTime OFF, Gameplay ON
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

        if (anim != null)
            anim.Play("idle", 0, 0f);

        // ✅ Restore PLAYER script input gate NOW (before disabling this GO)
        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (cachedPlayer != null)
            cachedPlayer.SetInputEnabled(true);

        ui.Hide();
        gameObject.SetActive(false);
    }

    private void HandleInput()
    {
        if (inputRouter == null) return;

        // Stroke moved to LateUpdate() so sex skills can suppress it this frame

        if (inputRouter.DeepBreathePressed())
            CastDeepBreathe();

        if (inputRouter.PausePressed())
            stateMachine.PauseForDialogue();
    }


    private void UpdateNPCAttack()
    {
        if (Time.time < npcAttackBarFillTimestamp || shouldPause) return;

        float reduction = playerStats != null ? playerStats.GetResilienceReductionMultiplier() : 1f;

        float newPlayerValue = Mathf.Clamp(UI.PlayerBarValue + (pussySqueeze * reduction), 0f, UI.PlayerBarMax);
        UI.UpdateBars(newPlayerValue, UI.PlayerBarMax, UI.PartnerBarValue, UI.PartnerBarMax);

        npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

        CheckBarsForClimaxAndEvents();
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

    private void DepletePlayerBar()
    {
        if (cumReached || isFucking || barDecayPerSecond <= 0f) return;

        float oldVal = UI.PlayerBarValue;
        float newVal = Mathf.Max(0f, oldVal - barDecayPerSecond * Time.deltaTime);
        if (Mathf.Abs(newVal - oldVal) > Mathf.Epsilon)
            UI.UpdateBars(newVal, UI.PlayerBarMax, UI.PartnerBarValue, UI.PartnerBarMax);
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

    private CharacterProfileSO FindPartnerProfile()
    {
        if (partnerStats == null) return null;
        var r = partnerStats.GetComponent<CharacterProfileRef>()
             ?? partnerStats.GetComponentInParent<CharacterProfileRef>(true)
             ?? partnerStats.GetComponentInChildren<CharacterProfileRef>(true);
        return r ? r.profile : null;
    }

    // ─────────────────────────────────────────────────────────────
    // Camera snap helpers (ONLY placement behavior kept)

    private Camera ResolveGameCamera() => Camera.main;

    private void SnapObjectToCamera()
    {
        var cam = ResolveGameCamera();
        if (cam == null) return;

        Vector3 worldPos = cam.transform.TransformPoint(cameraLocalObjectOffset);

        if (lockObjectZToStartPlane)
            worldPos.z = _objectStartZ; // keep on original plane (2D-friendly)

        transform.position = worldPos;

        if (rotateObjectToCamera)
            transform.rotation = cam.transform.rotation;
    }
    // ─────────────────────────────────────────────────────────────
}
