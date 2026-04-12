using System.Collections;
using System.Collections.Generic;
using PixelCrushers;
using Rewired;
using UnityEngine;

public class ReturnToTitleManager : MonoBehaviour
{
    public static ReturnToTitleManager Instance { get; private set; }

    [Header("Title Scene")]
    [SerializeField] private string titleSceneName = "Title Screen";

    [Header("Optional Suspend Save")]
    [SerializeField] private bool allowSuspendSlot = true;

    [Header("Keep On Return")]
    [SerializeField] private bool keepAudioManager = true;

    [Header("Force-Clean DDOL Roots By Name")]
    [SerializeField]
    private string[] destroyRootNames =
    {
        "GameManager",
        "PartyRoot",
        "Player",
        "UI",
        "Dialogue Manager",
        "Quest Machine",
        "CinemachineBrain",
        "Lioncard"
    };

    private const int SuspendSlot = -1;
    private const string SuspendKey = "suspend_exists";

    private bool isReturning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ReturnToTitleClean()
    {
        if (isReturning) return;
        StartCoroutine(ReturnToTitleCo(saveSuspend: false));
    }

    public void ReturnToTitleWithSuspend()
    {
        if (isReturning) return;
        StartCoroutine(ReturnToTitleCo(saveSuspend: true));
    }

    public static bool TryLoadSuspendAndClear()
    {
        if (PlayerPrefs.GetInt(SuspendKey, 0) != 1)
            return false;

        if (SaveSystem.hasInstance)
            SaveSystem.instance.allowNegativeSlotNumbers = true;

        if (SaveSystem.HasSavedGameInSlot(SuspendSlot))
        {
            SaveSystem.LoadFromSlot(SuspendSlot);
            SaveSystem.DeleteSavedGameInSlot(SuspendSlot);
            PlayerPrefs.DeleteKey(SuspendKey);
            PlayerPrefs.Save();
            return true;
        }

        PlayerPrefs.DeleteKey(SuspendKey);
        PlayerPrefs.Save();
        return false;
    }

    private IEnumerator ReturnToTitleCo(bool saveSuspend)
    {
        isReturning = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (saveSuspend && allowSuspendSlot)
        {
            if (SaveSystem.hasInstance)
                SaveSystem.instance.allowNegativeSlotNumbers = true;

            SaveSystem.SaveToSlotImmediate(SuspendSlot);
            PlayerPrefs.SetInt(SuspendKey, 1);
            PlayerPrefs.Save();
        }

        // Kill the player first.
        DestroyPlayerForTitleReturn();

        // Let Destroy() process.
        yield return null;

        // Then destroy gameplay DDOL leftovers.
        CleanDontDestroyOnLoadForTitle();

        yield return null;

        SaveSystem.autoUnloadAdditiveScenes = true;
        SaveSystem.RestartGame(titleSceneName);
    }

    private void CleanDontDestroyOnLoadForTitle()
    {
        var keepRoots = new HashSet<GameObject>();

        // Keep this manager root alive long enough to finish transition.
        keepRoots.Add(transform.root.gameObject);

        // Keep Pixel Crushers Save System.
        if (SaveSystem.hasInstance && SaveSystem.instance != null)
            keepRoots.Add(SaveSystem.instance.transform.root.gameObject);

        // Keep exactly one Rewired Input Manager.
        var rewired = FindObjectOfType<InputManager>(true);
        if (rewired != null)
            keepRoots.Add(rewired.transform.root.gameObject);

        // Optionally keep AudioManager.
        if (keepAudioManager && AudioManager.instance != null)
            keepRoots.Add(AudioManager.instance.transform.root.gameObject);

        var ddolScene = gameObject.scene;
        var roots = new List<GameObject>();
        ddolScene.GetRootGameObjects(roots);

        for (int i = roots.Count - 1; i >= 0; i--)
        {
            var root = roots[i];
            if (root == null) continue;

            bool forceDestroyByName = MatchesForcedDestroyName(root.name);

            if (!forceDestroyByName && keepRoots.Contains(root))
            {
                Debug.Log($"[ReturnToTitleManager] KEEP DDOL root: {root.name}");
                continue;
            }

            Debug.Log($"[ReturnToTitleManager] DESTROY DDOL root: {root.name}");
            Destroy(root);
        }
    }

    private void DestroyPlayerForTitleReturn()
    {
        // Prefer the singleton if it exists.
        if (Player.instance != null)
        {
            Debug.Log($"[ReturnToTitleManager] Destroying Player root: {Player.instance.transform.root.name}");
            Destroy(Player.instance.transform.root.gameObject);
            return;
        }

        // Fallback: find any player in active/inactive objects.
        var player = FindObjectOfType<Player>(true);
        if (player != null)
        {
            Debug.Log($"[ReturnToTitleManager] Destroying fallback Player root: {player.transform.root.name}");
            Destroy(player.transform.root.gameObject);
        }
    }

    private void DestroyPartyForTitleReturn()
    {
        if (CompanionPartyManager.Instance != null)
        {
            CompanionPartyManager.Instance.DismissAll(true);
        }
    }

    private bool MatchesForcedDestroyName(string rootName)
    {
        if (string.IsNullOrWhiteSpace(rootName) || destroyRootNames == null)
            return false;

        for (int i = 0; i < destroyRootNames.Length; i++)
        {
            var candidate = destroyRootNames[i];
            if (string.IsNullOrWhiteSpace(candidate)) continue;

            if (string.Equals(rootName, candidate, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}