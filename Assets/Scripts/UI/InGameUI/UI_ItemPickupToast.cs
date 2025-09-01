using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UI_ItemPickupToast : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float hold = 1.25f;
    [SerializeField] private float fadeOut = 0.35f;

    [Header("Motion")]
    [SerializeField] private float riseDistance = 24f;   // px
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rt;
    private Vector2 startPos;

    private void Awake()
    {
        rt = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        startPos = rt.anchoredPosition;
    }

    public void Setup(Sprite icon, string itemName, int amount)
    {
        if (iconImage) iconImage.sprite = icon;
        if (labelText) labelText.text = amount > 1 ? $"{itemName} x{amount}" : itemName;
    }

    private void OnEnable()
    {
        // reset for reuse
        if (canvasGroup) canvasGroup.alpha = 0f;
        if (rt) startPos = rt.anchoredPosition;
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        // Fade in + slight rise
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeIn);
            if (canvasGroup) canvasGroup.alpha = k;
            if (rt) rt.anchoredPosition = startPos + Vector2.up * (riseDistance * 0.25f * ease.Evaluate(k));
            yield return null;
        }

        // Hold
        float h = 0f;
        while (h < hold)
        {
            h += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out + further rise
        t = 0f;
        Vector2 pos0 = rt ? rt.anchoredPosition : Vector2.zero;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOut);
            if (canvasGroup) canvasGroup.alpha = 1f - k;
            if (rt) rt.anchoredPosition = pos0 + Vector2.up * (riseDistance * 0.75f * ease.Evaluate(k));
            yield return null;
        }

        gameObject.SetActive(false); // for pooling; safe if instantiated too
        Destroy(gameObject, 0.1f);   // remove if you build a pool
    }
}
