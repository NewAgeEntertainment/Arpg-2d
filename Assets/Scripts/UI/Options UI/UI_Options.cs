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

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    [Header("Saved Audio Pref Keys")]
    [SerializeField] private string bgmPrefsKey = "Options_BGMVolume";
    [SerializeField] private string sfxPrefsKey = "Options_SFXVolume";

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

    private void Awake()
    {
        player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (healthBarToggle != null) healthBarToggle.onValueChanged.AddListener(OnHealthToggleChanged);
        if (manaBarToggle != null) manaBarToggle.onValueChanged.AddListener(OnManaToggleChanged);
        if (controlsButton != null) controlsButton.onClick.AddListener(OnClickOpenControlMapper);

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

        LoadAudioSettings();
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
        Debug.Log("[UI_Options] Options panel opened.");
    }

    public bool HandleCancel()
    {
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
        SetInteractable(false);
        gameObject.SetActive(false);
        Debug.Log("[UI_Options] Closed.");
    }

    private void OnHealthToggleChanged(bool isOn)
    {
        if (player != null && player.health != null)
            player.health.EnableHealthBar(isOn);
    }

    private void OnManaToggleChanged(bool isOn)
    {
        if (player != null && player.mana != null)
            player.mana.EnableManaBar(isOn);
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
        SetMixerVolume(bgmVolumeParameter, value);
        PlayerPrefs.SetFloat(bgmPrefsKey, value);
        PlayerPrefs.Save();
    }

    private void OnSfxSliderChanged(float value)
    {
        SetMixerVolume(sfxVolumeParameter, value);
        PlayerPrefs.SetFloat(sfxPrefsKey, value);
        PlayerPrefs.Save();
    }

    private void LoadAudioSettings()
    {
        float savedBgm = PlayerPrefs.GetFloat(bgmPrefsKey, 0.75f);
        float savedSfx = PlayerPrefs.GetFloat(sfxPrefsKey, 0.75f);

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(savedBgm);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(savedSfx);

        SetMixerVolume(bgmVolumeParameter, savedBgm);
        SetMixerVolume(sfxVolumeParameter, savedSfx);
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