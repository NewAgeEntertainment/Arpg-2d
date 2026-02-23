using UnityEngine;

public class SexyTimeHotbar : MonoBehaviour
{
    [Tooltip("This bar only accepts Sex-category skills.")]
    public SkillCategory category = SkillCategory.Sex;

    [SerializeField] private UI_SkillSlot[] slots;
    public UI_SkillSlot[] Slots => slots;
    public int SlotCount => slots != null ? slots.Length : 0;

    // --------- Helpers ---------

    private bool SameSkill(Skill_DataSO a, Skill_DataSO b)
    {
        if (a == null || b == null) return false;
        if (!string.IsNullOrEmpty(a.id) && !string.IsNullOrEmpty(b.id)) return a.id == b.id;
        return ReferenceEquals(a, b);
    }

    private int FindExistingIndex(Skill_DataSO data)
    {
        if (data == null || slots == null) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null || !s.HasSkill) continue;
            if (SameSkill(s.Data, data)) return i;
        }
        return -1;
    }

    private int FindFirstEmptyIndex()
    {
        if (slots == null) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s != null && !s.HasSkill) return i;
        }
        return -1;
    }

    // --------- Public API ---------

    /// Assign the skill into the first empty slot.
    /// If it's already on the bar, do nothing (success).
    /// If full, do nothing (fail).
    public bool TryAssign(Skill_DataSO data)
    {
        if (data == null || data.category != SkillCategory.Sex) return false;

        // Already present? treat as success (don’t duplicate)
        if (FindExistingIndex(data) != -1) return true;

        int empty = FindFirstEmptyIndex();
        if (empty == -1) return false; // full -> don’t overwrite players

        return TryAssignToIndex(empty, data);
    }

    /// Assign to a specific index. If skill exists elsewhere, move it here (no duplicates).
    public bool TryAssignToIndex(int index, Skill_DataSO data)
    {
        if (data == null || data.category != SkillCategory.Sex) return false;
        if (slots == null || index < 0 || index >= slots.Length) return false;

        // If skill exists in another slot, clear that slot first (move behavior)
        int existing = FindExistingIndex(data);
        if (existing != -1 && existing != index)
        {
            var oldSlot = slots[existing];
            if (oldSlot != null) oldSlot.ClearSlot();
        }

        var s = slots[index];
        if (s == null) return false;

        return s.SetupSkillSlot(data);
    }

    public int IndexOf(UI_SkillSlot slot)
    {
        if (slot == null || slots == null) return -1;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == slot) return i;
        return -1;
    }

    public void ClearAll()
    {
        if (slots == null) return;
        foreach (var s in slots)
            if (s != null) s.ClearSlot();
    }
}