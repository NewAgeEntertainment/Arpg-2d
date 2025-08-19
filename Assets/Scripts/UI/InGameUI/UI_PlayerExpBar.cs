using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UI_PlayerExpBar : MonoBehaviour
{
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Display")]
    [Tooltip("If ON, slider is 0..1 and we feed it a ratio. If OFF, slider max = next level EXP and value = current EXP.")]
    [SerializeField] private bool useNormalizedSlider = true;

    [Tooltip("Format numbers without decimals.")]
    [SerializeField] private bool wholeNumbers = true;

    [Header("Animation")]
    [SerializeField] private bool animateFill = true;
    [SerializeField] private float fillDuration = 0.2f;

    private Coroutine _anim;

    private void Awake()
    {
        if (!expSlider) return;

        if (useNormalizedSlider)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.wholeNumbers = false;
        }
        else
        {
            expSlider.wholeNumbers = wholeNumbers;
        }
    }

    /// <summary>
    /// Update the EXP bar instantly.
    /// </summary>
    public void UpdateExp(float currentExp, float nextLevelExp)
    {
        nextLevelExp = Mathf.Max(0.0001f, nextLevelExp); // avoid division by zero
        float ratio = Mathf.Clamp01(currentExp / nextLevelExp);

        if (expSlider)
        {
            if (useNormalizedSlider)
            {
                expSlider.value = ratio;
            }
            else
            {
                expSlider.maxValue = nextLevelExp;
                expSlider.value = Mathf.Clamp(currentExp, 0f, nextLevelExp);
            }
        }

        if (expText)
        {
            if (wholeNumbers)
                expText.text = $"{Mathf.FloorToInt(currentExp)} / {Mathf.FloorToInt(nextLevelExp)}";
            else
                expText.text = $"{currentExp:0.##} / {nextLevelExp:0.##}";
        }
    }

    /// <summary>
    /// Update the EXP bar with a short animation.
    /// </summary>
    public void UpdateExpAnimated(float currentExp, float nextLevelExp)
    {
        nextLevelExp = Mathf.Max(0.0001f, nextLevelExp);
        float targetRatio = Mathf.Clamp01(currentExp / nextLevelExp);

        if (!expSlider)
        {
            UpdateExp(currentExp, nextLevelExp);
            return;
        }

        if (!animateFill)
        {
            UpdateExp(currentExp, nextLevelExp);
            return;
        }

        if (!useNormalizedSlider)
        {
            // Absolute mode: animate value in absolute space.
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateSlider(expSlider.value, Mathf.Clamp(currentExp, 0f, nextLevelExp), nextLevelExp));
        }
        else
        {
            // Normalized mode: animate 0..1
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateSlider(expSlider.value, targetRatio, 1f));
        }

        // Update label immediately (or also animate the number if you prefer)
        if (expText)
        {
            if (wholeNumbers)
                expText.text = $"{Mathf.FloorToInt(currentExp)} / {Mathf.FloorToInt(nextLevelExp)}";
            else
                expText.text = $"{currentExp:0.##} / {nextLevelExp:0.##}";
        }
    }

    private IEnumerator AnimateSlider(float from, float to, float maxForAbsoluteMode)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, fillDuration);

        // Ensure slider bounds are correct for mode
        if (useNormalizedSlider)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
        }
        else
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = maxForAbsoluteMode;
        }

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            expSlider.value = Mathf.Lerp(from, to, k);
            yield return null;
        }
        expSlider.value = to;
        _anim = null;
    }
}


