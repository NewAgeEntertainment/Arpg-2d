using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Reflection;
using Rewired;
using UnityEngine.Audio;

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
    [Tooltip("Existing ControlMapper in scene (optional).")]
    [SerializeField] private ControlMapper controlMapper;

    [Tooltip("Prefab with ControlMapper (used if none found in scene).")]
    [SerializeField] private GameObject controlMapperPrefab;

    [Tooltip("UI element to reselect when Options is re-shown (Title screen path).")]
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

    // Applied values (the currently saved/committed settings)
    private float appliedBgm = 0.75f;
    private float appliedSfx = 0.75f;
    private bool appliedHealthBar = true;
    private bool appliedManaBar = true;

    // Pending values (what the user is changing right now)
    private float pendingBgm = 0.75f;
    private float pendingSfx = 0.75f;
    private bool pendingHealthBar = true;
    private bool pendingManaBar = true;

    private bool suppressCallbacks = false;

    private void Awake()
    {
        player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (healthBarToggle != null) healthBarToggle.onValueChanged.AddListener(OnHealthToggleChanged);
        if (manaBarToggle != null) manaBarToggle.onValueChanged.AddListener(OnManaToggleChanged);
        if (controlsButton != null) controlsButton.onClick.AddListener(OnClickOpenControlMapper);
        if (applyButton != null) applyButton.onClick.AddListener(ApplyChanges);

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
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        LoadAppliedSettings();
        ResetPendingToApplied();
        RefreshControlsFromPending();
        UpdateApplyButtonState();
    }

    private void Start()
    {
        TryCacheRewired();
        CacheTitleMenuManager();
    }

    private void OnEnable()
    {
#if REWIRED
        HookMapperEvents(true);
#endif
        CacheTitleMenuManager();

        // Every time options opens, start from last applied values
        ResetPendingToApplied();
        RefreshControlsFromPending();
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
        if (rPlayer == null) { TryCacheRewired(); return; }
        if (rPlayer.GetButtonDown(cancelAction)) HandleCancel();
    }

    public void OpenOptions()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        BringToFront();
        SetInteractable(true);

        ResetPendingToApplied();
        RefreshControlsFromPending();
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

    private void OnHealthToggleChanged(bool isOn)
    {
        if (suppressCallbacks) return;

        pendingHealthBar = isOn;

        // Preview immediately
        if (player != null && player.health != null)
            player.health.EnableHealthBar(isOn);

        UpdateApplyButtonState();
    }

    private void OnManaToggleChanged(bool isOn)
    {
        if (suppressCallbacks) return;

        pendingManaBar = isOn;

        // Preview immediately
        if (player != null && player.mana != null)
            player.mana.EnableManaBar(isOn);

        UpdateApplyButtonState();
    }

    public bool IsOpen
    {
        get
        {
            if (!gameObject.activeInHierarchy) return false;
            return cg == null ? true : cg.blocksRaycasts;
        }
    }

    private void OnBgmSliderChanged(float value)
    {
        if (suppressCallbacks) return;

        pendingBgm = value;

        // Preview immediately, but do not save yet
        if (AudioManager.instance != null)
            AudioManager.instance.SetBgmVolume(value, saveToPrefs: false);
        else
            SetMixerVolume(bgmVolumeParameter, value);

        UpdateApplyButtonState();
    }

    private void OnSfxSliderChanged(float value)
    {
        if (suppressCallbacks) return;

        pendingSfx = value;

        // Preview immediately, but do not save yet
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
        if (applyButton == null) return;

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
        var cm = EnsureControlMapper();
        if (cm == null)
        {
            Debug.LogError("[UI_Options] ControlMapper not found and no prefab assigned.");
            return;
        }

        HideOptionsForMapper(true);

        try { cm.Open(); }
        catch
        {
            var m = cm.GetType().GetMethod("Open", BindingFlags.Public | BindingFlags.Instance);
            if (m != null) m.Invoke(cm, null);
            else cm.gameObject.SetActive(true);
        }
#endif
    }

#if REWIRED
    private ControlMapper EnsureControlMapper()
    {
        if (controlMapper != null) return controlMapper;

#if UNITY_2022_1_OR_NEWER
        controlMapper = FindFirstObjectByType<ControlMapper>(FindObjectsInactive.Include);
#else
        controlMapper = FindObjectOfType<ControlMapper>(true);
#endif
        if (controlMapper != null) return controlMapper;

        if (controlMapperPrefab != null)
        {
            var go = Instantiate(controlMapperPrefab);
            controlMapper = go.GetComponentInChildren<ControlMapper>(true);
        }
        return controlMapper;
    }

    private void HookMapperEvents(bool hook)
    {
        var cm = EnsureControlMapper();
        if (cm == null) return;

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
        if (UI.Instance != null && UI.Instance.inGameUI != null)
        {
            UI.Instance.inGameUI.RefreshAllSkillSlotLabels();
        }

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
        try { rPlayer = ReInput.players.GetPlayer(playerID); } catch { }
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
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }
}