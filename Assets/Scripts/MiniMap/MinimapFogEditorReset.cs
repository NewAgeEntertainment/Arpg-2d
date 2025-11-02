#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MinimapFogEditorReset : MonoBehaviour
{
    [Tooltip("If your MinimapFog exposes ClearAll(), we’ll call it at the right times.")]
    public Object fogComponent; // drag your MinimapFog component here (optional)

    [Tooltip("If your fog saves to PlayerPrefs, set the key used for persistence (optional).")]
    public string playerPrefsKey; // e.g., "MinimapFog_MyMap"

    [Tooltip("Clear fog when entering Play Mode (fresh run).")]
    public bool clearOnPlayEnter = true;

    [Tooltip("Clear saved fog when exiting Play Mode (so it doesn't stick next run).")]
    public bool clearOnPlayExit = true;

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += HandleState;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandleState;
    }

    void HandleState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && clearOnPlayEnter)
            TryClearNow();

        if (state == PlayModeStateChange.ExitingPlayMode && clearOnPlayExit)
            TryClearNow();
    }

    void TryClearNow()
    {
        // Call ClearAll() if your fog exposes it
        if (fogComponent != null)
        {
            var m = fogComponent.GetType().GetMethod("ClearAll");
            if (m != null) m.Invoke(fogComponent, null);
        }

        // Nuke PlayerPrefs key if provided
        if (!string.IsNullOrEmpty(playerPrefsKey) && PlayerPrefs.HasKey(playerPrefsKey))
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
#endif
