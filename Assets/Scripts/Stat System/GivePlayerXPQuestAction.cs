using UnityEngine;
using PixelCrushers;
using PixelCrushers.QuestMachine;

public class GivePlayerExperienceQuestAction : QuestAction
{
    public enum ExpType
    {
        Normal,
        Sex
    }

    [Tooltip("Which type of EXP to give.")]
    public ExpType expType = ExpType.Normal;

    [Tooltip("Amount of EXP to give. Can be constant or driven by counters/variables.")]
    public QuestNumber amount = new QuestNumber();

    public override string GetEditorName()
    {
        var value = amount.GetValue(quest);
        string kind = expType == ExpType.Normal ? "EXP" : "Sex EXP";
        return $"Give {value} {kind} to player";
    }

    public override void Execute()
    {
        base.Execute();

        float value = amount.GetValue(quest);
        if (value <= 0f) return;

        // Find Player_Stats in the scene.
        var playerStats = Object.FindFirstObjectByType<Player_Stats>(FindObjectsInactive.Include);
        if (playerStats == null)
        {
            Debug.LogWarning("[GivePlayerExperienceQuestAction] No Player_Stats found in scene.");
            return;
        }

        switch (expType)
        {
            case ExpType.Normal:
                playerStats.AddEXP(value);
                Debug.Log($"[GivePlayerExperienceQuestAction] Gave {value} EXP to {playerStats.name}.");
                break;

            case ExpType.Sex:
                playerStats.AddSexEXP(value);
                Debug.Log($"[GivePlayerExperienceQuestAction] Gave {value} Sex EXP to {playerStats.name}.");
                break;
        }
    }
}
