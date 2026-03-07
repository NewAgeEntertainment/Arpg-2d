using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private string musicGroupName;
    public string MusicGroupName => musicGroupName;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (AudioManager.instance != null && !string.IsNullOrWhiteSpace(musicGroupName))
            AudioManager.instance.StartBGM(musicGroupName);
    }
}