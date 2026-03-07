using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

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

    private Transform player;

    private AudioClip lastMusicPlayed;
    private string currentBgmGroupName;
    private bool bgmShouldPlay;

    private Coroutine bgmRoutine;

    private void Start()
    {
        ApplyMixerGroups();
    }

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

        ApplyMixerGroups();
    }

    private void Update()
    {
        if (!bgmShouldPlay)
            return;

        if (bgmSource == null || audioDB == null)
            return;

        if (!bgmSource.isPlaying && !string.IsNullOrEmpty(currentBgmGroupName) && bgmRoutine == null)
        {
            NextBGM(currentBgmGroupName);
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

        bgmShouldPlay = true;

        if (musicGroup == currentBgmGroupName && bgmSource.isPlaying)
            return;

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

        if (bgmSource == null)
            return;

        StopBgmRoutine();
        bgmRoutine = StartCoroutine(StopBGMCo());
    }

    private IEnumerator StopBGMCo()
    {
        yield return FadeVolumeCo(bgmSource, 0f, bgmFadeDuration);

        bgmSource.Stop();
        bgmSource.clip = null;
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
        bgmSource.clip = nextMusic;
        bgmSource.volume = 0f;
        bgmSource.Play();

        yield return FadeVolumeCo(bgmSource, data.maxVolume, bgmFadeDuration);

        bgmRoutine = null;
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