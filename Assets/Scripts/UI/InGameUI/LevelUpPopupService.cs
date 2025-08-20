using System.Collections.Generic;
using UnityEngine;

public class LevelUpPopupService : MonoBehaviour
{
    public static LevelUpPopupService Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private UI_LevelUpPopup popupPrefab;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Show(int newLevel, IDictionary<string, float> gains)
    {
        if (!Instance || !Instance.popupPrefab) return;

        var canvas = FindTopCanvas();
        var parent = canvas ? canvas.transform : null;

        var popup = Instantiate(Instance.popupPrefab, parent);
        // Make absolutely sure it’s active before Show():
        popup.gameObject.SetActive(true);

        if (popup.transform is RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        var dict = gains as Dictionary<string, float> ?? new Dictionary<string, float>(gains);
        popup.Show(newLevel, dict);
    }

    private static Canvas FindTopCanvas()
    {
        foreach (var c in Object.FindObjectsOfType<Canvas>())
            if (c.isActiveAndEnabled &&
               (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                return c;
        return null;
    }
}
