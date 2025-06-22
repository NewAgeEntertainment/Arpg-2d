using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class SexyTimeLogic : MonoBehaviour
{
    [SerializeField] private SexyTimeStateMachine stateMachine;
    Entity_Stats stats;
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

    public Entity_Stats playerStats;
    public Entity_Stats partnerStats;
    [SerializeField] private TextMeshProUGUI critText;
    [SerializeField] private ParticleSystem strokeEffect;
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

    [Header("Sex Exp")]
    [SerializeField] private float sexExpToGive = 5f;
    private GameObject target;

    [Header("Player")]
    [SerializeField] private int playerID = 0;
    private Rewired.Player player;

    [Header("Input/Keybinds")]
    [SerializeField] private KeyCode strokeKey = KeyCode.P;
    [SerializeField] private KeyCode deepBreatheKey = KeyCode.T;
    [SerializeField] private KeyCode startSexyTimeKey = KeyCode.Y;

    [Header("Stroke Sound")]
    [SerializeField] private AudioClip strokeSound;
    [Header("Cumming Sound")]
    [SerializeField] private AudioClip cummingSound;

    [Header("Quest System")]
    [SerializeField] public UnityEvent OnPlayerBarFull = new UnityEvent();
    [SerializeField] public UnityEvent OnPartnerBarFull = new UnityEvent();

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

    private void OnEnable()
    {
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
    }

    void Start()
    {
        player = Rewired.ReInput.players.GetPlayer(playerID);
        anim = GetComponent<Animator>();

        if (autoStart)
            StartSexyTime();
    }

    void Update()
    {
        if (shouldPause)
            return;

        if (!isSexyTimeGoingOn && Input.GetKeyDown(startSexyTimeKey))
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

    //public void SetPlayerData(Entity_Stats playerStats, Slider playerBar, TextMeshProUGUI playerBarFillText)
    //{
    //    this.playerStats = playerStats;
    //    this.playerBar = playerBar;
    //    this.playerBarFillText = playerBarFillText;

    //    if (playerStats != null)
    //    {
    //        float maxArousal = playerStats.sex.maxArousal.GetValue();
    //        if (playerBar != null)
    //        {
    //            playerBar.maxValue = maxArousal;
    //            playerBar.value = 0f;
    //        }
    //        if (playerBarFillText != null)
    //        {
    //            playerBarFillText.text = $"0 / {Mathf.FloorToInt(maxArousal)}";
    //        }
    //    }
    //}

    //public void SetPartnerData(Entity_Stats partnerStats, Slider partnerBar, TextMeshProUGUI partnerBarFillText, float partnerMaxArousal)
    //{
    //    this.partnerStats = partnerStats;
    //    this.partnerBar = partnerBar;
    //    this.partnerBarFillText = partnerBarFillText;
    //    this.partnerMaxArousalValue = partnerMaxArousal;

    //    if (partnerBar != null)
    //    {
    //        partnerBar.maxValue = partnerMaxArousal;
    //        partnerBar.value = 0f;
    //    }
    //    if (partnerBarFillText != null)
    //    {
    //        partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerMaxArousal)}";
    //    }
    //}


    private float GetPlayerMaxArousal()
    {
        return playerStats != null ? playerStats.sex.maxArousal.GetValue() : 100f;
    }

    public void UpdateBarText()
    {
        if (playerBarFillText != null)
        {
            playerBarFillText.text = $"{Mathf.FloorToInt(playerBar.value)} / {Mathf.FloorToInt(GetPlayerMaxArousal())}";
        }

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
        float remainingCooldown = Mathf.Max(0, npcAttackBarFillTimestamp - Time.time);
        pussySqueezeCooldownText.text = remainingCooldown.ToString("F1");
    }

    private void UpdateBarUI()
    {
        playerBarFillText.text = $"{playerBar.value:F0}/{GetPlayerMaxArousal():F0}";
        partnerBarFillText.text = $"{partnerBar.value:F0}/{partnerBar.maxValue:F0}";

        playerPowerText.text = $"+{arouselPerStroke:F0}";
        partnerPowerText.text = $"+{arouselPerStroke:F0}";

        //blueBarFillPowerText.text = $"+{playerStats.GetBlueBarStrokeValue():F0}";
        //pinkBarFillPowerText.text = $"+{playerStats.GetPinkBarStrokeValue():F0}";

        if (playerBar != null && playerBarFillText != null)
            playerBarFillText.text = $"{playerBar.value:F0}/{GetPlayerMaxArousal():F0}";

        if (partnerBar != null && partnerBarFillText != null)
        {
            float maxPartner = partnerStats != null ? partnerStats.sex.maxArousal.GetValue() : partnerBar.maxValue;
            partnerBarFillText.text = $"{Mathf.FloorToInt(partnerBar.value)} / {Mathf.FloorToInt(maxPartner)}";
        }
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
        float resilienceReduction = 0f; // Define resilienceReduction variable  
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



    //sexyTimeLogic.PauseSexyTimeForDialogue();
    // dialogue runs...
    //sexyTimeLogic.ResumeSexyTimeAfterDialogue();
    public void PauseSexyTimeForDialogue()
    {
        stateMachine.PauseForDialogue();
    }

    public void ResumeSexyTimeAfterDialogue()
    {
        stateMachine.ResumeAfterDialogue();

    }

    //public void SetBlueBarFill(float value)
    //{
    //    blueBarArousalPerStroke = value;
    //}


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

            // Clamp to 0
            if (playerBar.value < 0f)
                playerBar.value = 0f;

            // Debug
            if (playerBar.value < oldPlayer)
                Debug.Log($"[DECAY] Decreased PlayerBar from {oldPlayer:F1} → {playerBar.value:F1}");
        }
    }


    public void SetIsFuckingFalse()
    {
        isFucking = false;


    }


    //bool isCoroutineRunning = false;
    public void StartSexyTime()
    {
        isSexyTimeGoingOn = true;
        this.gameObject.SetActive(true);

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

        this.gameObject.SetActive(true);
        isSexyTimeGoingOn = true;

        

        // ✅ Set up bars AFTER playerStats is valid
        if (playerStats == null)
        {
            Debug.LogError("⚠️ playerStats is NULL in StartSexyTime! Cannot set player bar max.");
        }
        else
        {
            float playerArousalMax = playerStats.sex.maxArousal.GetValue();
            Debug.Log($"✅ Setting playerBar.maxValue to {playerArousalMax}");

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
            Debug.Log($"✅ Setting partnerBar.maxValue to {partnerArousalMax}");

            // ✅ Only overwrite if null
            if (partnerBar != null)
            {
                partnerBar.maxValue = partnerArousalMax;
                partnerBar.value = 0f;
            }
            else
            {
                Debug.LogWarning("⚠️ partnerBar is NULL during StartSexyTime.");
            }

            partnerMaxArousalValue = partnerArousalMax;

            if (partnerBarFillText != null)
                partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerArousalMax)}";
            else
                Debug.LogWarning("⚠️ partnerBarFillText is NULL.");
        }

        bool isCrit; // We don’t need to use this now, but it's required by the method
        float partnerSexualDamage = partnerStats.GetSexualDamage(out isCrit);

        // Scale down if needed (to avoid huge bursts), e.g., base it on 25% of sexual damage
        pussySqueeze = pussySqueeze = Mathf.Clamp(partnerSexualDamage * 0.25f, 2f, 15f);
        originalPussySqueeze = pussySqueeze;
        originalPussySqueezeCooldown = pussySqueezeCooldown;
        Debug.Log($"[INIT] pussySqueeze set from partner sexual damage: {pussySqueeze}");


        StartCoroutine(StartSexyTimeCoroutine());
        isCoroutineRunning = true;
    }



    private IEnumerator StartSexyTimeCoroutine()
    {
        yield return new WaitForSeconds(0.75f);

        // 🎥 Enable sexy time camera and disable main camera
        if (sexyTimeCamera != null)
        {
            sexyTimeCamera.enabled = true;

            if (autoPositionSexyCamera)
            {
                sexyTimeCamera.transform.position = this.transform.position + sexyCameraOffset;
                sexyTimeCamera.transform.LookAt(this.transform.position);
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
                this.transform.position = playerObj.transform.position + offsetForPosition;

                Player playerComponent = playerObj.GetComponent<Player>();
                if (playerComponent != null)
                {
                    if (playerBar == null)
                        playerBar = playerComponent.playerBar;
                }
                else
                {
                    Debug.LogError("Player component not found on Player GameObject.");
                }
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
            canvasBackground.SetActive(true); // ✅ Enable UI canvas
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

        // Call TryUseSkill() to handle cooldown, blue bar drain, etc.
        skillManager.deepBreath.TryUseSkill();
    }

    private void CastNPCAttackBarFill()
    {
        if (Time.time >= npcAttackBarFillTimestamp && !shouldPause)
        {
            float reduction = playerStats.GetResilienceReductionMultiplier(); // Based on player's defense
            playerBar.value += pussySqueeze * reduction;
            currentArousal = playerBar.value; // Ensure currentArousal is in sync
            npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

            // Update text if applicable
            if (playerBarFillText != null)
            {
                float maxArousal = playerStats.sex.maxArousal.GetValue();
                playerBarFillText.text = $"{Mathf.FloorToInt(playerBar.value)} / {Mathf.FloorToInt(maxArousal)}";
            }

            // ✅ Check for climax
            float maxPlayerArousal = playerStats.sex.maxArousal.GetValue();
            if (playerBar.value >= maxPlayerArousal || partnerBar.value >= partnerMaxArousalValue)
            {
                cumReached = true;
                cumTimeElapsed = 0f;
                stateMachine.ChangeState(new Sex_ClimaxState(this, stateMachine));
            }

            // Optional: trigger OnPlayerBarFull if this is the first time hitting full
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
        }
    }
    public void ResetNPCAttack()
    {
        npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;
        npcAttackApplied = false;         // Or your default

        Debug.Log($"[RESET] NPC Attack reset. pussySqueeze restored to: {pussySqueeze}");
    }



    public void ResetSexyTime()
    {
            canvasBackground.SetActive(false);

        // 🎬 Switch back to normal camera
        if (sexyTimeCamera != null)
            sexyTimeCamera.enabled = false;

        if (mainGameplayCamera != null)
            mainGameplayCamera.enabled = true;

        if (canvasBackground != null)



        // Reset bars
        playerBar.value = 0f;
        partnerBar.value = 0f;

        npcAttackBarFillTimestamp = 0f;
        // Restore NPC attack values to original
        pussySqueeze = originalPussySqueeze;
        pussySqueezeCooldown = originalPussySqueezeCooldown;
        // Reset flags
        cumReached = false;
        cumTimeElapsed = 0f;
        isFucking = false;
        isCoroutineRunning = false;
        isSexyTimeGoingOn = false;


        playerBarReachedOnce = false;
        partnerBarReachedOnce = false;

        ResetNPCAttack(); // 🔁 Reset NPC attack system

        // Reset animation or state
        anim.Play("idle"); // Or whatever your default anim is



        this.gameObject.SetActive(false);
        isSexyTimeGoingOn = false;

        // Optional: Reset animation
        if (anim != null)
            anim.Play("idle", 0, 0f);

        Debug.Log("SexyTime Reset Complete");


    }


}

