using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_LevelUpPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI statGainText;
    [SerializeField] private float displayDuration = 2f;

    public void ShowPopup(int newLevel, Dictionary<string, float> statGains)
    {
        gameObject.SetActive(true);

        titleText.text = "Level Up!";
        levelText.text = $"Level {newLevel}";

        statGainText.text = "";
        foreach (var pair in statGains)
        {
            statGainText.text += $"{pair.Key} +{pair.Value}\n";
        }

        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        gameObject.SetActive(false);
    }
}

