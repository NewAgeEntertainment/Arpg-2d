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

    [Header("Bars")]
    [SerializeField] private Slider pinkBar, blueBar;
    [SerializeField] private float pinkBarMaxValue = 100f;
    [SerializeField] private float blueBarMaxValue = 100f;
    [SerializeField] private float blueBarFillPerStroke = 3f;
    [SerializeField] private float pinkBarFillPerStroke = 3f;
    [SerializeField] private float barDecayPerSecond = 10f;

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

        blueBarFillPowerText.text = $"+{blueBarFillPerStroke.ToString("F0")}";
        pinkBarFillPowerText.text = $"+{pinkBarFillPerStroke.ToString("F0")}";

        if (shouldPause)
            return;

        if (cumReached)
        {
            cumTimeElapsed += Time.deltaTime;

            if (cumTimeElapsed >= cumDuration)
            {
                this.gameObject.SetActive(false);
                if (canvasBackground != null)
                    canvasBackground.SetActive(false);
                isSexyTimeGoingOn = false;
                isCoroutineRunning = false;
                cumReached = false;
                // StatsManager.Instance.AddSexExp(sexExpToGive);
                //unlockedSO.UnlockAnim(animID);
            }
        }

        DepleteBars();
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

            //if (SoundManager.instance != null && strokeSound != null)
            //    SoundManager.instance.PlaySound(strokeSound);

            if (!isMouthOpen)
            {
                anim.Play("fuck", 0, 0f);
            }
            else
            {
                anim.Play("fuck", 0, 0f);
            }

            blueBar.value += blueBarFillPerStroke; //+ StatsManager.Instance.stroke - StatsManager.Instance.resilience;
            pinkBar.value += pinkBarFillPerStroke; //+ StatsManager.Instance.stroke;

            

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
                if (OnBlueBarFull != null)
                    OnBlueBarFull.Invoke();
            }


            if (pinkBar.value >= pinkBarMaxValue && !pinkBarReachedOnce)
            {
                pinkBarReachedOnce = true;
                Debug.Log("Pink BAR FULL QUEST");
                if (OnPinkBarFull != null)
                    OnPinkBarFull.Invoke();
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
            blueBar.value += npcAttackBarFillValue;
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

    
}

