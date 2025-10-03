// QM_JournalBridge.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCrushers.QuestMachine.Wrappers; // UnityUIQuestJournalUI

public class QM_JournalBridge : MonoBehaviour
{
    public static QM_JournalBridge Instance { get; private set; }

    [SerializeField] private UnityUIQuestJournalUI journalUI;
    [SerializeField] private UI ui;

    [Header("Behavior")]
    [Tooltip("Attach a watcher to the journal UI to notify this bridge when it is shown/hidden.")]
    [SerializeField] private bool autoCreateWatcher = true;

    private JournalOpenCloseWatcher _watcher;

    private void Reset()
    {
        if (journalUI == null) journalUI = GetComponent<UnityUIQuestJournalUI>();
        if (ui == null) ui = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
    }

    private void Awake()
    {
        // Singleton + persist
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // Bind for the current scene (in case we were added at runtime)
        StartCoroutine(BindAfterSceneLoads());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        DetachWatcher();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(BindAfterSceneLoads());
    }

    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        StartCoroutine(BindAfterSceneLoads());
    }

    private IEnumerator BindAfterSceneLoads()
    {
        // Give scene a couple frames to finish spawning UI prefabs
        yield return null;
        yield return null;

        TryFindRefs();
        AttachWatcher();
    }

    private void TryFindRefs()
    {
        // Find UI singleton or a scene instance
        if (ui == null)
        {
            ui = UI.Instance ?? FindFirstObjectByType<UI>(FindObjectsInactive.Include);
        }

        // Find the Quest Machine journal UI in the scene
        if (journalUI == null)
        {
            journalUI = FindFirstObjectByType<UnityUIQuestJournalUI>(FindObjectsInactive.Include);
        }
    }

    private void AttachWatcher()
    {
        DetachWatcher();
        if (!autoCreateWatcher || journalUI == null) return;

        _watcher = journalUI.gameObject.GetComponent<JournalOpenCloseWatcher>();
        if (_watcher == null) _watcher = journalUI.gameObject.AddComponent<JournalOpenCloseWatcher>();
        _watcher.bridge = this;
    }

    private void DetachWatcher()
    {
        if (_watcher != null) _watcher.bridge = null;
        _watcher = null;
    }

    // ---- Notifications from the watcher (use or extend as needed) ----
    internal void NotifyJournalEnabled()
    {
        // If you later add callbacks on UI (e.g., ui.OnQuestJournalOpenedFromOutside()),
        // call them here safely. For now we just ensure references are valid.
        TryFindRefs();
    }

    internal void NotifyJournalDisabled()
    {
        TryFindRefs();
    }

    // Public helpers
    public UnityUIQuestJournalUI GetJournalUI() => journalUI;
    public UI GetUI() => ui;

    // Small helper that relays OnEnable/OnDisable of the journal UI
    private class JournalOpenCloseWatcher : MonoBehaviour
    {
        public QM_JournalBridge bridge;
        private void OnEnable() { bridge?.NotifyJournalEnabled(); }
        private void OnDisable() { bridge?.NotifyJournalDisabled(); }
    }
}
