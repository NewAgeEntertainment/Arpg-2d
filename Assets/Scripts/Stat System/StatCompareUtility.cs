public static class StatCompareUtility
{
    private const float EPSILON = 0.0001f;

    public struct StatCompareResult
    {
        public float current;
        public float projected;
        public float delta;
    }

    /// <summary>
    /// Compares the player's current final value for a stat against what it would be
    /// if the hovered item replaced the currently equipped item in the same slot.
    /// </summary>
    public static StatCompareResult CompareStat(
        Player_Stats playerStats,
        Inventory_Player playerInventory,
        Inventory_Item newItem,
        StatType statType)
    {
        var result = new StatCompareResult();

        float currentWithAllGear = playerStats != null
            ? playerStats.GetStatByType(statType)?.GetValue() ?? 0f
            : 0f;

        result.current = currentWithAllGear;

        // What’s currently equipped in the same slot?
        Inventory_Item equippedItem = playerInventory != null
            ? playerInventory.GetEquippedItemByType(newItem.itemData.itemType)
            : null;

        // Remove the equipped item’s contribution to get a baseline
        float equippedModifier = GetTotalModifierFor(equippedItem, statType);
        float baselineWithoutThisSlot = currentWithAllGear - equippedModifier;

        // Add the new item’s contribution
        float newItemModifier = GetTotalModifierFor(newItem, statType);
        float projectedWithNew = baselineWithoutThisSlot + newItemModifier;

        result.projected = projectedWithNew;
        result.delta = newItemModifier - equippedModifier;

        return result;
    }

    /// <summary>
    /// Sums all modifiers the item contributes to the given statType.
    /// Works with both item.modifiers[] and itemData.itemModifiers (List).
    /// </summary>
    public static float GetTotalModifierFor(Inventory_Item item, StatType statType)
    {
        float total = 0f;
        if (item == null) return 0f;

        if (item.modifiers != null)
        {
            foreach (var mod in item.modifiers)
                if (mod.statType == statType)
                    total += mod.value;
        }

        if (item.itemData?.itemModifiers != null)
        {
            foreach (var mod in item.itemData.itemModifiers)
                if (mod.statType == statType)
                    total += mod.value;
        }

        return total;
    }

    public static string ArrowDelta(float delta)
    {
        if (delta > EPSILON) return "<color=green>▲</color>";
        if (delta < -EPSILON) return "<color=red>▼</color>";
        return "-";
    }

    public static string DeltaText(float delta, bool percent = false)
    {
        if (delta > EPSILON)
            return percent ? $" <color=green>(+{delta:0.##}%)</color>" : $" <color=green>(+{delta:0.##})</color>";
        if (delta < -EPSILON)
            return percent ? $" <color=red>({delta:0.##}%)</color>" : $" <color=red>({delta:0.##})</color>";
        return "";
    }

    public static string AsValue(float v, bool percent = false)
        => percent ? $"{v:0.##}%" : $"{v:0.##}";
}
