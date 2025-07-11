using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ManaBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    public void UpdateHealth(float current, float max)
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = max;
            manaSlider.value = current;
        }

        if (manaText != null)
        {
            manaText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }
}

