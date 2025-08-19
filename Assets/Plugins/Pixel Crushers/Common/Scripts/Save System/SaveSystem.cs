// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace PixelCrushers
{
    /// <summary>
    /// This is the main Save System class. It runs as a singleton MonoBehaviour
    /// and provides static methods to save and load games.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper instead.
    public class SaveSystem : MonoBehaviour
    {
        public const int NoSceneIndex = -1;

        /// <summary>
        /// Stores an int indicating the slot number of the most recently saved game.
        /// </summary>
        public const string LastSavedGameSlotPlayerPrefsKey = "savedgame_lastSlotNum";

        [Tooltip("Optional saved game version number of your choosing. Version number is included in saved game files.")]
        [SerializeField] private int m_version = 0;

        [Tooltip("When loading a game, load the scene that the game was saved in.")]
        [SerializeField] private bool m_saveCurrentScene = true;

        [Tooltip("Highest save slot number allowed.")]
        [SerializeField] private int m_maxSaveSlot = 99999;

        [Tooltip("When loading a game/scene, wait this many frames before applying saved data to allow other scripts to initialize first.")]
        [SerializeField] private int m_framesToWaitBeforeApplyData = 0;

        [Tooltip("Log debug info.")]
        [SerializeField] private bool m_debug = false;

        // ---------- ADDS: Metadata + Playtime ----------
        [Header("Slot Metadata (Adds)")]
        [Tooltip("If enabled, writes scene, play time, timestamp & exists flag to PlayerPrefs after each save.")]
        [SerializeField] private bool m_writeSlotMetadataOnSave = true;

        [Tooltip("Track unscaled play time while the app is running (used for metadata).")]
        [SerializeField] private bool m_trackPlayTime = true;

        private static double m_playSecondsUnscaled = 0; // unscaled seconds since app start (or since you reset)
        public static int playSecondsInt => Mathf.Max(0, (int)Math.Floor(m_playSecondsUnscaled));

        /// <summary>Raised after a save completes and metadata is written. Arg = slot number.</summary>
        public static event Action<int> savedToSlot = delegate { };

        /// <summary>Convenience accessor for the last saved slot (from PlayerPrefs).</summary>
        public static int lastSavedSlot => PlayerPrefs.GetInt(LastSavedGameSlotPlayerPrefsKey, -1);

        public static void ResetPlayTimeCounter() => m_playSecondsUnscaled = 0;
        // ------------------------------------------------

        private bool m_isLoadingAdditiveScene = false;

        private static SaveSystem m_instance = null;
        private static HashSet<Saver> m_savers = new HashSet<Saver>();
        private static List<Saver> m_tmpSavers = new List<Saver>();
        private static SavedGameData m_savedGameData = new SavedGameData();
        private static DataSerializer m_serializer = null;
        private static SavedGameDataStorer m_storer = null;
        private static SceneTransitionManager m_sceneTransitionManager = null;
        private static bool m_allowNegativeSlotNumbers = false;
        private static GameObject m_playerSpawnpoint = null;
        private static int m_currentSceneIndex = NoSceneIndex;
        private static List<string> m_addedScenes = new List<string>();
        private static bool m_autoUnloadAdditiveScenes = false;
        private static AsyncOperation m_currentAsyncOperation = null;

#if USE_ADDRESSABLES
        private static UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> m_currentAsyncOperationHandle;
#endif

        private static int m_framesToWaitBeforeSaveDataAppliedEvent = 0;
        private static bool m_isQuitting = false;

#if UNITY_2019_3_OR_NEWER && UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStaticVariables()
        {
            m_instance = null;
            m_savers = new HashSet<Saver>();
            m_tmpSavers = new List<Saver>();
            m_savedGameData = new SavedGameData();
            m_serializer = null;
            m_storer = null;
            m_sceneTransitionManager = null;
            m_playerSpawnpoint = null;
            m_currentSceneIndex = NoSceneIndex;
            m_addedScenes = new List<string>();
            m_currentAsyncOperation = null;
            m_framesToWaitBeforeSaveDataAppliedEvent = 0;
            m_isQuitting = false;
            m_playSecondsUnscaled = 0; // (Adds) reset on domain reload in Editor
        }
#endif

        public static int version
        {
            get { return (m_instance != null) ? m_instance.m_version : 0; }
            set { if (m_instance != null) m_instance.m_version = value; }
        }

        public static bool saveCurrentScene
        {
            get { return (m_instance != null) ? m_instance.m_saveCurrentScene : true; }
            set { if (m_instance != null) m_instance.m_saveCurrentScene = value; }
        }

        public static int maxSaveSlot
        {
            get { return (m_instance != null) ? m_instance.m_maxSaveSlot : int.MaxValue; }
            set { if (m_instance != null) m_instance.m_maxSaveSlot = value; }
        }

        public static int framesToWaitBeforeApplyData
        {
            get { return (m_instance != null) ? m_instance.m_framesToWaitBeforeApplyData : 1; }
            set { if (m_instance != null) m_instance.m_framesToWaitBeforeApplyData = value; }
        }

        public static int framesToWaitBeforeSaveDataAppliedEvent
        {
            get { return m_framesToWaitBeforeSaveDataAppliedEvent; }
            set { m_framesToWaitBeforeSaveDataAppliedEvent = value; }
        }

        public static bool debug
        {
            get { return (m_instance != null) ? m_instance.m_debug && Debug.isDebugBuild : false; }
            set { if (m_instance != null) m_instance.m_debug = value; }
        }

        public static bool hasInstance { get { return m_instance != null; } }

        public static SaveSystem instance
        {
            get
            {
                if (m_instance == null && !m_isQuitting)
                {
                    m_instance = PixelCrushers.GameObjectUtility.FindFirstObjectByType<SaveSystem>();
                    if (m_instance == null)
                    {
                        m_instance = new GameObject("Save System", typeof(SaveSystem)).GetComponent<SaveSystem>();
                    }
                }
                return m_instance;
            }
        }

        public static DataSerializer serializer
        {
            get
            {
                if (m_serializer == null)
                {
                    m_serializer = instance.GetComponent<DataSerializer>();
                    if (m_serializer == null && !m_isQuitting)
                    {
                        Debug.Log("Save System: No DataSerializer found on " + instance.name + ". Adding JsonDataSerializer.", instance);
                        m_serializer = instance.gameObject.AddComponent<JsonDataSerializer>();
                    }
                }
                return m_serializer;
            }
        }

        public static SavedGameDataStorer storer
        {
            get
            {
                if (m_storer == null)
                {
                    m_storer = instance.GetComponent<SavedGameDataStorer>();
                    if (m_storer == null && !m_isQuitting)
                    {
                        Debug.Log("Save System: No SavedGameDataStorer found on " + instance.name + ". Adding PlayerPrefsSavedGameDataStorer.", instance);
                        m_storer = instance.gameObject.AddComponent<PlayerPrefsSavedGameDataStorer>();
                    }
                }
                return m_storer;
            }
        }

        public static SceneTransitionManager sceneTransitionManager
        {
            get
            {
                if (m_sceneTransitionManager == null)
                {
                    m_sceneTransitionManager = instance.GetComponentInChildren<SceneTransitionManager>();
                }
                return m_sceneTransitionManager;
            }
        }

        public bool allowNegativeSlotNumbers
        {
            get { return m_allowNegativeSlotNumbers; }
            set { m_allowNegativeSlotNumbers = value; }
        }

        public static List<string> addedScenes { get { return m_addedScenes; } }

        public static bool autoUnloadAdditiveScenes
        {
            get { return m_autoUnloadAdditiveScenes; }
            set { m_autoUnloadAdditiveScenes = value; }
        }

        public static AsyncOperation currentAsyncOperation
        {
            get { return m_currentAsyncOperation; }
            set { m_currentAsyncOperation = value; }
        }

        public static SavedGameData currentSavedGameData
        {
            get { return m_savedGameData; }
            set { m_savedGameData = value; }
        }

        public static GameObject playerSpawnpoint
        {
            get { return m_playerSpawnpoint; }
            set { m_playerSpawnpoint = value; }
        }

        public static int currentSceneIndex
        {
            get
            {
                if (m_currentSceneIndex == NoSceneIndex) m_currentSceneIndex = GetCurrentSceneIndex();
                return m_currentSceneIndex;
            }
        }

        public delegate string ValidateSceneNameDelegate(string sceneName, SceneValidationMode sceneValidationMode);
        public static ValidateSceneNameDelegate validateNameScene = null;

        public delegate void SceneLoadedDelegate(string sceneName, int sceneIndex);
        public static event SceneLoadedDelegate sceneLoaded = delegate { };

        public static event System.Action saveStarted = delegate { };
        public static event System.Action saveEnded = delegate { };
        public static event System.Action loadStarted = delegate { };
        public static event System.Action loadEnded = delegate { };
        public static event System.Action saveDataApplied = delegate { };

        private void Awake()
        {
            if (m_instance == null)
            {
                m_instance = this;
#if UNITY_EDITOR
                if (Application.isPlaying)
                { // If GameObject is hidden in Scene view, DontDestroyOnLoad will report (harmless) error.
                    UnityEditor.SceneVisibilityManager.instance.Show(gameObject, true);
                }
#endif
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // (Adds) simple unscaled playtime tracker
            if (Application.isPlaying && m_trackPlayTime)
            {
                m_playSecondsUnscaled += Time.unscaledDeltaTime;
            }
        }

        private void OnApplicationQuit()
        {
            m_isQuitting = true;
            BeforeSceneChange();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            FinishedLoadingScene(scene.name, scene.buildIndex);
        }

        public static string GetCurrentSceneName()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        public static int GetCurrentSceneIndex()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        }

        public static bool IsSceneInBuildSettings(string sceneName)
        {
            for (var n = 0; n < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; ++n)
            {
                var scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(n);
                if (string.IsNullOrEmpty(scenePath)) continue;
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(scenePath), sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static void SceneManagerOrAddressablesLoadScene(string sceneName)
        {
            if (IsSceneInBuildSettings(sceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                return;
            }
#if USE_ADDRESSABLES
            // If not in build settings, try loading an Addressable scene:
            m_currentAsyncOperationHandle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(sceneName);
#else
            Debug.LogError("Can't load scene. Scene is not in build settings: " + sceneName);
#endif
        }

        private static void SceneManagerOrAddressablesLoadSceneAsync(string sceneName)
        {
            m_currentAsyncOperation = null;
            if (IsSceneInBuildSettings(sceneName))
            {
                m_currentAsyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                return;
            }
#if USE_ADDRESSABLES
            // If not in build settings, try loading an Addressable scene:
            m_currentAsyncOperationHandle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(sceneName);
#else
            Debug.LogError("Can't load scene. Scene is not in build settings: " + sceneName);
#endif
        }

        private static IEnumerator SceneManagerOrAddressablesLoadSceneAdditiveAsync(string sceneName)
        {
            if (IsSceneInBuildSettings(sceneName))
            {
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
            else
            {
#if USE_ADDRESSABLES
                // If not in build settings, try loading an Addressable scene:
                m_currentAsyncOperationHandle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                while (!m_currentAsyncOperation.isDone)
                {
                    yield return null;
                }
#else
                Debug.LogError("Can't load additive scene. Scene is not in build settings: " + sceneName);
#endif
            }
        }

        private static IEnumerator LoadSceneInternal(string sceneName, SceneValidationMode sceneValidationMode)
        {
            m_addedScenes.Clear();
            if (sceneTransitionManager == null)
            {
                if (sceneName.StartsWith("index:"))
                {
                    var index = SafeConvert.ToInt(sceneName.Substring("index:".Length));
                    UnityEngine.SceneManagement.SceneManager.LoadScene(index);
                }
                else
                {
                    if (validateNameScene != null) sceneName = validateNameScene(sceneName, sceneValidationMode);
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        if (debug) Debug.LogWarning("Scene '" + sceneName + "' is not a valid scene to load.");
                        yield break;
                    }
                    SceneManagerOrAddressablesLoadScene(sceneName);
                }
                yield break;
            }
            else
            {
                yield return instance.StartCoroutine(LoadSceneInternalTransitionCoroutine(sceneName, sceneValidationMode));
            }
        }

        private static IEnumerator LoadSceneInternalTransitionCoroutine(string sceneName, SceneValidationMode sceneValidationMode)
        {
            m_addedScenes.Clear();
            yield return instance.StartCoroutine(sceneTransitionManager.LeaveScene());
            if (sceneName.StartsWith("index:"))
            {
                var index = SafeConvert.ToInt(sceneName.Substring("index:".Length));
                m_currentAsyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(index);
            }
            else
            {
                if (validateNameScene != null) sceneName = validateNameScene(sceneName, sceneValidationMode);
                if (string.IsNullOrEmpty(sceneName))
                {
                    if (debug) Debug.LogWarning("Scene '" + sceneName + "' is not a valid scene to load.");
                    yield break;
                }
                SceneManagerOrAddressablesLoadSceneAsync(sceneName);
            }
            if (m_currentAsyncOperation != null)
            {
                while (m_currentAsyncOperation != null && !m_currentAsyncOperation.isDone)
                {
                    sceneTransitionManager.OnLoading(m_currentAsyncOperation.progress);
                    yield return null;
                }
            }
#if USE_ADDRESSABLES
            else
            {
                while (!m_currentAsyncOperationHandle.IsDone)
                {
                    sceneTransitionManager.OnLoading(m_currentAsyncOperationHandle.PercentComplete);
                    yield return null;
                }
            }
#endif
            sceneTransitionManager.OnLoading(1);
            m_currentAsyncOperation = null;
            instance.StartCoroutine(sceneTransitionManager.EnterScene());
        }

        public static IEnumerator LoadAdditiveSceneInternal(string sceneName, SceneValidationMode sceneValidationMode)
        {
            if (validateNameScene != null) sceneName = validateNameScene(sceneName, sceneValidationMode);
            if (string.IsNullOrEmpty(sceneName)) yield break;
            yield return SceneManagerOrAddressablesLoadSceneAdditiveAsync(sceneName);
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid()) yield break;
            var rootGOs = scene.GetRootGameObjects();
            for (int i = 0; i < rootGOs.Length; i++)
            {
                RecursivelyApplySavers(rootGOs[i].transform);
            }
        }

        public static void UnloadAdditiveSceneInternal(string sceneName)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
            {
                var rootGOs = scene.GetRootGameObjects();
                for (int i = 0; i < rootGOs.Length; i++)
                {
                    var rootGO = rootGOs[i].transform;
                    RecursivelyRecordSavers(rootGO, scene.buildIndex);
                    RecursivelyInformBeforeSceneChange(rootGO);
                }
            }
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
        }

        public static void RecursivelyRecordSavers(Transform t, int sceneIndex)
        {
            if (t == null) return;
            var saver = t.GetComponent<Saver>();
            if (saver != null) currentSavedGameData.SetData(saver.key, saver.saveAcrossSceneChanges ? -1 : sceneIndex, saver.RecordData());
            foreach (Transform child in t)
            {
                RecursivelyRecordSavers(child, sceneIndex);
            }
        }

        public static void RecursivelyApplySavers(Transform t)
        {
            if (t == null) return;
            var saver = t.GetComponent<Saver>();
            if (saver != null) saver.ApplyData(currentSavedGameData.GetData(saver.key));
            foreach (Transform child in t)
            {
                RecursivelyApplySavers(child);
            }
        }

        public static void RecursivelyInformBeforeSceneChange(Transform t)
        {
            if (t == null) return;
            var saver = t.GetComponent<Saver>();
            if (saver != null) saver.OnBeforeSceneChange();
            foreach (Transform child in t)
            {
                RecursivelyInformBeforeSceneChange(child);
            }
        }

        private static bool SanitizeSlotNumberForSave(int slotNumber, out int sanitizedSlotNumber)
        {
            if (slotNumber >= 0 || m_instance == null || m_instance.allowNegativeSlotNumbers)
            {
                sanitizedSlotNumber = slotNumber;
                return true;
            }
            for (int i = 0; i <= maxSaveSlot; i++)
            {
                if (!HasSavedGameInSlot(i))
                {
                    sanitizedSlotNumber = i;
                    return true;
                }
            }
            sanitizedSlotNumber = 0;
            return false;
        }

        public void SaveGameToSlot(int slotNumber) { SaveToSlot(slotNumber); }
        public void LoadGameFromSlot(int slotNumber) { LoadFromSlot(slotNumber); }
        public void LoadSceneAtSpawnpoint(string sceneNameAndSpawnpoint) { LoadScene(sceneNameAndSpawnpoint); }

        public static bool HasSavedGameInSlot(int slotNumber) { return storer.HasDataInSlot(slotNumber); }
        public static void DeleteSavedGameInSlot(int slotNumber) { storer.DeleteSavedGameData(slotNumber); }

        public static void SaveToSlot(int slotNumber)
        {
            instance.StartCoroutine(SaveToSlotCoroutine(slotNumber));
        }

        private static IEnumerator SaveToSlotCoroutine(int slotNumber)
        {
            if (!SanitizeSlotNumberForSave(slotNumber, out slotNumber))
            {
                Debug.LogError("Can't save game. Invalid save slot: " + slotNumber);
                yield break;
            }
            saveStarted();
            yield return null;

            PlayerPrefs.SetInt(LastSavedGameSlotPlayerPrefsKey, slotNumber);

            // Perform actual save:
            yield return storer.StoreSavedGameDataAsync(slotNumber, RecordSavedGameData());

            // ---------- ADDS: write metadata after save ----------
            if (instance != null && instance.m_writeSlotMetadataOnSave)
            {
                WriteSlotMetadata(slotNumber);
            }
            // Notify listeners which slot was saved:
            savedToSlot(slotNumber);
            // -----------------------------------------------------

            saveEnded();
        }

        public static void SaveToSlotImmediate(int slotNumber)
        {
            if (!SanitizeSlotNumberForSave(slotNumber, out slotNumber))
            {
                Debug.LogError("Can't save game. Invalid save slot: " + slotNumber);
                return;
            }
            saveStarted();

            PlayerPrefs.SetInt(LastSavedGameSlotPlayerPrefsKey, slotNumber);

            storer.StoreSavedGameData(slotNumber, RecordSavedGameData());

            // ---------- ADDS: write metadata after save ----------
            if (instance != null && instance.m_writeSlotMetadataOnSave)
            {
                WriteSlotMetadata(slotNumber);
            }
            savedToSlot(slotNumber);
            // -----------------------------------------------------

            saveEnded();
        }

        // ---------- ADDS: central metadata writer ----------
        private static void WriteSlotMetadata(int slotNumber)
        {
            try
            {
                PlayerPrefs.SetString($"SaveSlot_{slotNumber}_scene", GetCurrentSceneName());
                PlayerPrefs.SetInt($"SaveSlot_{slotNumber}_playSeconds", playSecondsInt);
                PlayerPrefs.SetString($"SaveSlot_{slotNumber}_time",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                PlayerPrefs.SetInt($"SaveSlot_{slotNumber}_exists", 1);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        // -----------------------------------------------------

        public static void LoadFromSlot(int slotNumber)
        {
            if (!HasSavedGameInSlot(slotNumber))
            {
                if (Debug.isDebugBuild) Debug.LogWarning("Save System: LoadFromSlot(" + slotNumber + ") but there is no saved game in this slot.");
                return;
            }
            if (loadStarted.GetInvocationList().Length > 1)
            {
                instance.StartCoroutine(LoadFromSlotCoroutine(slotNumber));
            }
            else
            {
                LoadFromSlotNow(slotNumber);
            }
        }

        private static IEnumerator LoadFromSlotCoroutine(int slotNumber)
        {
            loadStarted();
            yield return null;
            LoadFromSlotNow(slotNumber);
        }

        private static void NotifyLoadEndedWhenSceneLoaded(string sceneName, int sceneIndex)
        {
            sceneLoaded -= NotifyLoadEndedWhenSceneLoaded;
            loadEnded();
        }

        private static void LoadFromSlotNow(int slotNumber)
        {
            sceneLoaded += NotifyLoadEndedWhenSceneLoaded;
            LoadGame(storer.RetrieveSavedGameData(slotNumber));
        }

        public static void RegisterSaver(Saver saver)
        {
            if (saver == null || m_savers.Contains(saver)) return;
            m_savers.Add(saver);
        }

        public static void UnregisterSaver(Saver saver)
        {
            m_savers.Remove(saver);
        }

        public static void ClearSavedGameData()
        {
            m_savedGameData = new SavedGameData();
        }

        public static SavedGameData RecordSavedGameData()
        {
            m_savedGameData.version = version;
            m_savedGameData.sceneName = GetCurrentSceneName();
            foreach (var saver in m_savers)
            {
                try
                {
                    m_savedGameData.SetData(saver.key, GetSaverSceneIndex(saver), saver.RecordData());
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return m_savedGameData;
        }

        private static int GetSaverSceneIndex(Saver saver)
        {
            return (saver == null || !saver.saveAcrossSceneChanges) ? currentSceneIndex : NoSceneIndex;
        }

        public static void UpdateSaveData(Saver saver, string data)
        {
            m_savedGameData.SetData(saver.key, GetSaverSceneIndex(saver), data);
        }

        public static void ApplySavedGameData(SavedGameData savedGameData)
        {
            if (savedGameData != null)
            {
                m_savedGameData = savedGameData;
                if (m_savers.Count > 0)
                {
                    m_tmpSavers.Clear();
                    m_tmpSavers.AddRange(m_savers);
                    for (int i = m_tmpSavers.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            if (0 <= i && i < m_tmpSavers.Count)
                            {
                                var saver = m_tmpSavers[i];
                                if (saver != null) saver.ApplyData(savedGameData.GetData(saver.key));
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
            if (framesToWaitBeforeSaveDataAppliedEvent == 0 || instance == null)
            {
                saveDataApplied();
            }
            else
            {
                instance.StartCoroutine(DelayedSaveDataAppliedCoroutine(framesToWaitBeforeSaveDataAppliedEvent));
                framesToWaitBeforeSaveDataAppliedEvent = 0;
            }
        }

        protected static IEnumerator DelayedSaveDataAppliedCoroutine(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
            yield return CoroutineUtility.endOfFrame;
            saveDataApplied();
        }

        public static void ApplySavedGameData()
        {
            ApplySavedGameData(m_savedGameData);
        }

        public static void BeforeSceneChange()
        {
            var savers = new List<Saver>(m_savers);
            for (int i = savers.Count - 1; i >= 0; i--)
            {
                var saver = savers[i];
                if (saver == null) continue;
                try
                {
                    saver.OnBeforeSceneChange();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            try
            {
                SceneNotifier.NotifyWillUnloadScene(m_currentSceneIndex);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void LoadGame(SavedGameData savedGameData)
        {
            if (savedGameData == null)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("SaveSystem.LoadGame received null saved game data. Not loading.");
            }
            else if (saveCurrentScene)
            {
                instance.StartCoroutine(LoadSceneCoroutine(savedGameData, null, SceneValidationMode.LoadingSavedGame));
            }
            else
            {
                ApplySavedGameData(savedGameData);
            }
        }

        public static void LoadScene(string sceneNameAndSpawnpoint)
        {
            if (string.IsNullOrEmpty(sceneNameAndSpawnpoint)) return;
            string sceneName = sceneNameAndSpawnpoint;
            string spawnpointName = string.Empty;
            if (sceneNameAndSpawnpoint.Contains("@"))
            {
                var strings = sceneNameAndSpawnpoint.Split('@');
                sceneName = strings[0];
                spawnpointName = (strings.Length > 1) ? strings[1] : null;
            }
            var savedGameData = RecordSavedGameData();
            savedGameData.sceneName = sceneName;
            instance.StartCoroutine(LoadSceneCoroutine(savedGameData, spawnpointName, SceneValidationMode.LoadingScene));
        }

        private static IEnumerator LoadSceneCoroutine(SavedGameData savedGameData, string spawnpointName, SceneValidationMode sceneValidationMode)
        {
            if (savedGameData == null) yield break;
            if (debug) Debug.Log("Save System: Loading scene " + savedGameData.sceneName +
                (string.IsNullOrEmpty(spawnpointName) ? string.Empty : " [spawn at " + spawnpointName + "]"));
            m_savedGameData = savedGameData;
            BeforeSceneChange();
            if (autoUnloadAdditiveScenes) UnloadAllAdditiveScenes();
            yield return LoadSceneInternal(savedGameData.sceneName, sceneValidationMode);
            ApplyDataImmediate();
            for (int i = 0; i < framesToWaitBeforeApplyData; i++)
            {
                yield return null;
            }
            yield return CoroutineUtility.endOfFrame;
            m_playerSpawnpoint = !string.IsNullOrEmpty(spawnpointName) ? GameObject.Find(spawnpointName) : null;
            if (!string.IsNullOrEmpty(spawnpointName) && m_playerSpawnpoint == null) Debug.LogWarning("Save System: Can't find spawnpoint '" + spawnpointName + "'. Is spelling and capitalization correct?");
            ApplySavedGameData(savedGameData);
        }

        private static void ApplyDataImmediate()
        {
            if (m_savers.Count > 0)
            {
                m_tmpSavers.Clear();
                m_tmpSavers.AddRange(m_savers);
                for (int i = m_tmpSavers.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (0 <= i && i < m_tmpSavers.Count)
                        {
                            var saver = m_tmpSavers[i];
                            if (saver != null) saver.ApplyDataImmediate();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private void FinishedLoadingScene(string sceneName, int sceneIndex)
        {
            m_currentSceneIndex = sceneIndex;
            if (!m_isLoadingAdditiveScene)
            {
                m_savedGameData.DeleteObsoleteSaveData(sceneIndex);
            }
            m_isLoadingAdditiveScene = false;
            sceneLoaded(sceneName, sceneIndex);
        }

        public static void LoadAdditiveScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || m_addedScenes.Contains(sceneName)) return;
            m_addedScenes.Add(sceneName);
            instance.m_isLoadingAdditiveScene = true;
            instance.StartCoroutine(LoadAdditiveSceneInternal(sceneName, SceneValidationMode.LoadingScene));
        }

        public static void UnloadAdditiveScene(string sceneName)
        {
            if (!m_addedScenes.Contains(sceneName)) return;
            m_addedScenes.Remove(sceneName);
            UnloadAdditiveSceneInternal(sceneName);
        }

        public static void UnloadAllAdditiveScenes()
        {
            for (int i = m_addedScenes.Count - 1; i >= 0; i--)
            {
                UnloadAdditiveScene(m_addedScenes[i]);
            }
        }

        public static void RestartGame(string startingSceneName)
        {
            ResetGameState();
            instance.StartCoroutine(LoadSceneInternal(startingSceneName, SceneValidationMode.RestartingGame));
        }

        public static void ResetGameState()
        {
            ClearSavedGameData();
            BeforeSceneChange();
            SaversRestartGame();
            // (Adds) You can uncomment if you want to reset playtime on "New Game":
            // ResetPlayTimeCounter();
        }

        public static void SaversRestartGame()
        {
            if (m_savers.Count <= 0) return;
            foreach (var saver in m_savers.ToList())
            {
                try
                {
                    if (saver != null) saver.OnRestartGame();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static string Serialize(object data) { return serializer.Serialize(data); }
        public static T Deserialize<T>(string s, T data = default(T)) { return serializer.Deserialize<T>(s, data); }
    }
}
