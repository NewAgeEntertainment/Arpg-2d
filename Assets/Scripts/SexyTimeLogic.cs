using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using rewired = Rewired;

public class SexyTimeLogic : MonoBehaviour
{

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

    [SerializeField] private Entity_Stats playerStats; // Assign in Inspector or via code
    [SerializeField] private Entity_Stats partnerStats; // Optional: the receiver of the stroking
    

    public float barSpeed = 0f;

    [SerializeField] private ParticleSystem strokeEffect;
    [SerializeField] private TextMeshProUGUI critText;

    [Header("Bars")]
    [SerializeField] private Slider pinkBar, blueBar;
    [SerializeField] private float pinkBarMaxValue = 100f;
    [SerializeField] private float blueBarMaxValue = 100f;
    private float blueBarFillPerStroke;
    private float pinkBarFillPerStroke;
    [SerializeField] private float barDecayPerSecond = 10f;

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
    [SerializeField] private float cumDuration = 5f;

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
    [SerializeField] private UnityEvent OnBlueBarFull = new UnityEvent();
    [SerializeField] private UnityEvent OnPinkBarFull = new UnityEvent();

    public static bool isSexyTimeGoingOn = false;

    private Animator anim;

    private bool isFucking, isMouthOpen, isCumming;
    private bool cachedStrokeWhileFucking = false;
    private bool cumReached = false;
    private bool isDialogueAlreadyTriggered = false;
    private bool shouldPause = false;

    private float lastStrokeTimestamp;
    private float timeBetweenStrokes = 1f;
    private float cumTimeElapsed;
    private float deepBreatheTimestamp;
    private float npcAttackBarFillTimestamp;
    //private CharacterActionsInputs playerInputActions;

    public bool HasSexyTimeFinished => cumTimeElapsed >= cumDuration;

    // Start is called before the first frame update
    void Start()
    {

        
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
        if (player.GetButtonDown("Stroke"))
            StrokeLogic(default);

        if (player.GetButtonDown("DeepBreathe"))
            CastDeepBreathe(default);

        CastNPCAttackBarFill();

        // Calculate remaining cooldown time
        float remainingCooldown = Mathf.Max(0, npcAttackBarFillTimestamp - Time.time);

        // Update TextMeshProUGUI with remaining cooldown time
        npcAttackBarFillCooldownText.text = remainingCooldown.ToString("F1"); // Displaying with one decimal place


        blueBarFillText.text = $"{blueBar.value.ToString("F0")}/{blueBar.maxValue.ToString("F0")}";
        pinkBarFillText.text = $"{pinkBar.value.ToString("F0")}/{pinkBar.maxValue.ToString("F0")}";

        blueBarFillPowerText.text = $"+{playerStats.GetBlueBarStrokeValue().ToString("F0")}";
        pinkBarFillPowerText.text = $"+{playerStats.GetPinkBarStrokeValue().ToString("F0")}";

        if (shouldPause)
            return;

        if (cumReached)
        {
            cumTimeElapsed += Time.deltaTime;

            if (cumTimeElapsed >= cumDuration)
            {
                // StatsManager.Instance.AddSexExp(sexExpToGive);
                // unlockedSO.UnlockAnim(animID);
                ResetSexyTime();
            }
        }

        if (isFucking)
        {
            if (Input.GetKeyDown(KeyCode.Space)) // Example stroke input
            {
                bool isCrit = false;
                float strokePower = playerStats != null ? playerStats.GetSexualStrokeDamage(out isCrit) : 10f;
                float resilience = partnerStats != null ? partnerStats.GetSexualResistance() : 0f;

                // Clamp resilience to a max of 50 just in case it's not already
                resilience = Mathf.Clamp(resilience, 0f, 50f);

                // Convert Resilience to a damage reduction factor (50% resilience = 0.5 reduction)
                float reductionFactor = 1f - (resilience / 100f);

                float adjustedStroke = strokePower * reductionFactor;

                if (isCrit)
                {
                    // Flash visual, sound, or screen shake for feedback
                    Debug.Log("<color=magenta>💥 Critical Stroke!</color>");
                    strokeEffect.Play(); // If you have a particle/sound
                                         // You can also briefly double barSpeed or trigger a climax threshold
                }

                pinkBar.value += adjustedStroke * Time.deltaTime;

                // Cache for transition detection
                cachedStrokeWhileFucking = true;
                Debug.Log($"StrokePower: {strokePower} | Resilience: {resilience}% | Final Gain: {adjustedStroke}");

            }
        }

        DepleteBars();
    }

    //public void SetBlueBarFill(float value)
    //{
    //    blueBarFillPerStroke = value;
    //}


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

    private void StrokeLogic(InputAction.CallbackContext context)
    {
        if (!cumReached && !shouldPause)
        {
            timeBetweenStrokes = Time.time - lastStrokeTimestamp;
            Debug.Log(timeBetweenStrokes);
            if (!isFucking)
            {
                InitiateStroke();
            }
            else
            {
                cachedStrokeWhileFucking = true;
            }

            lastStrokeTimestamp = Time.time;

        }
    }

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

        if (cachedStrokeWhileFucking)
        {
            InitiateStroke();

            cachedStrokeWhileFucking = false;
        }
        else
        {
            anim.Play("idle", 0, 1f);
        }
    }

    bool pinkBarReachedOnce = false;
    bool blueBarReachedOnce = false;

    private void InitiateStroke()
    {
        if (!isFucking && !cumReached)
        {
            anim.SetFloat("speed", GetSpeed());

            if (!isMouthOpen)
            {
                anim.Play("fuck", 0, 0f);
            }
            else
            {
                anim.Play("fuck", 0, 0f);
            }

            // Get stroke values from stats
            float blueGain = playerStats != null ? playerStats.GetBlueBarStrokeValue() : 0f;
            float pinkGain = playerStats != null ? playerStats.GetPinkBarStrokeValue() : 0f;

            blueBar.value += blueGain;
            pinkBar.value += pinkGain;

            if (blueBar.value >= blueBarMaxValue || pinkBar.value >= pinkBarMaxValue)
            {
                anim.Play("sperm shot", 0, 0f);
                cumReached = true;

                //if (SoundManager.instance != null && cummingSound != null)
                //    SoundManager.instance.PlaySound(cummingSound);
            }

            if (blueBar.value >= blueBarMaxValue && !blueBarReachedOnce)
            {
                blueBarReachedOnce = true;
                Debug.Log("Blue BAR FULL QUEST");
                OnBlueBarFull?.Invoke();
            }

            if (pinkBar.value >= pinkBarMaxValue && !pinkBarReachedOnce)
            {
                pinkBarReachedOnce = true;
                Debug.Log("Pink BAR FULL QUEST");
                OnPinkBarFull?.Invoke();
            }

            isFucking = true;
        }
    }

    bool isCoroutineRunning = false;
    public void StartSexyTime()
    {
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

    private float GetSpeed()
    {
        float speed = 1f;

        if (timeBetweenStrokes >= 0.5f)
        {
            speed = 1f;
        }
        else if (timeBetweenStrokes >= 0.25f && timeBetweenStrokes < 0.5f)
        {
            speed = 1.25f;
        }
        else if (timeBetweenStrokes >= 0.15f && timeBetweenStrokes < 0.25f)
        {
            speed = 1.5f;
        }
        else if (timeBetweenStrokes >= 0.05f && timeBetweenStrokes < 0.15f)
        {
            speed = 2f;
        }

        return speed;
    }

    //public void OpenSexyIfEnoughGold(int goldToSpend)
    //{
    //    if (StatsManager.Instance.gold >= goldToSpend)
    //    {
    //        StatsManager.Instance.gold -= goldToSpend;
    //        this.gameObject.SetActive(true);
    //    }
    //}

    private void ResetSexyTime()
    {
        // Reset bars
        blueBar.value = 0f;
        pinkBar.value = 0f;

        // Reset flags
        isFucking = false;
        isMouthOpen = false;
        isCumming = false;
        cachedStrokeWhileFucking = false;
        cumReached = false;
        isDialogueAlreadyTriggered = false;
        shouldPause = false;
        isCoroutineRunning = false;
        blueBarReachedOnce = false;
        pinkBarReachedOnce = false;
        cumTimeElapsed = 0f;
        
        // Reset background
        //if (canvasBackground != null)
        //    canvasBackground.SetActive(false);

        this.gameObject.SetActive(false);
        isSexyTimeGoingOn = false;

        // Optional: Reset animation
        if (anim != null)
            anim.Play("idle", 0, 0f);

        Debug.Log("SexyTime Reset Complete");
    }
}

