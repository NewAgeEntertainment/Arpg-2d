using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Reflection;
using Rewired;
using UnityEngine.Audio;
using TMPro;

#if REWIRED
using Rewired.UI.ControlMapper;
#endif

public class UI_Options : MonoBehaviour
{
    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    private Rewired.Player rPlayer;

    [Header("Option Toggles")]
    [SerializeField] private Toggle healthBarToggle;
    [SerializeField] private Toggle manaBarToggle;

    [Header("Input Mode Switch")]
    [Tooltip("OFF = Keyboard, ON = Controller")]
    [SerializeField] private Toggle controllerModeToggle;

    [Tooltip("Optional text that displays the current input mode.")]
    [SerializeField] private TextMeshProUGUI inputModeText;

    [Header("Audio Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Apply Button")]
    [SerializeField] private Button applyButton;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    [Header("Saved Audio Pref Keys")]
    [SerializeField] private string bgmPrefsKey = "Options_BGMVolume";
    [SerializeField] private string sfxPrefsKey = "Options_SFXVolume";

    [Header("Toggle Pref Keys")]
    [SerializeField] private string healthBarPrefsKey = "Options_ShowHealthBar";
    [SerializeField] private string manaBarPrefsKey = "Options_ShowManaBar";

    [Header("Controls (Rewired Control Mapper)")]
    [SerializeField] private Button controlsButton;

#if REWIRED
    [Tooltip("Existing ControlMapper in scene optional.")]
    [SerializeField] private ControlMapper controlMapper;

    [Tooltip("Prefab with ControlMapper used if none found in scene.")]
    [SerializeField] private GameObject controlMapperPrefab;

    [Tooltip("UI element to reselect when Options is re-shown.")]
    [SerializeField] private Selectable defaultSelectable;
#endif

    [Header("Behaviour")]
    [Tooltip("In-game: after mapper closes, return to UI main menu instead of back to Options.")]
    [SerializeField] private bool returnToUIMenuWhenMapperCloses = true;

    private CanvasGroup cg;
    private Player player;
    private TitleMenuManager titleMenuManagerScene;

    private const float MinLinearVolume = 0.0001f;
    private const float MinMixerDb = -80f;
    private const float MaxMixerDb = 0f;

    private float appliedBgm = 0.75f;
    private float appliedSfx = 0.75f;
    private bool appliedHealthBar = true;
    private bool appliedManaBar = true;

    private float pendingBgm = 0.75f;
    private float pendingSfx = 0.75f;
    private bool pendingHealthBar = true;
    private bool pendingManaBar = true;

    private bool suppressCallbacks = false;

    // Backup polling for the Keyboard/Controller toggle.
    private bool inputToggleInitialized = false;
    private bool lastControllerToggleValue = false;

    private void Awake()
    {
        player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (healthBarToggle != null)
            healthBarToggle.onValueChanged.AddListener(OnHealthToggleChanged);

        if (manaBarToggle != null)
            manaBarToggle.onValueChanged.AddListener(OnManaToggleChanged);

        ConnectInputModeToggle();

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OnClickOpenControlMapper);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyChanges);

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        LoadAppliedSettings();
        ResetPendingToApplied();
        RefreshControlsFromPending();
        RefreshInputModeText();
        UpdateApplyButtonState();
    }

    private void Start()
    {
        TryCacheRewired();
        CacheTitleMenuManager();
        RefreshInputModeText();
    }

    private void OnEnable()
    {
#if REWIRED
        HookMapperEvents(true);
#endif

        CacheTitleMenuManager();
        ConnectInputModeToggle();

        ResetPendingToApplied();
        RefreshControlsFromPending();
        RefreshInputModeText();
        UpdateApplyButtonState();
    }

    private void OnDisable()
    {
#if REWIRED
        HookMapperEvents(false);
#endif
    }

    private void Update()
    {
        if (rPlayer == null)
            TryCacheRewired();

        if (rPlayer != null && rPlayer.GetButtonDown(cancelAction))
            HandleCancel();

        PollInputModeToggle();
    }

    public void OpenOptions()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        BringToFront();
        SetInteractable(true);

        ConnectInputModeToggle();

        ResetPendingToApplied();
        RefreshControlsFromPending();

        if (InputDeviceModeManager.Instance != null)
            InputDeviceModeManager.Instance.LoadMode();
        else
            Debug.LogWarning("[UI_Options] InputDeviceModeManager is missing.");

        RefreshInputModeText();
        RefreshSkillSlotLabels();
        UpdateApplyButtonState();

        Debug.Log("[UI_Options] Options panel opened.");
    }

    public bool HandleCancel()
    {
        RevertPendingChanges();

        if (IsTitleScreenContext())
        {
            ReturnToTitleMainAndCloseSelf();
            return true;
        }

        if (UI.Instance != null)
            UI.Instance.CloseOptions();
        else
            ClosePanel();

        return true;
    }

    public void ClosePanel()
    {
        RevertPendingChanges();
        SetInteractable(false);
        gameObject.SetActive(false);
        Debug.Log("[UI_Options] Closed.");
    }

    public bool IsOpen
    {
        get
        {
            if (!gameObject.activeInHierarchy)
                return false;

            return cg == null ? true : cg.blocksRaycasts;
        }
    }

    private void ConnectInputModeToggle()
    {
        if (controllerModeToggle == null)
        {
            Debug.LogWarning("[UI_Options] controllerModeToggle is NOT assigned in Inspector.");
            return;
        }

        controllerModeToggle.onValueChanged.RemoveListener(OnControllerModeToggleChanged);
        controllerModeToggle.onValueChanged.AddListener(OnControllerModeToggleChanged);

        lastControllerToggleValue = controllerModeToggle.isOn;
        inputToggleInitialized = true;

        Debug.Log($"[UI_Options] Controller mode toggle listener connected to: {controllerModeToggle.name}");
    }

    private void PollInputModeToggle()
    {
        if (controllerModeToggle == null)
            return;

        bool currentValue = controllerModeToggle.isOn;

        if (!inputToggleInitialized)
        {
            lastControllerToggleValue = currentValue;
            inputToggleInitialized = true;
            return;
        }

        if (currentValue == lastControllerToggleValue)
            return;

        lastControllerToggleValue = currentValue;

        Debug.Log($"[UI_Options] Poll detected input toggle changed: {currentValue}");

        ApplyInputModeFromToggle(currentValue);
    }

    public void OnControllerModeToggleChanged(bool controllerMode)
    {
        Debug.Log($"[Options Toggle] Controller Mode Toggle Changed: {controllerMode}");

        lastControllerToggleValue = controllerMode;
        inputToggleInitialized = true;

        ApplyInputModeFromToggle(controllerMode);
    }

    private void ApplyInputModeFromToggle(bool controllerMode)
    {
        if (InputDeviceModeManager.Instance == null)
        {
            Debug.LogWarning("[UI_Options] No InputDeviceModeManager found in scene.");
            return;
        }

        if (controllerMode)
            InputDeviceModeManager.Instance.SetControllerMode();
        else
            InputDeviceModeManager.Instance.SetKeyboardMode();

        if (inputModeText != null)
            inputModeText.text = controllerMode ? "Input: Controller" : "Input: Keyboard";

        if (controllerModeToggle != null && controllerModeToggle.graphic != null)
            controllerModeToggle.graphic.gameObject.SetActive(controllerMode);

        RefreshSkillSlotLabels();

        Debug.Log($"[UI_Options] InputDeviceManager changed to: {InputDeviceModeManager.Instance.CurrentMode}");
    }

    private void RefreshInputModeText()
    {
        if (InputDeviceModeManager.Instance == null)
        {
            if (inputModeText != null)
                inputModeText.text = "Input: Unknown";

            Debug.LogWarning("[UI_Options] RefreshInputModeText failed: InputDeviceModeManager missing.");
            return;
        }

        bool isController = InputDeviceModeManager.Instance.IsController;

        if (inputModeText != null)
            inputModeText.text = isController ? "Input: Controller" : "Input: Keyboard";

        if (controllerModeToggle != null)
        {
            suppressCallbacks = true;

            controllerModeToggle.SetIsOnWithoutNotify(isController);

            lastControllerToggleValue = isController;
            inputToggleInitialized = true;

            if (controllerModeToggle.graphic != null)
                controllerModeToggle.graphic.gameObject.SetActive(isController);

            suppressCallbacks = false;
        }

        Debug.Log($"[UI_Options] Input switch synced. Mode={(isController ? "Controller" : "Keyboard")}, ToggleIsOn={(controllerModeToggle != null && controllerModeToggle.isOn)}");
    }

    private void RefreshSkillSlotLabels()
    {
        if (UI.Instance != null && UI.Instance.inGameUI != null)
            UI.Instance.inGameUI.RefreshAllSkillSlotLabels();

        SexyTimeUIController sexUI =
            FindFirstObjectByType<SexyTimeUIController>(FindObjectsInactive.Include);

        if (sexUI == null || sexUI.Hotbar == null || sexUI.Hotbar.Slots == null)
            return;

        Player playerRef = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (playerRef == null || playerRef.skillManager == null)
            return;

        foreach (UI_SkillSlot slot in sexUI.Hotbar.Slots)
        {
            if (slot != null)
                slot.RefreshBindingLabel(playerRef.skillManager);
        }
    }

    private void OnHealthToggleChanged(bool isOn)
    {
        if (suppressCallbacks)
            return;

        pendingHealthBar = isOn;

        if (player != null && player.health != null)
            player.health.EnableHealthBar(isOn);

        UpdateApplyButtonState();
    }

    private void OnManaToggleChanged(bool isOn)
    {
        if (suppressCallbacks)
            return;

        pendingManaBar = isOn;

        if (player != null && player.mana != null)
            player.mana.EnableManaBar(isOn);

        UpdateApplyButtonState();
    }

    private void OnBgmSliderChanged(float value)
    {
        if (suppressCallbacks)
            return;

        pendingBgm = value;

        if (AudioManager.instance != null)
            AudioManager.instance.SetBgmVolume(value, saveToPrefs: false);
        else
            SetMixerVolume(bgmVolumeParameter, value);

        UpdateApplyButtonState();
    }

    private void OnSfxSliderChanged(float value)
    {
        if (suppressCallbacks)
            return;

        pendingSfx = value;

        if (AudioManager.instance != null)
            AudioManager.instance.SetSfxVolume(value, saveToPrefs: false);
        else
            SetMixerVolume(sfxVolumeParameter, value);

        UpdateApplyButtonState();
    }

    public void ApplyChanges()
    {
        appliedBgm = pendingBgm;
        appliedSfx = pendingSfx;
        appliedHealthBar = pendingHealthBar;
        appliedManaBar = pendingManaBar;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBgmVolume(appliedBgm, saveToPrefs: true);
            AudioManager.instance.SetSfxVolume(appliedSfx, saveToPrefs: true);
        }
        else
        {
            SetMixerVolume(bgmVolumeParameter, appliedBgm);
            SetMixerVolume(sfxVolumeParameter, appliedSfx);

            PlayerPrefs.SetFloat(bgmPrefsKey, appliedBgm);
            PlayerPrefs.SetFloat(sfxPrefsKey, appliedSfx);
        }

        PlayerPrefs.SetInt(healthBarPrefsKey, appliedHealthBar ? 1 : 0);
        PlayerPrefs.SetInt(manaBarPrefsKey, appliedManaBar ? 1 : 0);
        PlayerPrefs.Save();

        ApplyGameplayToggles(appliedHealthBar, appliedManaBar);
        UpdateApplyButtonState();

        Debug.Log("[UI_Options] Changes applied.");
    }

    private void RevertPendingChanges()
    {
        ResetPendingToApplied();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBgmVolume(appliedBgm, saveToPrefs: false);
            AudioManager.instance.SetSfxVolume(appliedSfx, saveToPrefs: false);
        }
        else
        {
            SetMixerVolume(bgmVolumeParameter, appliedBgm);
            SetMixerVolume(sfxVolumeParameter, appliedSfx);
        }

        ApplyGameplayToggles(appliedHealthBar, appliedManaBar);
        RefreshControlsFromPending();
        RefreshInputModeText();
        UpdateApplyButtonState();
    }

    private void LoadAppliedSettings()
    {
        appliedBgm = PlayerPrefs.GetFloat(bgmPrefsKey, 0.75f);
        appliedSfx = PlayerPrefs.GetFloat(sfxPrefsKey, 0.75f);
        appliedHealthBar = PlayerPrefs.GetInt(healthBarPrefsKey, 1) == 1;
        appliedManaBar = PlayerPrefs.GetInt(manaBarPrefsKey, 1) == 1;

        ApplyGameplayToggles(appliedHealthBar, appliedManaBar);
    }

    private void ResetPendingToApplied()
    {
        pendingBgm = appliedBgm;
        pendingSfx = appliedSfx;
        pendingHealthBar = appliedHealthBar;
        pendingManaBar = appliedManaBar;
    }

    private void RefreshControlsFromPending()
    {
        suppressCallbacks = true;

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(pendingBgm);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(pendingSfx);

        if (healthBarToggle != null)
            healthBarToggle.SetIsOnWithoutNotify(pendingHealthBar);

        if (manaBarToggle != null)
            manaBarToggle.SetIsOnWithoutNotify(pendingManaBar);

        suppressCallbacks = false;
    }

    private void ApplyGameplayToggles(bool healthOn, bool manaOn)
    {
        if (player != null && player.health != null)
            player.health.EnableHealthBar(healthOn);

        if (player != null && player.mana != null)
            player.mana.EnableManaBar(manaOn);
    }

    private void UpdateApplyButtonState()
    {
        if (applyButton == null)
            return;

        bool hasChanges =
            !Mathf.Approximately(pendingBgm, appliedBgm) ||
            !Mathf.Approximately(pendingSfx, appliedSfx) ||
            pendingHealthBar != appliedHealthBar ||
            pendingManaBar != appliedManaBar;

        applyButton.interactable = hasChanges;
    }

    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        float clamped = Mathf.Clamp(sliderValue, 0f, 1f);

        if (clamped <= 0f)
        {
            audioMixer.SetFloat(parameterName, MinMixerDb);
            return;
        }

        float db = Mathf.Log10(Mathf.Max(clamped, MinLinearVolume)) * 20f;
        db = Mathf.Clamp(db, MinMixerDb, MaxMixerDb);

        audioMixer.SetFloat(parameterName, db);
    }

    private void OnClickOpenControlMapper()
    {
#if !REWIRED
        Debug.LogError("[UI_Options] Rewired not present. Cannot open Control Mapper.");
        return;
#else
        ControlMapper cm = EnsureControlMapper();

        if (cm == null)
        {
            Debug.LogError("[UI_Options] ControlMapper not found and no prefab assigned.");
            return;
        }

        HideOptionsForMapper(true);

        try
        {
            cm.Open();
        }
        catch
        {
            MethodInfo m = cm.GetType().GetMethod(
                "Open",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (m != null)
                m.Invoke(cm, null);
            else
                cm.gameObject.SetActive(true);
        }
#endif
    }

#if REWIRED
    private ControlMapper EnsureControlMapper()
    {
        if (controlMapper != null)
            return controlMapper;

#if UNITY_2022_1_OR_NEWER
        controlMapper = FindFirstObjectByType<ControlMapper>(FindObjectsInactive.Include);
#else
        controlMapper = FindObjectOfType<ControlMapper>(true);
#endif

        if (controlMapper != null)
            return controlMapper;

        if (controlMapperPrefab != null)
        {
            GameObject go = Instantiate(controlMapperPrefab);
            controlMapper = go.GetComponentInChildren<ControlMapper>(true);
        }

        return controlMapper;
    }

    private void HookMapperEvents(bool hook)
    {
        ControlMapper cm = EnsureControlMapper();

        if (cm == null)
            return;

        if (hook)
        {
            cm.ScreenOpenedEvent += OnMapperOpened;
            cm.ScreenClosedEvent += OnMapperClosed;
        }
        else
        {
            cm.ScreenOpenedEvent -= OnMapperOpened;
            cm.ScreenClosedEvent -= OnMapperClosed;
        }
    }

    private void OnMapperOpened()
    {
        HideOptionsForMapper(true);
    }

    private void OnMapperClosed()
    {
        RefreshSkillSlotLabels();

        if (IsTitleScreenContext())
        {
            ReturnToTitleMainAndCloseSelf();
            return;
        }

        if (returnToUIMenuWhenMapperCloses)
        {
            UI.Instance?.CloseOptions();
        }
        else
        {
            HideOptionsForMapper(false);
            BringToFront();

            if (defaultSelectable != null && EventSystem.current != null)
                StartCoroutine(SelectNextFrame(defaultSelectable.gameObject));
        }
    }
#endif

    private void TryCacheRewired()
    {
        try
        {
            rPlayer = ReInput.players.GetPlayer(playerID);
        }
        catch
        {
            rPlayer = null;
        }
    }

    private void CacheTitleMenuManager()
    {
#if UNITY_2022_1_OR_NEWER
        titleMenuManagerScene = FindFirstObjectByType<TitleMenuManager>(FindObjectsInactive.Include);
#else
        titleMenuManagerScene = FindObjectOfType<TitleMenuManager>(true);
#endif
    }

    private bool IsTitleScreenContext()
    {
        return titleMenuManagerScene != null;
    }

    private void ReturnToTitleMainAndCloseSelf()
    {
        HideOptionsForMapper(true);

        if (titleMenuManagerScene != null)
            titleMenuManagerScene.CloseAllOptionPanels();

        gameObject.SetActive(false);
    }

    private void HideOptionsForMapper(bool hide)
    {
        if (cg != null)
        {
            cg.alpha = hide ? 0f : 1f;
            cg.interactable = !hide;
            cg.blocksRaycasts = !hide;
        }
        else
        {
            gameObject.SetActive(!hide);
        }
    }

    private void SetInteractable(bool on)
    {
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = on;
            cg.blocksRaycasts = on;
        }
    }

    private void BringToFront()
    {
        transform.SetAsLastSibling();
    }

    private IEnumerator SelectNextFrame(GameObject go)
    {
        yield return null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(go);
    }
}