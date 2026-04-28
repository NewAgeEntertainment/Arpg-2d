using UnityEngine;

public class MiniGameMusicPlayer : MonoBehaviour
{
    [Header("Mini-Game Music")]
    [SerializeField] private string miniGameMusicGroupName;
    [SerializeField] private bool playOnEnable = false;
    [SerializeField] private bool stopOnDisable = true;
    [SerializeField] private bool debugLogs = true;

    private bool musicStarted;

    private void OnEnable()
    {
        if (playOnEnable)
            StartMiniGameMusic();
    }

    private void OnDisable()
    {
        if (stopOnDisable)
            StopMiniGameMusic();
    }

    private void OnDestroy()
    {
        StopMiniGameMusic();
    }

    public void StartMiniGameMusic()
    {
        if (musicStarted)
        {
            if (debugLogs)
                Debug.Log($"[MiniGameMusicPlayer] Start skipped because music already started on {gameObject.name}");

            return;
        }

        if (string.IsNullOrWhiteSpace(miniGameMusicGroupName))
        {
            Debug.LogWarning($"[MiniGameMusicPlayer] Missing mini-game music group name on {gameObject.name}.");
            return;
        }

        if (AudioManager.instance == null)
        {
            Debug.LogWarning("[MiniGameMusicPlayer] Cannot start music. AudioManager.instance is null.");
            return;
        }

        musicStarted = true;

        if (debugLogs)
            Debug.Log($"[MiniGameMusicPlayer] START mini-game music: {miniGameMusicGroupName} on {gameObject.name}");

        AudioManager.instance.StartTemporaryBGM(miniGameMusicGroupName);
    }

    public void StopMiniGameMusic()
    {
        if (!musicStarted)
        {
            if (debugLogs)
                Debug.Log($"[MiniGameMusicPlayer] Stop skipped because musicStarted is false on {gameObject.name}");

            return;
        }

        if (debugLogs)
            Debug.Log($"[MiniGameMusicPlayer] STOP requested for mini-game music: {miniGameMusicGroupName} on {gameObject.name}");

        musicStarted = false;

        if (AudioManager.instance == null)
        {
            Debug.LogWarning("[MiniGameMusicPlayer] Cannot stop music. AudioManager.instance is null.");
            return;
        }

        if (LevelManager.Instance != null && !string.IsNullOrWhiteSpace(LevelManager.Instance.MusicGroupName))
        {
            AudioManager.instance.ForceStartBGM(LevelManager.Instance.MusicGroupName);

            if (debugLogs)
                Debug.Log($"[MiniGameMusicPlayer] Force restored level music: {LevelManager.Instance.MusicGroupName}");
        }
        else
        {
            AudioManager.instance.ForceStopBGMImmediate();

            if (debugLogs)
                Debug.Log("[MiniGameMusicPlayer] No LevelManager music found. Force stopped BGM.");
        }
    }
}