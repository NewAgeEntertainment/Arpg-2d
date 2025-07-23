using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerExpBar : MonoBehaviour
{
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    public void UpdateExp(float currentExp, float nextLevelExp)
    {
        if (expSlider != null)
            expSlider.value = currentExp / nextLevelExp;

        if (expText != null)
            expText.text = $"{currentExp} / {nextLevelExp}";
    }
}

