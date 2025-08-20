using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_LevelUpPopup : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private TMP_Text lineTemplate;   // keep DISABLED in prefab

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float showDuration = 1.00f;
    [SerializeField] private float fadeOutDuration = 0.30f;

    [Header("Optional SFX")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip levelUpClip;

    private readonly List<TMP_Text> spawned = new();

    private void Awake()
    {
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (lineTemplate) lineTemplate.gameObject.SetActive(false); // critical
    }

    public void Show(int newLevel, IDictionary<string, float> gains)
    {
        // Ensure we’re active before starting coroutines:
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (titleText) titleText.text = "Level Up!";
        if (levelText) levelText.text = $"Level {newLevel}";

        // clear old
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) Destroy(spawned[i].gameObject);
        spawned.Clear();

        // spawn lines
        if (listRoot && lineTemplate)
        {
            foreach (var kv in gains)
            {
                var line = Instantiate(lineTemplate, listRoot);
                line.gameObject.SetActive(true);
                line.text = $"+{FormatVal(kv.Value)} {kv.Key}";
                spawned.Add(line);
            }
        }

        if (sfx && levelUpClip) sfx.PlayOneShot(levelUpClip);

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private static string FormatVal(float v) =>
        Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.0");

    private System.Collections.IEnumerator FadeRoutine()
    {
        float t = 0f, d = Mathf.Max(0.0001f, fadeInDuration);
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            if (!canvasGroup) yield break;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / d);
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(showDuration);

        t = 0f; d = Mathf.Max(0.0001f, fadeOutDuration);
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            if (!canvasGroup) yield break;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / d);
            yield return null;
        }

        if (this) Destroy(gameObject);
    }
}
