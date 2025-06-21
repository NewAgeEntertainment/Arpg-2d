using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Rewired;


public class SexyTimeLogic : MonoBehaviour
{
    [SerializeField] private SexyTimeStateMachine stateMachine;
    Entity_Stats stats;
    [SerializeField] private Player_SkillManager skillManager;

    //[Header("Gallery")]
    //[SerializeField] private GalleryUnlockedItems unlockedSO;
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
    [SerializeField] private Entity_Stats partnerStats; // Optional: the receiver of the stroking
    [SerializeField] private TextMeshProUGUI critText;
    [SerializeField] private ParticleSystem strokeEffect;
    public Animator anim { get; private set; }

    //public event Action OnCriticalStroke;

    #region settings
    [Header("Bars")]
    public Slider partnerBar;
    public Slider playerBar;
    public float partnerMaxArousalValue;
    [HideInInspector] public float currentArousal = 0f;
    private float blueBarArousalPerStroke;
    private float partnerArousalPerStroke;
    public float barDecayPerSecond { get; set; }

    public float arouselPerStroke = 3f;
    public float strokeMultiplier = 1f;

    [Header("Info Text")]
    public TextMeshProUGUI playerBarFillText;
    public TextMeshProUGUI partnerBarFillText;
    [SerializeField] private TextMeshProUGUI playerPowerText;
    [SerializeField] private TextMeshProUGUI partnerPowerText;

    

    public float deepBreatheCooldown { get; set; } = 5f; // Cooldown for deep breathing
    public float playerBarValueDeplete { get; set; } = 10f; // Amount to deplete from blue bar when deep breathing

    [Header("NPC Attack skill")]
    [SerializeField] private float pussySqueeze = 10f;
    [SerializeField] private float pussySqueezeCooldown = 5f;
    [SerializeField] private TextMeshProUGUI pussySqueezeCooldownText;
    [Tooltip("True if the npc attack should be executed immediately when starting the sexy time. If false, it will wait for the cooldown initially.")]
    [SerializeField] private bool shouldNpcAttackImmediately = false;

    [Header("Climax Reach")]
    public float cumDuration = 5f;

    [Header("Sex Exp")]
    [SerializeField] private float sexExpToGive = 5f;
    private GameObject target; // Add this field to define 'target'


    [Header("Player")]
    [SerializeField] private int playerID = 0;
    private Rewired.Player player;

    [Header("Input/Keybinds")]
    [SerializeField] private KeyCode strokeKey = KeyCode.P;
    [SerializeField] private KeyCode deepBreatheKey = KeyCode.T;

    [Header("Stroke Sound")]
    [SerializeField] private AudioClip strokeSound;
    [Header("Cumming Sound")]
    [SerializeField] private AudioClip cummingSound;

    [Header("Quest System")]
    [SerializeField] public UnityEvent OnPlayerBarFull = new UnityEvent();
    [SerializeField] public UnityEvent OnPartnerBarFull = new UnityEvent();
    #endregion


    #region bools
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
    #endregion

    #region floats
    public float barSpeed = 0f;
    public float lastStrokeTimestamp { get; private set; } = 0f; // Timestamp of the last stroke
    public float timeBetweenStrokes { get; set; } = 1f; // Time in seconds between strokes
    public float cumTimeElapsed;
    public float deepBreatheTimestamp { get; set; } = 0f; // Timestamp for the last deep breathe action
    private float npcAttackBarFillTimestamp;
    #endregion


    // Start is called before the first frame update
    void Start()
{
    stateMachine = GetComponent<SexyTimeStateMachine>();
    stateMachine.logic = this;
    stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));

    // ✅ Apply stat setup FIRST
    playerStats.ApplyDefaultStatSetup(); // Make sure this is done BEFORE using any GetValue()

    player = Rewired.ReInput.players.GetPlayer(playerID);
    anim = GetComponent<Animator>();

    // ✅ NOW we can safely fetch max arousal
    float playerArousalMax = playerStats.GetMaxArousel(); // uses sex.maxArousal + vitality * 5
    playerBar.maxValue = playerArousalMax;
    playerBar.value = 0f;

    if (playerBarFillText != null)
        playerBarFillText.text = $"0 / {Mathf.FloorToInt(playerArousalMax)}";

    if (playerBar != null)
        playerBar.value = 0;

    if (partnerBar != null)
        partnerBar.value = 0;

    partnerBar.maxValue = partnerMaxArousalValue;

    if (partnerBarFillText != null)
        partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerMaxArousalValue)}";

    if (!shouldNpcAttackImmediately)
        npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;

    if (autoStart)
        StartSexyTime();
}





    // Update is called once per frame
    void Update()
    {
       

        if (shouldPause)
            return;

        HandleInput();
        UpdateAttackBarCooldown();
        UpdateBarUI();
        HandleClimaxTimer();
        HandleFuckingInput();
        DepleteBars();

    }

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
            partnerBarFillText.text = $"{Mathf.FloorToInt(partnerBar.value)} / {Mathf.FloorToInt(partnerMaxArousalValue)}";
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

    public void SetBlueBarFill(float value)
    {
        blueBarArousalPerStroke = value;
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


    bool isCoroutineRunning = false;
    public void StartSexyTime()
    {
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

        StartCoroutine(StartSexyTimeCoroutine());
        isCoroutineRunning = true;
    }



    private IEnumerator StartSexyTimeCoroutine()
    {
        yield return new WaitForSeconds(0.75f);


        if (canvasBackground != null)
            canvasBackground.SetActive(true);

        if (goToPlayerPosition)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                this.transform.position = playerObj.transform.position + offsetForPosition;

                Player playerComponent = playerObj.GetComponent<Player>();
                if (playerComponent != null)
                {
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
            float reduction = playerStats.GetResilienceReductionMultiplier(); // or targetStats, depending who’s getting hit
            playerBar.value += pussySqueeze * reduction;
            npcAttackBarFillTimestamp = Time.time + pussySqueezeCooldown;
        }
    }
 

    public void ResetSexyTime()
    {
        // Reset bars
        playerBar.value = 0f;
        partnerBar.value = 0f;


        pussySqueeze = 0f;
        pussySqueezeCooldown = 0f;
        // Reset flags
        cumReached = false;
        cumTimeElapsed = 0f;
        isFucking = false;
        isCoroutineRunning = false;
        isSexyTimeGoingOn = false;

       

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

