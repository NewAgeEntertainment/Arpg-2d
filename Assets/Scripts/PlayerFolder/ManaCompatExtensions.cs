using UnityEngine;

public static class ManaCompatExtensions
{
    public static void RestoreManaOnHitWithScaling(this Entity_Mana mana, int level)
    {
        if (mana == null) return;
        int recovery = Mathf.Min(2 + ((level / 10) * 2), 8);
        mana.IncreaseMana(recovery);
        Debug.Log($"🔋 Recovered {recovery} MP on hit (Level {level}) [compat]");
    }
}
