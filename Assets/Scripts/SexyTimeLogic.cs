using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Rewired;

public class SexyTimeLogic : MonoBehaviour
{
    [SerializeField] private SexyTimeStateMachine stateMachine;
    private Entity_Stats stats;
    [SerializeField] private Player_SkillManager skillManager;

    [Header("Camera Control")]
    [SerializeField] private Camera sexyTimeCamera;
    [SerializeField] private Camera mainGameplayCamera;
    [SerializeField] private CinemachineCamera sexyTimeVirtualCam;
    [SerializeField] private CinemachineCamera mainVirtualCam;
    [SerializeField] private bool autoPositionSexyCamera = true;
    [SerializeField] private Vector3 sexyCameraOffset = new Vector3(0f, 3f, -5f);

    [Tooltip("Id is from GalleryItemHolder SO")]
    [SerializeField] private int animID;

    [Header("Background & Settings")]
    [SerializeField] private GameObject canvasBackground;
    [SerializeField] private bool autoStart = false;
    [Tooltip("Should the sexy time go to the player position? If not, the sexy time will center itself to the camera. Use the offset below to offset it from the player/camera centering.")]
    [SerializeField] private bool goToPlayerPosition = true;
    [Tooltip("The offset that will be used to move the sexy time a bit from its centering (player/camera).")]
    [SerializeField] private Vector3 offsetForPosition = new Vector3(0f, -4f, 0f);

    [Header("Entities")]
    public Entity_Stats playerStats;
    public Entity_Stats partnerStats;

    [Header("FX / Feedback")]
    [SerializeField] private TextMeshProUGUI critText;
    [SerializeField] private ParticleSystem strokeEffect;
    [Header("Stroke Sound")]
    [SerializeField] private AudioClip strokeSound;
    [Header("Cumming Sound")]
    [SerializeField] private AudioClip cummingSound;

    public Animator anim { get; private set; }

    [Header("Bars")]
    public Slider partnerBar;
    public Slider playerBar;
    public float partnerMaxArousalValue;
    [HideInInspector] public float currentArousal = 0f;
    [HideInInspector] public float partnerCurrentArousal = 0f;
    private float originalPussySqueeze;
    private float originalPussySqueezeCooldown;

    public float barDecayPerSecond { get; set; }
    public float arouselPerStroke = 3f;
    public float strokeMultiplier = 1f;

    [Header("Info Text")]
    public TextMeshProUGUI playerBarFillText;
    public TextMeshProUGUI partnerBarFillText;
    [SerializeField] private TextMeshProUGUI playerPowerText;
    [SerializeField] private TextMeshProUGUI partnerPowerText;

    private bool npcAttackApplied = false;

    public float deepBreatheCooldown { get; set; } = 5f;
    public float playerBarValueDeplete { get; set; } = 10f;

    [Header("NPC Attack skill")]
    [SerializeField] private float pussySqueeze = 10f;
    [SerializeField] private float pussySqueezeCooldown = 5f;
    [SerializeField] private TextMeshProUGUI pussySqueezeCooldownText;
    [SerializeField] private bool shouldNpcAttackImmediately = false;

    [Header("Climax Reach")]
    public float cumDuration = 5f;

    [Header("Sex EXP Rewards")]
    [SerializeField] private int blueBarExp = 50;   // Player (blue) reaches max first
    [SerializeField] private int pinkBarExp = 100;  // Partner (pink) reaches max first

    private enum FinishWinner { None, PlayerBlue, PartnerPink }
    private FinishWinner winner = FinishWinner.None;
    private bool expGranted = false;

    [Header("Quest System")]
    [SerializeField] public UnityEvent OnPlayerBarFull = new UnityEvent();
    [SerializeField] public UnityEvent OnPartnerBarFull = new UnityEvent();

    [Header("Player")]
    [SerializeField] private int playerID = 0;
    private Rewired.Player player;

    [Header("Input/Keybinds")]
    [SerializeField] private KeyCode strokeKey = KeyCode.P;
    [SerializeField] private KeyCode deepBreatheKey = KeyCode.T;
    [SerializeField] private KeyCode startSexyTimeKey = KeyCode.Y;

    public static bool isSexyTimeGoingOn = false;
    public bool playerBarReachedOnce = false;
    public bool partnerBarReachedOnce = false;
    public bool isFucking { get; private set; }
    public bool isMouthOpen { get; private set; }
    public bool isCumming { get; private set; }
    private bool cachedStrokeWhileFucking = false;
    public bool cumReached = false;
    private bool isDialogueAlreadyTriggered = false;
    public bool shouldPause { get; set; }
    public bool HasSexyTimeFinished => cumTimeElapsed >= cumDuration;

    public float barSpeed = 0f;
    public float lastStrokeTimestamp { get; private set; } = 0f;
    public float timeBetweenStrokes { get; set; } = 1f;
    public float cumTimeElapsed;
    public float deepBreatheTimestamp { get; set; } = 0f;
    private float npcAttackBarFillTimestamp;

    private bool isCoroutineRunning = false;

    #region Unity

    private void OnEnable()
    {
        // Cameras
        if (!isSexyTimeGoingOn)
        {
            if (sexyTimeCamera != null) sexyTimeCamera.enabled = false;
            if (mainGameplayCamera != null) mainGameplayCamera.enabled = true;

            if (sexyTimeVirtualCam != null) sexyTimeVirtualCam.Priority = 0;
            if (mainVirtualCam != null) mainVirtualCam.Priority = 10;
        }
        else
        {
            if (sexyTimeCamera != null) sexyTimeCamera.enabled = true;
            if (mainGameplayCamera != null) mainGameplayCamera.enabled = false;

            if (sexyTimeVirtualCam != null) sexyTimeVirtualCam.Priority = 10;
            if (mainVirtualCam != null) mainVirtualCam.Priority = 0;
        }

        // ensure single subscription
        OnPlayerBarFull.RemoveListener(OnBlueWinsFirst);
        OnPartnerBarFull.RemoveListener(OnPinkWinsFirst);
        OnPlayerBarFull.AddListener(OnBlueWinsFirst);
        OnPartnerBarFull.AddListener(OnPinkWinsFirst);
    }

    private void OnDisable()
    {
        OnPlayerBarFull.RemoveListener(OnBlueWinsFirst);
        OnPartnerBarFull.RemoveListener(OnPinkWinsFirst);
    }

    private void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
        anim = GetComponent<Animator>();

        if (autoStart)
            StartSexyTime();
    }

    private void Update()
    {
        if (shouldPause)
            return;

        if (!isSexyTimeGoingOn && player.GetButtonDown("StartSexyTime"))
        {
            StartSexyTime();
        }

        if (isSexyTimeGoingOn)
        {
            HandleInput();
            UpdateAttackBarCooldown();
            UpdateBarUI();
            HandleClimaxTimer();
            HandleFuckingInput();
            DepleteBars();
        }
    }

    #endregion

    #region Winner detection

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

    #endregion

    #region Helpers / UI

    private float GetPlayerMaxArousal()
    {
        return playerStats != null ? playerStats.sex.maxArousal.GetValue() : 100f;
    }

    public void UpdateBarText()
    {
        if (playerBarFillText != null)
            playerBarFillText.text = $"{Mathf.FloorToInt(playerBar.value)} / {Mathf.FloorToInt(GetPlayerMaxArousal())}";

        if (partnerBarFillText != null)
        {
            float maxPartner = partnerStats != null ? partnerStats.sex.maxArousal.GetValue() : partnerBar.maxValue;
            partnerBarFillText.text = $"{Mathf.FloorToInt(partnerBar.value)} / {Mathf.FloorToInt(maxPartner)}";
        }
    }

    private void HandleInput()
    {
        if (player.GetButtonDown("Stroke"))
            stateMachine.Stroke();

        if (player.GetButtonDown("DeepBreathe"))
            CastDeepBreathe();
    }

    private void UpdateAttackBarCooldown()
    {
        CastNPCAttackBarFill();
        if (pussySqueezeCooldownText != null)
        {
            float remainingCooldown = Mathf.Max(0, npcAttackBarFillTimestamp - Time.time);
            pussySqueezeCooldownText.text = remainingCooldown.ToString("F1");
        }
    }

    private void UpdateBarUI()
    {
        if (playerBar != null && playerBarFillText != null)
            playerBarFillText.text = $"{playerBar.value:F0}/{GetPlayerMaxArousal():F0}";

        if (partnerBar != null && partnerBarFillText != null)
        {
            float maxPartner = partnerStats != null ? partnerStats.sex.maxArousal.GetValue() : partnerBar.maxValue;
            partnerBarFillText.text = $"{Mathf.FloorToInt(partnerBar.value)} / {Mathf.FloorToInt(maxPartner)}";
        }

        if (playerPowerText != null) playerPowerText.text = $"+{arouselPerStroke:F0}";
        if (partnerPowerText != null) partnerPowerText.text = $"+{arouselPerStroke:F0}";
    }

    private void HandleClimaxTimer()
    {
        if (!cumReached) return;

        cumTimeElapsed += Time.deltaTime;
        if (cumTimeElapsed >= cumDuration)
        {
            ResetSexyTime();
        }
    }

    private void HandleFuckingInput()
    {
        if (!isFucking || !Input.GetKeyDown(KeyCode.Space)) return;

        bool isCrit = false;
        float resilienceReduction = 0f;
        float strokePower = playerStats?.GetSexualDamage(out isCrit) ?? 10f;
        float resilience = Mathf.Clamp(partnerStats != null ? partnerStats.GetResilienceMitigation(resilienceReduction) : 0f, 0f, 50f);
        float adjustedStroke = strokePower * (1f - (resilience / 100f));

        if (isCrit)
        {
            Debug.Log("<color=magenta>💥 Critical Stroke!</color>");
            strokeEffect?.Play();
        }

        partnerBar.value += adjustedStroke * Time.deltaTime;
        cachedStrokeWhileFucking = true;

        Debug.Log($"StrokePower: {strokePower} | Resilience: {resilience}% | Final Gain: {adjustedStroke}");
    }

    public void PauseSexyTimeForDialogue()
    {
        stateMachine.PauseForDialogue();
    }

    public void ResumeSexyTimeAfterDialogue()
    {
        stateMachine.ResumeAfterDialogue();
    }

    public void UpdateStrokeMultiplier(float multiplier)
    {
        strokeMultiplier = multiplier;
    }

    public void ShowCritFeedback()
    {
        if (strokeEffect) strokeEffect.Play();
        if (critText)
        {
            critText.text = "💥 Critical Stroke!";
            critText.gameObject.SetActive(true);
            StartCoroutine(HideCritText());
        }
    }

    IEnumerator HideCritText()
    {
        yield return new WaitForSeconds(1f);
        critText.gameObject.SetActive(false);
    }

    private void DepleteBars()
    {
        if (!cumReached && !isFucking)
        {
            float oldPlayer = playerBar.value;
            playerBar.value -= barDecayPerSecond * Time.deltaTime;

            if (playerBar.value < 0f)
                playerBar.value = 0f;

            if (playerBar.value < oldPlayer)
                Debug.Log($"[DECAY] Decreased PlayerBar from {oldPlayer:F1} → {playerBar.value:F1}");
        }
    }

    public void SetIsFuckingFalse()
    {
        isFucking = false;
    }

    #endregion

    #region Flow

    public void StartSexyTime()
    {
        // reset winner/xp
        winner = FinishWinner.None;
        expGranted = false;

        isSexyTimeGoingOn = true;
        gameObject.SetActive(true);

        if (sexyTimeCamera != null) sexyTimeCamera.enabled = true;
        if (mainGameplayCamera != null) mainGameplayCamera.enabled = false;

        if (sexyTimeVirtualCam != null) sexyTimeVirtualCam.Priority = 10;
        if (mainVirtualCam != null) mainVirtualCam.Priority = 0;

        if (canvasBackground != null && sexyTimeCamera != null)
        {
            canvasBackground.transform.SetParent(sexyTimeCamera.transform, false);
            canvasBackground.transform.localPosition = new Vector3(0f, 0f, 2f);
            canvasBackground.transform.localRotation = Quaternion.identity;
            canvasBackground.transform.localScale = Vector3.one;
            canvasBackground.SetActive(true);
        }

        stateMachine = GetComponent<SexyTimeStateMachine>();
        stateMachine.logic = this;
        stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));

        if (isCoroutineRunning)
            return;

        // ✅ Set up bars AFTER playerStats is valid
        if (playerStats == null)
        {
            Debug.LogError("⚠️ playerStats is NULL in StartSexyTime! Cannot set player bar max.");
        }
        else
        {
            float playerArousalMax = playerStats.sex.maxArousal.GetValue();
            playerBar.maxValue = playerArousalMax;
            playerBar.value = 0f;

            if (playerBarFillText != null)
                playerBarFillText.text = $"0 / {Mathf.FloorToInt(playerArousalMax)}";
        }

        if (partnerStats == null)
        {
            Debug.LogWarning("⚠️ partnerStats is NULL in StartSexyTime! Cannot set partner bar max.");
        }
        else
        {
            float partnerArousalMax = partnerStats.sex.maxArousal.GetValue();

            if (partnerBar != null)
            {
                partnerBar.maxValue = partnerArousalMax;
                partnerBar.value = 0f;
            }

            partnerMaxArousalValue = partnerArousalMax;

            if (partnerBarFillText != null)
                partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerArousalMax)}";
        }

        bool isCrit;
        float partnerSexualDamage = partnerStats != null ? partnerStats.GetSexualDamage(out isCrit) : 5f;

        pussySqueeze = Mathf.Clamp(partnerSexualDamage * 0.25f, 2f, 15f);
        originalPussySqueeze = pussySqueeze;
        originalPussySqueezeCooldown = pussySqueezeCooldown;

        StartCoroutine(StartSexyTimeCoroutine());
        isCoroutineRunning = true;
    }

    private IEnumerator StartSexyTimeCoroutine()
    {
        yield return new WaitForSeconds(0.75f);

        if (sexyTimeCamera != null)
        {
            sexyTimeCamera.enabled = true;

            if (autoPositionSexyCamera)
            {
                sexyTimeCamera.transform.position = transform.position + sexyCameraOffset;
                sexyTimeCamera.transform.LookAt(transform.position);
            }
        }

        if (mainGameplayCamera != null)
        {
            mainGameplayCamera.enabled = false;
        }

        if (goToPlayerPosition)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                transform.position = playerObj.transform.position + offsetForPosition;
            }
            else
            {
                Debug.LogError("Player GameObject with tag 'Player' not found.");
            }
        }

        if (canvasBackground != null)
        {
            canvasBackground.transform.SetParent(sexyTimeCamera.transform);
            canvasBackground.transform.localPosition = new Vector3(0, 0, 1f);
            canvasBackground.transform.localRotation = Quaternion.identity;
            canvasBackground.transform.localScale = Vector3.one;
            canvasBackground.SetActive(true);
        }
    }

    private void OnEndDialogue()
    {
        shouldPause = false;
    }

    public bool IsDeepBreathUnlocked()
    {
        SexSkill_DeepBreath deepBreath = skillManager?.deepBreath;
        return deepBreath != null && deepBreath.Unlocked(SkillUpgradeType.DeepBreath);
    }

    public void CastDeepBreathe()
    {
        if (!IsDeepBreathUnlocked())
        {
            Debug.Log("Deep Breathe is locked.");
            return;
        }

        skillManager.deepBreath.TryUseSkill();
    }

    private void CastNPCAttackBarFill()
    {
        if (Time.time >= npcAttackBarFillTimestamp && !shouldPause)
        {
            float reduction = playerStats != null ? playerStats.GetResilienceReductionMultiplier() : 1f;
            playerBar.value += pussySqueeze * reduction;
            currentArousal = playerBar.value;
            npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

            if (playerBarFillText != null && playerStats != null)
            {
                float maxArousal = playerStats.sex.maxArousal.GetValue();
                playerBarFillText.text = $"{Mathf.FloorToInt(playerBar.value)} / {Mathf.FloorToInt(maxArousal)}";
            }

            float maxPlayerArousal = playerStats != null ? playerStats.sex.maxArousal.GetValue() : playerBar.maxValue;

            if (playerBar.value >= maxPlayerArousal && !playerBarReachedOnce)
            {
                playerBarReachedOnce = true;
                OnPlayerBarFull?.Invoke();
            }

            if (partnerBar.value >= partnerMaxArousalValue && !partnerBarReachedOnce)
            {
                partnerBarReachedOnce = true;
                OnPartnerBarFull?.Invoke();
            }

            // ✅ Check for climax
            if (playerBar.value >= maxPlayerArousal || partnerBar.value >= partnerMaxArousalValue)
            {
                cumReached = true;
                cumTimeElapsed = 0f;
                stateMachine.ChangeState(new Sex_ClimaxState(this, stateMachine));
            }
        }
    }

    public void ResetNPCAttack()
    {
        npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;
        npcAttackApplied = false;
    }

    public void ResetSexyTime()
    {
        // Award Sex EXP
        GrantSexExpIfNeeded();

        if (canvasBackground != null)
            canvasBackground.SetActive(false);

        if (sexyTimeCamera != null)
            sexyTimeCamera.enabled = false;

        if (mainGameplayCamera != null)
            mainGameplayCamera.enabled = true;

        if (playerBar != null) playerBar.value = 0f;
        if (partnerBar != null) partnerBar.value = 0f;

        npcAttackBarFillTimestamp = 0f;

        pussySqueeze = originalPussySqueeze;
        pussySqueezeCooldown = originalPussySqueezeCooldown;

        cumReached = false;
        cumTimeElapsed = 0f;
        isFucking = false;
        isCoroutineRunning = false;
        isSexyTimeGoingOn = false;

        playerBarReachedOnce = false;
        partnerBarReachedOnce = false;

        ResetNPCAttack();

        if (anim != null)
            anim.Play("idle", 0, 0f);

        gameObject.SetActive(false);
    }

    #endregion

    #region EXP

    private void GrantSexExpIfNeeded()
    {
        if (expGranted) return;

        int amount = 0;
        switch (winner)
        {
            case FinishWinner.PlayerBlue: amount = blueBarExp; break;
            case FinishWinner.PartnerPink: amount = pinkBarExp; break;
            default: amount = 0; break;
        }

        if (amount > 0)
        {
            // Prefer going through Player so it can centralize UI updates, events, etc.
            var playerComponent = FindFirstObjectByType<Player>();
            if (playerComponent != null)
            {
                playerComponent.GainSexEXP(amount);
                Debug.Log($"[SexyTime] Granted {amount} Sex EXP to Player via Player.GainSexEXP ({winner})");
            }
            else
            {
                // Fallback: write directly to stats if for some reason Player isn't around
                Player_Stats pStats = null;

                if (playerStats != null)
                    pStats = playerStats as Player_Stats ?? playerStats.GetComponent<Player_Stats>();

                if (pStats == null)
                    pStats = FindFirstObjectByType<Player_Stats>();

                if (pStats != null)
                {
                    pStats.AddSexEXP(amount);
                    Debug.Log($"[SexyTime] Granted {amount} Sex EXP directly to Player_Stats ({winner})");
                }
                else
                {
                    Debug.LogWarning($"[SexyTime] Could not find Player or Player_Stats to give Sex EXP ({amount}).");
                }
            }
        }

        expGranted = true;
    }


    #endregion
}
