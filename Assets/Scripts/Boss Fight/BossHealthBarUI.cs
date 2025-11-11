using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("Wiring")]
    [SerializeField] private CanvasGroup canvasGroup; // optional fade
    [SerializeField] private Slider slider;           // 0..1
    [SerializeField] private TextMeshProUGUI bossName;

    private Entity_Health boundHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unbind();
    }

    // ---- Public API ----
    public void ShowFor(Entity_Health health, string displayName)
    {
        if (health == null) return;

        // Rebind
        Unbind();
        boundHealth = health;
        boundHealth.OnHealthUpdate += HandleHealthUpdate;
        boundHealth.OnDied += HandleDied;

        if (bossName) bossName.text = string.IsNullOrEmpty(displayName) ? "BOSS" : displayName;

        // Refresh once
        HandleHealthUpdate();

        // Show
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;
        }
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        Unbind();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else gameObject.SetActive(false);
    }

    // ---- Internals ----
    private void HideImmediate()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        else gameObject.SetActive(false);
    }

    private void Unbind()
    {
        if (boundHealth != null)
        {
            boundHealth.OnHealthUpdate -= HandleHealthUpdate;
            boundHealth.OnDied -= HandleDied;
            boundHealth = null;
        }
    }

    private void HandleHealthUpdate()
    {
        if (boundHealth == null || slider == null) return;
        slider.value = boundHealth.GetHealthPercent(); // 0..1
    }

    private void HandleDied()
    {
        Hide();
    }
}
