using System.Collections;
using TMPro;
using UnityEngine;

public class UI_StatToolTip : UI_ToolTip
{
    private Player_Stats playerStats;
    private TextMeshProUGUI statToolTipText;

    protected override void Awake()
    {
        base.Awake();
        playerStats = FindAnyObjectByType<Player_Stats>();
        statToolTipText = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Show tooltip comparing equipment stat with player's current stat.
    /// </summary>
    /// <param name="show"></param>
    /// <param name="targetRect"></param>
    /// <param name="statType"></param>
    /// <param name="newItemValue"></param>
    public void ShowToolTip(bool show, RectTransform targetRect, StatType statType)
    {
        base.ShowToolTip(show, targetRect);
        statToolTipText.text = GetStatTextByType(statType);
    }

    /// <summary>
    /// Builds one line showing: Stat Name: Value (colored)
    /// </summary>
    private string BuildColoredStatLine(StatType type, float newValue, float currentValue)
    {
        string colorTag;

        if (newValue > currentValue)
            colorTag = "#00FF00"; // Green
        else if (newValue < currentValue)
            colorTag = "#FF0000"; // Red
        else
            colorTag = "#FFFFFF"; // White

        return $"{type}: <color={colorTag}>{newValue}</color> (Current: {currentValue})";
    }

    public string GetStatTextByType(StatType type)
    {
        // ⏬ same as your original switch — keep your detailed descriptions here.
        switch (type)
        {
            case StatType.Strength:
                return "Increases physical damage by 1 per point.\nIncreases physical damage by 0.5% per point.";
            case StatType.Luck:
                return "Increases critical chance by 0.3% per point.\nIncreases evasion by 0.5% per point.";
            // ➜ and so on ...
            default:
                return "No tooltip available for this stat.";
        }
    }
}
