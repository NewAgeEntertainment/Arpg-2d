using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("References")]
    [SerializeField] private AudioDatabaseSO audioDB;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource globalSfxSource;

    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("BGM Settings")]
    [SerializeField] private float bgmFadeDuration = 1f;

    [Header("Audio Mixer Volume Params")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    [Header("Saved Audio Pref Keys")]
    [SerializeField] private string bgmPrefsKey = "Options_BGMVolume";
    [SerializeField] private string sfxPrefsKey = "Options_SFXVolume";

    private const float MinLinearVolume = 0.0001f;
    private const float MinMixerDb = -80f;
    private const float MaxMixerDb = 0f;

    private Transform player;

    private AudioClip lastMusicPlayed;
    private string currentBgmGroupName;
    private bool bgmShouldPlay;

    private Coroutine bgmRoutine;

    // Exact restore state:
    private string currentClipName;
    private float currentClipTime;
    private bool restorePendingAfterSceneLoad;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
            Debug.LogWarning("[AudioManager] BGM AudioSource is not assigned.");

        if (globalSfxSource == null)
            Debug.LogWarning("[AudioManager] Global SFX AudioSource is not assigned.");

        if (audioDB == null)
            Debug.LogWarning("[AudioManager] AudioDatabaseSO is not assigned.");

        if (audioMixer == null)
            Debug.LogWarning("[AudioManager] AudioMixer is not assigned.");

        RefreshAudioSetup();

        if (bgmSource != null)
            bgmSource.ignoreListenerPause = true;

        if (globalSfxSource != null)
            globalSfxSource.ignoreListenerPause = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        RefreshAudioSetup();
    }

    private void Update()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            currentClipTime = bgmSource.time;

            if (bgmSource.clip != null)
                currentClipName = bgmSource.clip.name;
        }

        if (!bgmShouldPlay)
            return;

        if (bgmSource == null || audioDB == null)
            return;

        if (!bgmSource.isPlaying && !string.IsNullOrEmpty(currentBgmGroupName) && bgmRoutine == null)
        {
            RestoreCurrentBGMExact();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAudioSetup();

        if (bgmSource != null)
            bgmSource.ignoreListenerPause = true;

        if (globalSfxSource != null)
            globalSfxSource.ignoreListenerPause = true;

        if (restorePendingAfterSceneLoad)
        {
            restorePendingAfterSceneLoad = false;
            RestoreCurrentBGMExact();
            return;
        }

        if (bgmShouldPlay && !string.IsNullOrEmpty(currentBgmGroupName))
        {
            if (bgmSource != null && !bgmSource.isPlaying && bgmRoutine == null)
            {
                RestoreCurrentBGMExact();
            }
        }
    }

    [ContextMenu("Debug Audio Routing")]
    private void DebugAudioRouting()
    {
        string bgmGroup = bgmSource != null && bgmSource.outputAudioMixerGroup != null
            ? bgmSource.outputAudioMixerGroup.name
            : "NULL";

        string sfxGroup = globalSfxSource != null && globalSfxSource.outputAudioMixerGroup != null
            ? globalSfxSource.outputAudioMixerGroup.name
            : "NULL";

        Debug.Log($"[AudioManager] BGM Source Group = {bgmGroup}");
        Debug.Log($"[AudioManager] SFX Source Group = {sfxGroup}");

        if (audioMixer != null)
        {
            if (audioMixer.GetFloat(bgmVolumeParameter, out float bgmDb))
                Debug.Log($"[AudioManager] {bgmVolumeParameter} = {bgmDb} dB");

            if (audioMixer.GetFloat(sfxVolumeParameter, out float sfxDb))
                Debug.Log($"[AudioManager] {sfxVolumeParameter} = {sfxDb} dB");
        }
    }

    private void RefreshAudioSetup()
    {
        ApplyMixerGroups();
        ApplySavedMixerVolumes();
    }

    private void ApplySavedMixerVolumes()
    {
        if (audioMixer == null)
            return;

        float savedBgm = PlayerPrefs.GetFloat(bgmPrefsKey, 0.75f);
        float savedSfx = PlayerPrefs.GetFloat(sfxPrefsKey, 0.75f);

        SetBgmVolume(savedBgm, saveToPrefs: false);
        SetSfxVolume(savedSfx, saveToPrefs: false);
    }

    public void SetBgmVolume(float sliderValue, bool saveToPrefs = true)
    {
        SetMixerVolume(bgmVolumeParameter, sliderValue);

        if (saveToPrefs)
        {
            PlayerPrefs.SetFloat(bgmPrefsKey, Mathf.Clamp01(sliderValue));
            PlayerPrefs.Save();
        }
    }

    public void SetSfxVolume(float sliderValue, bool saveToPrefs = true)
    {
        SetMixerVolume(sfxVolumeParameter, sliderValue);

        if (saveToPrefs)
        {
            PlayerPrefs.SetFloat(sfxPrefsKey, Mathf.Clamp01(sliderValue));
            PlayerPrefs.Save();
        }
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

    public void StartBGM(string musicGroup)
    {
        if (string.IsNullOrWhiteSpace(musicGroup))
        {
            Debug.LogWarning("[AudioManager] StartBGM called with an empty group name.");
            return;
        }

        if (audioDB == null || bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] Cannot start BGM. Missing AudioDatabaseSO or BGM AudioSource.");
            return;
        }

        if (musicGroup == currentBgmGroupName && bgmShouldPlay && bgmSource.isPlaying)
            return;

        bgmShouldPlay = true;
        currentBgmGroupName = musicGroup;
        currentClipName = null;
        currentClipTime = 0f;
        restorePendingAfterSceneLoad = false;

        NextBGM(musicGroup);
    }

    public void NextBGM(string musicGroup)
    {
        if (string.IsNullOrWhiteSpace(musicGroup))
        {
            Debug.LogWarning("[AudioManager] NextBGM called with an empty group name.");
            return;
        }

        if (audioDB == null || bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] Cannot switch BGM. Missing AudioDatabaseSO or BGM AudioSource.");
            return;
        }

        bgmShouldPlay = true;
        currentBgmGroupName = musicGroup;

        StopBgmRoutine();
        bgmRoutine = StartCoroutine(SwitchMusicCo(musicGroup));
    }

    public void StopBGM()
    {
        bgmShouldPlay = false;
        currentBgmGroupName = null;
        currentClipName = null;
        currentClipTime = 0f;
        restorePendingAfterSceneLoad = false;

        if (bgmSource == null)
            return;

        StopBgmRoutine();
        bgmRoutine = StartCoroutine(StopBGMCo());
    }

    private IEnumerator StopBGMCo()
    {
        yield return FadeVolumeCo(bgmSource, 0f, bgmFadeDuration);

        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        bgmRoutine = null;
    }

    private IEnumerator SwitchMusicCo(string musicGroup)
    {
        AudioClipData data = audioDB.Get(musicGroup);

        if (data == null || data.clips == null || data.clips.Count == 0)
        {
            Debug.LogWarning("[AudioManager] No audio found for group: " + musicGroup);
            bgmRoutine = null;
            yield break;
        }

        AudioClip nextMusic = data.GetRandomClip();

        if (nextMusic == null)
        {
            Debug.LogWarning("[AudioManager] Random clip returned null for group: " + musicGroup);
            bgmRoutine = null;
            yield break;
        }

        if (data.clips.Count > 1)
        {
            int safety = 0;
            while (nextMusic == lastMusicPlayed && safety < 10)
            {
                nextMusic = data.GetRandomClip();
                safety++;
            }
        }

        if (bgmSource.isPlaying)
        {
            yield return FadeVolumeCo(bgmSource, 0f, bgmFadeDuration);
            bgmSource.Stop();
        }

        if (!bgmShouldPlay)
        {
            bgmRoutine = null;
            yield break;
        }

        lastMusicPlayed = nextMusic;
        currentClipName = nextMusic.name;
        currentClipTime = 0f;

        bgmSource.clip = nextMusic;
        bgmSource.volume = 0f;
        bgmSource.time = 0f;
        bgmSource.Play();

        yield return FadeVolumeCo(bgmSource, data.maxVolume, bgmFadeDuration);

        bgmRoutine = null;
    }

    private void RestoreCurrentBGMExact()
    {
        if (!bgmShouldPlay)
            return;

        if (audioDB == null || bgmSource == null)
            return;

        if (string.IsNullOrWhiteSpace(currentBgmGroupName))
            return;

        AudioClipData data = audioDB.Get(currentBgmGroupName);
        if (data == null || data.clips == null || data.clips.Count == 0)
        {
            Debug.LogWarning("[AudioManager] Cannot restore BGM. Missing group: " + currentBgmGroupName);
            return;
        }

        AudioClip clipToRestore = FindClipByName(data, currentClipName);

        if (clipToRestore == null)
        {
            NextBGM(currentBgmGroupName);
            return;
        }

        StopBgmRoutine();
        bgmRoutine = StartCoroutine(RestoreMusicCo(data, clipToRestore, currentClipTime));
    }

    private IEnumerator RestoreMusicCo(AudioClipData data, AudioClip clipToRestore, float timeSeconds)
    {
        if (bgmSource == null || clipToRestore == null)
        {
            bgmRoutine = null;
            yield break;
        }

        if (bgmSource.isPlaying)
        {
            yield return FadeVolumeCo(bgmSource, 0f, bgmFadeDuration);
            bgmSource.Stop();
        }

        if (!bgmShouldPlay)
        {
            bgmRoutine = null;
            yield break;
        }

        bgmSource.clip = clipToRestore;
        bgmSource.volume = 0f;

        float safeTime = Mathf.Clamp(timeSeconds, 0f, Mathf.Max(0f, clipToRestore.length - 0.05f));
        bgmSource.time = safeTime;
        bgmSource.Play();

        lastMusicPlayed = clipToRestore;
        currentClipName = clipToRestore.name;
        currentClipTime = safeTime;

        yield return FadeVolumeCo(bgmSource, data.maxVolume, bgmFadeDuration);

        bgmRoutine = null;
    }

    private AudioClip FindClipByName(AudioClipData data, string clipName)
    {
        if (data == null || data.clips == null || string.IsNullOrWhiteSpace(clipName))
            return null;

        for (int i = 0; i < data.clips.Count; i++)
        {
            var clip = data.clips[i];
            if (clip != null && clip.name == clipName)
                return clip;
        }

        return null;
    }

    public void MarkBgmForRestoreAfterLoad()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            currentClipTime = bgmSource.time;

            if (bgmSource.clip != null)
                currentClipName = bgmSource.clip.name;
        }

        restorePendingAfterSceneLoad = bgmShouldPlay && !string.IsNullOrEmpty(currentBgmGroupName);
    }

    private IEnumerator FadeVolumeCo(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
            yield break;

        if (duration <= 0f)
        {
            source.volume = targetVolume;
            yield break;
        }

        float time = 0f;
        float startVolume = source.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    private void StopBgmRoutine()
    {
        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
            bgmRoutine = null;
        }
    }

    public void PlaySFX(string soundName, AudioSource source, float maxHearingDistance = 5f)
    {
        if (string.IsNullOrWhiteSpace(soundName))
        {
            Debug.LogWarning("[AudioManager] PlaySFX called with an empty sound name.");
            return;
        }

        if (audioDB == null)
        {
            Debug.LogWarning("[AudioManager] Cannot play SFX. AudioDatabaseSO is missing.");
            return;
        }

        if (source == null)
        {
            Debug.LogWarning("[AudioManager] PlaySFX called with a null AudioSource.");
            return;
        }

        if (player == null && Player.instance != null)
            player = Player.instance.transform;

        if (sfxMixerGroup != null && source.outputAudioMixerGroup != sfxMixerGroup)
            source.outputAudioMixerGroup = sfxMixerGroup;

        AudioClipData data = audioDB.Get(soundName);
        if (data == null)
        {
            Debug.LogWarning("[AudioManager] Attempted to play missing sound: " + soundName);
            return;
        }

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
            return;

        float maxVolume = data.maxVolume;
        float finalVolume = maxVolume;

        if (player != null && maxHearingDistance > 0f)
        {
            float distance = Vector2.Distance(source.transform.position, player.position);
            float t = Mathf.Clamp01(1f - (distance / maxHearingDistance));
            finalVolume = Mathf.Lerp(0f, maxVolume, t * t);
        }

        source.pitch = Random.Range(0.95f, 1.1f);
        source.PlayOneShot(clip, finalVolume);
    }

    public void PlayGlobalSFX(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName))
        {
            Debug.LogWarning("[AudioManager] PlayGlobalSFX called with an empty sound name.");
            return;
        }

        if (audioDB == null || globalSfxSource == null)
        {
            Debug.LogWarning("[AudioManager] Cannot play global SFX. Missing AudioDatabaseSO or global AudioSource.");
            return;
        }

        AudioClipData data = audioDB.Get(soundName);
        if (data == null)
        {
            Debug.LogWarning("[AudioManager] Missing global SFX: " + soundName);
            return;
        }

        AudioClip clip = data.GetRandomClip();
        if (clip == null)
            return;

        if (sfxMixerGroup != null && globalSfxSource.outputAudioMixerGroup != sfxMixerGroup)
            globalSfxSource.outputAudioMixerGroup = sfxMixerGroup;

        globalSfxSource.pitch = Random.Range(0.95f, 1.1f);
        globalSfxSource.PlayOneShot(clip, data.maxVolume);
    }

    private void ApplyMixerGroups()
    {
        if (bgmSource != null && bgmMixerGroup != null)
            bgmSource.outputAudioMixerGroup = bgmMixerGroup;

        if (globalSfxSource != null && sfxMixerGroup != null)
            globalSfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }
}