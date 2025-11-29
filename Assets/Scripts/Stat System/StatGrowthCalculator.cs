using UnityEngine;

public static class StatGrowthCalculator
{
    public static int GetMaxHealth(int level) => 10 + (level * 0);
    public static int GetMaxMana(int level) => 50 + (level * 6);
    public static int GetStrength(int level) => 7 + (level * 2);
    public static int GetDefense(int level) => 3 + (level * 2);
    public static int GetIntelligence(int level) => 5 + (level * 2);
    public static int GetLuck(int level) => 1 + (level / 5);
    public static int GetVitality(int level) => 3 + (level * 1);

    // 🔞 Sex-related stats
    public static int GetStroke(int level) => 2 + (level / 2); // Level 10 = +7
    public static int GetResilience(int level) => 1 + (level / 3);
    public static int GetSexualDamage(int level) => 5 + (level * 1);
    public static int GetMaxArousal(int level) => 50 + (level * 5);
    public static int GetSexualRestraint(int level) => 2 + (level / 4);
}


