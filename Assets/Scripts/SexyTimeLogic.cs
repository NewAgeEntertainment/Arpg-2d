using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using rewired = Rewired;

public class SexyTimeLogic : MonoBehaviour
{
    [SerializeField] private SexyTimeStateMachine stateMachine;
    Entity_Stats stats;


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


    public float barSpeed = 0f;

    [SerializeField] private ParticleSystem strokeEffect;
    [SerializeField] private TextMeshProUGUI critText;

    [Header("Bars")]
    public Slider pinkBar, blueBar;
    public float pinkBarMaxValue;
    public float blueBarMaxValue;
    private float blueBarFillPerStroke { get; set; }
    private float pinkBarFillPerStroke { get; set; }
    public float barDecayPerSecond { get; set; }

    public float baseBlueBarFillPerStroke = 3f;
    public float strokeMultiplier = 1f;

    [Header("Info Text")]
    [SerializeField] private TextMeshProUGUI blueBarFillText;
    [SerializeField] private TextMeshProUGUI pinkBarFillText;
    [SerializeField] private TextMeshProUGUI blueBarFillPowerText, pinkBarFillPowerText;

    [Header("Deep Breathe")]
    [SerializeField] private float blueBarValueDeplete = 10f;
    [SerializeField] private float deepBreatheCooldown = 3f;

    [Header("NPC Attack Bar Fill")]
    [SerializeField] private float npcAttackBarFillValue = 10f;
    [SerializeField] private float npcAttackBarFillCooldown = 5f;
    [SerializeField] private TextMeshProUGUI npcAttackBarFillCooldownText;
    [Tooltip("True if the npc attack should be executed immediately when starting the sexy time. If false, it will wait for the cooldown initially.")]
    [SerializeField] private bool shouldNpcAttackImmediately = false;

    [Header("Climax Reach")]
    public float cumDuration = 5f;

    [Header("Sex Exp")]
    [SerializeField] private float sexExpToGive = 5f;
    private GameObject target; // Add this field to define 'target'


    [Header("Player")]
    [SerializeField] private int playerID = 0;
    private rewired.Player player;

    [Header("Input/Keybinds")]
    [SerializeField] private KeyCode strokeKey = KeyCode.P;
    [SerializeField] private KeyCode deepBreatheKey = KeyCode.T;

    [Header("Stroke Sound")]
    [SerializeField] private AudioClip strokeSound;
    [Header("Cumming Sound")]
    [SerializeField] private AudioClip cummingSound;

    [Header("Quest System")]
    [SerializeField] public UnityEvent OnBlueBarFull = new UnityEvent();
    [SerializeField] public UnityEvent OnPinkBarFull = new UnityEvent();

    public static bool isSexyTimeGoingOn = false;
    public bool blueBarReachedOnce = false;
    public bool pinkBarReachedOnce = false;

    public Animator anim { get; private set; }

    public bool isFucking { get; private set; }
    public bool isMouthOpen { get; private set; }
    public bool isCumming { get; private set; }
    private bool cachedStrokeWhileFucking = false;
    public bool cumReached = false;
    private bool isDialogueAlreadyTriggered = false;
    public bool shouldPause { get; set; }

    private float lastStrokeTimestamp;
    private float timeBetweenStrokes = 1f;
    public float cumTimeElapsed;
    private float deepBreatheTimestamp;
    private float npcAttackBarFillTimestamp;
    //private CharacterActionsInputs playerInputActions;

    public bool HasSexyTimeFinished => cumTimeElapsed >= cumDuration;

    // Start is called before the first frame update
    void Start()
    {
        stateMachine = GetComponent<SexyTimeStateMachine>();
        stateMachine.logic = this;
        stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));
        // Enter that state

        

        //playerInputActions = new CharacterActionsInputs();
        //foreach (InputAction action in playerInputActions)
        //{
        //    string overridePath = PlayerPrefs.GetString($"CharacterActionsInputs_{action.name}", null);
        //    if (!string.IsNullOrEmpty(overridePath))
        //        action.ApplyBindingOverride(overridePath);
        //}

        //playerInputActions.Player.Enable();
        //playerInputActions.Player.StrokeSexyGame.performed += StrokeLogic;
        //playerInputActions.Player.StrokeSexyGame.Enable();

        //playerInputActions.Player.DeepBreatheSkillSexyGame.performed += CastDeepBreathe;
        //playerInputActions.Player.DeepBreatheSkillSexyGame.Enable();

        player = rewired.ReInput.players.GetPlayer(playerID);

        anim = GetComponent<Animator>();

        //if (StatsManager.Instance.sexLevel > 1)
        //{
        //    blueBarMaxValue -= StatsManager.Instance.blueBarMaxValuePerLevel;
        //    blueBarMaxValue += StatsManager.Instance.blueBarMaxValuePerLevel * StatsManager.Instance.sexLevel;
        //}

        

        blueBar.maxValue = blueBarMaxValue;
        pinkBar.maxValue = pinkBarMaxValue;

        if (!shouldNpcAttackImmediately)
            npcAttackBarFillTimestamp = Time.time + npcAttackBarFillCooldown;

        if (autoStart)
            StartSexyTime();
    }



    // Update is called once per frame
    void Update()
    {
        //if (cumReached)
        //{
        //    cumTimeElapsed += Time.deltaTime;

        //    if (cumTimeElapsed >= cumDuration)
        //    {
        //        Debug.Log("Cum duration over, resetting sexy time");
        //        ResetSexyTime();
        //    }
        //}

        if (shouldPause)
            return;

        HandleInput();
        UpdateAttackBarCooldown();
        UpdateBarUI();
        HandleClimaxTimer();
        HandleFuckingInput();
        DepleteBars();

    }

    private void HandleInput()
    {
        if (player.GetButtonDown("Stroke"))
            stateMachine.Stroke();

        if (player.GetButtonDown("DeepBreathe"))
            CastDeepBreathe(default);
    }

    private void UpdateAttackBarCooldown()
    {
        CastNPCAttackBarFill();
        float remainingCooldown = Mathf.Max(0, npcAttackBarFillTimestamp - Time.time);
        npcAttackBarFillCooldownText.text = remainingCooldown.ToString("F1");
    }

    private void UpdateBarUI()
    {
        blueBarFillText.text = $"{blueBar.value:F0}/{blueBar.maxValue:F0}";
        pinkBarFillText.text = $"{pinkBar.value:F0}/{pinkBar.maxValue:F0}";

        blueBarFillPowerText.text = $"+{playerStats.GetBlueBarStrokeValue():F0}";
        pinkBarFillPowerText.text = $"+{playerStats.GetPinkBarStrokeValue():F0}";
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
        float strokePower = playerStats?.GetSexualStrokeDamage(out isCrit) ?? 10f;
        float resilience = Mathf.Clamp(partnerStats?.GetSexualResistance() ?? 0f, 0f, 50f);
        float adjustedStroke = strokePower * (1f - (resilience / 100f));

        if (isCrit)
        {
            Debug.Log("<color=magenta>💥 Critical Stroke!</color>");
            strokeEffect?.Play();
        }

        pinkBar.value += adjustedStroke * Time.deltaTime;
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
        blueBarFillPerStroke = value;
    }


    public void UpdateStrokeMultiplier(float multiplier)
    {
        strokeMultiplier = multiplier;
    }

    void ShowCritFeedback()
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

    //private void StrokeLogic(InputAction.CallbackContext context)
    //{
    //    if (!cumReached && !shouldPause)
    //    {
    //        timeBetweenStrokes = Time.time - lastStrokeTimestamp;
    //        Debug.Log(timeBetweenStrokes);
    //        if (!isFucking)
    //        {
    //            InitiateStroke();
    //        }
    //        else
    //        {
    //            cachedStrokeWhileFucking = true;
    //        }

    //        lastStrokeTimestamp = Time.time;

    //    }
    //}

    private void DepleteBars()
    {
        if (!cumReached && !isFucking)
        {
            blueBar.value -= barDecayPerSecond * Time.deltaTime;
            pinkBar.value -= barDecayPerSecond * Time.deltaTime;

            if (blueBar.value <= 0f)
                blueBar.value = 0f;

            if (pinkBar.value <= 0f)
                pinkBar.value = 0f;
        }
    }

    public void SetIsFuckingFalse()
    {
        isFucking = false;

       
    }



    //public void PerformStroke()
    //{
    //    float blueGain = playerStats != null ? playerStats.GetBlueBarStrokeValue() : 0f;
    //    float pinkGain = playerStats != null ? playerStats.GetPinkBarStrokeValue() : 0f;

    //    blueBar.value += blueGain;
    //    pinkBar.value += pinkGain;
    //}

    //private void InitiateStroke()
    //{
    //    if (!isFucking && !cumReached)
    //    {
    //        anim.SetFloat("speed", GetSpeed());

    //        if (!isMouthOpen)
    //        {
    //            anim.Play("fuck", 0, 0f);
    //        }
    //        else
    //        {
    //            anim.Play("fuck", 0, 0f);
    //        }

    //        // Get stroke values from stats
    //        float blueGain = playerStats != null ? playerStats.GetBlueBarStrokeValue() : 0f;
    //        float pinkGain = playerStats != null ? playerStats.GetPinkBarStrokeValue() : 0f;

    //        blueBar.value += blueGain;
    //        pinkBar.value += pinkGain;

    //        if (blueBar.value >= blueBarMaxValue || pinkBar.value >= pinkBarMaxValue)
    //        {
    //            anim.Play("sperm shot", 0, 0f);
    //            cumReached = true;

    //            //if (SoundManager.instance != null && cummingSound != null)
    //            //    SoundManager.instance.PlaySound(cummingSound);
    //        }

    //        if (blueBar.value >= blueBarMaxValue && !blueBarReachedOnce)
    //        {
    //            blueBarReachedOnce = true;
    //            Debug.Log("Blue BAR FULL QUEST");
    //            OnBlueBarFull?.Invoke();
    //        }

    //        if (pinkBar.value >= pinkBarMaxValue && !pinkBarReachedOnce)
    //        {
    //            pinkBarReachedOnce = true;
    //            Debug.Log("Pink BAR FULL QUEST");
    //            OnPinkBarFull?.Invoke();
    //        }

    //        isFucking = true;
    //    }
    //}

    bool isCoroutineRunning = false;
    public void StartSexyTime()
    {

        stateMachine = GetComponent<SexyTimeStateMachine>();
        stateMachine.logic = this;
        stateMachine.ChangeState(new Sex_IdleState(this, stateMachine));


        Debug.Log("BEFORE COROUTINE CHECK");
        if (isCoroutineRunning)
            return;
        Debug.Log("AFTER COROUTINE CHECK");
        this.gameObject.SetActive(true);
        isSexyTimeGoingOn = true;

        StartCoroutine(StartSexyTimeCoroutine());
        isCoroutineRunning = true;
    }

    private IEnumerator StartSexyTimeCoroutine()
    {
        yield return new WaitForSeconds(0.75f);

        if (canvasBackground != null)
            canvasBackground.SetActive(true);

        if (goToPlayerPosition)
            this.gameObject.transform.position = GameObject.FindGameObjectWithTag("Player").transform.position + offsetForPosition;
        else
        {
            this.gameObject.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane)) + offsetForPosition;
            this.gameObject.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

    }



    private void OnEndDialogue()
    {
        shouldPause = false;
    }

    private void CastDeepBreathe(InputAction.CallbackContext context)
    {
        if (Time.time >= deepBreatheTimestamp && !shouldPause)
        {
            blueBar.value -= blueBarValueDeplete;
            deepBreatheTimestamp = Time.time + deepBreatheCooldown;
        }
    }



    private void CastNPCAttackBarFill()
    {
        if (Time.time >= npcAttackBarFillTimestamp && !shouldPause)
        {
            float reduction = playerStats.GetResilienceReductionMultiplier(); // or targetStats, depending who’s getting hit
            blueBar.value += npcAttackBarFillValue * reduction;
            npcAttackBarFillTimestamp = Time.time + npcAttackBarFillCooldown;
        }
    }

    //private float GetSpeed()
    //{
    //    float speed = 1f;

    //    if (timeBetweenStrokes >= 0.5f)
    //    {
    //        speed = 1f;
    //    }
    //    else if (timeBetweenStrokes >= 0.25f && timeBetweenStrokes < 0.5f)
    //    {
    //        speed = 1.25f;
    //    }
    //    else if (timeBetweenStrokes >= 0.15f && timeBetweenStrokes < 0.25f)
    //    {
    //        speed = 1.5f;
    //    }
    //    else if (timeBetweenStrokes >= 0.05f && timeBetweenStrokes < 0.15f)
    //    {
    //        speed = 2f;
    //    }

    //    return speed;
    //}

    //public void OpenSexyIfEnoughGold(int goldToSpend)
    //{
    //    if (StatsManager.Instance.gold >= goldToSpend)
    //    {
    //        StatsManager.Instance.gold -= goldToSpend;
    //        this.gameObject.SetActive(true);
    //    }
    //}

    public void ResetSexyTime()
    {
        // Reset bars
        //blueBarMaxValue = 0f;
        //pinkBarMaxValue = 0f;
        blueBar.value = 0f;
        pinkBar.value = 0f;


        npcAttackBarFillValue = 0f;
        npcAttackBarFillCooldown = 0f;
        // Reset flags
        cumReached = false;
        cumTimeElapsed = 0f;
        isFucking = false;
        isCoroutineRunning = false;
        isSexyTimeGoingOn = false;

       

        // Reset animation or state
        anim.Play("idle"); // Or whatever your default anim is

        // Reset background
        //if (canvasBackground != null)
        //    canvasBackground.SetActive(false);

        this.gameObject.SetActive(false);
        isSexyTimeGoingOn = false;

        // Optional: Reset animation
        if (anim != null)
            anim.Play("idle", 0, 0f);

        Debug.Log("SexyTime Reset Complete");

        //StartCoroutine(RestartSexyTimeAfterDelay(2f));
    }

    //private IEnumerator RestartSexyTimeAfterDelay(float delay)
    //{
    //    yield return new WaitForSeconds(delay);
    //    StartSexyTime();
    //}
}

