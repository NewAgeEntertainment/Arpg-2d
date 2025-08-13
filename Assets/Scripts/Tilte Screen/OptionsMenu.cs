// Assets/Scripts/Title/OptionsMenu.cs
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Basic Settings")]
    [SerializeField] private Slider masterVolume;  // 0..1
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button backButton;

    private TitleMenuManager title;

    private void Awake()
    {
        title = GetComponentInParent<TitleMenuManager>(true);

        if (controlsButton != null) controlsButton.onClick.AddListener(OpenControls);
        if (backButton != null) backButton.onClick.AddListener(CloseOptions);

        // Init from PlayerPrefs (basic example)
        if (masterVolume != null)
        {
            var vol = PlayerPrefs.GetFloat("opt_masterVol", 1f);
            masterVolume.value = vol;
            ApplyVolume(vol);
            masterVolume.onValueChanged.AddListener(ApplyVolume);
        }

        if (fullscreenToggle != null)
        {
            var fs = PlayerPrefs.GetInt("opt_fullscreen", 1) == 1;
            fullscreenToggle.isOn = fs;
            ApplyFullscreen(fs);
            fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);
        }
    }

    private void ApplyVolume(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("opt_masterVol", AudioListener.volume);
    }

    private void ApplyFullscreen(bool isFS)
    {
        Screen.fullScreen = isFS;
        PlayerPrefs.SetInt("opt_fullscreen", isFS ? 1 : 0);
    }

    private void OpenControls() => title?.OpenControls();
    private void CloseOptions() => title?.CloseOptions();
}
