using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PixelCrushers; // for SaveSystem (optional)

public class UI_GameOver : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private GameObject root;          // GameOverPanel
    [SerializeField] private CanvasGroup canvasGroup;  // on GameOverPanel

    [Header("Buttons")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button titleButton;

    [Header("Options")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
    [SerializeField] private UI_SaveLoadPanel saveLoadPanel; // optional drag; will auto-find
    [SerializeField] private string titleSceneName = "Title Screen"; // fallback if no UI.Instance

    private static UI_GameOver _instance;
    private Coroutine _fadeCo;
    private bool _openedSaveFromGameOver;

    public static UI_GameOver Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindObjectOfType<UI_GameOver>(true);
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        if (root == null) root = gameObject;
        if (canvasGroup == null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
        }

        // Safe default: hidden & non-interactive at start
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // The panel itself can be inactive in editor; we’ll activate when showing.
        if (root.activeSelf) root.SetActive(false);
    }

    private void OnEnable()
    {
        if (loadButton) { loadButton.onClick.RemoveAllListeners(); loadButton.onClick.AddListener(OnClickLoad); }
        if (titleButton) { titleButton.onClick.RemoveAllListeners(); titleButton.onClick.AddListener(OnClickTitle); }
    }

    private void Update()
    {
        // When Save/Load was opened from Game Over, allow ESC to go back.
        if (_openedSaveFromGameOver && saveLoadPanel != null && saveLoadPanel.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                saveLoadPanel.HandleCancel();
                Show(fadeDuration);
                _openedSaveFromGameOver = false;
            }
        }
    }

    // ------------------ Public Static API ------------------

    public static void ShowStatic(float fade = -1f)
    {
        var go = Instance;
        if (go == null) return;
        if (fade < 0f) fade = go.fadeDuration;
        go.Show(fade);
    }

    public static void HideStatic(float fade = -1f)
    {
        var go = Instance;
        if (go == null) return;
        if (fade < 0f) fade = go.fadeDuration;
        go.Hide(fade);
    }

    // ------------------ Instance API ------------------

    public void Show(float fade)
    {
        if (!root.activeSelf) root.SetActive(true);

        // ensure top of input stack
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeCanvas(1f, fade));
    }

    /// <summary>
    /// Hide GameOver. If you plan to show Save/Load and possibly come back with ESC,
    /// call HideKeepActive() instead to keep this object active but transparent.
    /// </summary>
    public void Hide(float fade)
    {
        if (!root.activeSelf) return;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutThenDisable(fade));
    }

    /// <summary>
    /// Hide visually but keep GameOver active (so we can re-show instantly on ESC).
    /// </summary>
    private void HideKeepActive(float fade)
    {
        if (!root.activeSelf) root.SetActive(true); // must be active to run coroutine
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeCanvas(0f, fade, after: () =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            // keep root active
        }));
    }

    // ------------------ Buttons ------------------

    private void OnClickLoad()
    {
        if (saveLoadPanel == null)
            saveLoadPanel = FindObjectOfType<UI_SaveLoadPanel>(true);

        if (saveLoadPanel == null)
        {
            Debug.LogError("[UI_GameOver] UI_SaveLoadPanel not found.");
            return;
        }

        // Hide this (fade or set inactive as you prefer)
        HideKeepActive(fadeDuration); // or root.SetActive(false);

        // Open in Load mode, tagging where it came from:
        saveLoadPanel.gameObject.SetActive(true);
        saveLoadPanel.OpenForLoad(UI_SaveLoadPanel.OpenContext.GameOver);
    }


    private void OnClickTitle()
    {
        // Prefer your UI helper if present (preserves DDOL filtering etc.)
        if (UI.Instance != null)
        {
            UI.Instance.GoToTitleScreenClean();
            return;
        }

        // Fallbacks if UI singleton isn’t around:
        if (SaveSystem.hasInstance)
        {
            SaveSystem.RestartGame(titleSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
        }
    }

    // ------------------ Fades ------------------

    private IEnumerator FadeCanvas(float target, float duration, System.Action after = null)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }
        canvasGroup.alpha = target;
        after?.Invoke();
    }

    private IEnumerator FadeOutThenDisable(float duration)
    {
        yield return FadeCanvas(0f, duration);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (root.activeSelf) root.SetActive(false);
    }
}
