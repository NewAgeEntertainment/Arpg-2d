using UnityEngine;

/// Simple container for sex-only skill slots.
public class SexyTimeHotbar : MonoBehaviour
{
    [Tooltip("This bar only accepts Sex-category skills.")]
    public SkillCategory category = SkillCategory.Sex;

    [SerializeField] private UI_SkillSlot[] slots;

    public UI_SkillSlot[] Slots => slots;

    /// Count helper (used by SexyTimeUIController)
    public int SlotCount => slots != null ? slots.Length : 0;

    /// Assign the skill into the first empty slot (or replace the first slot if all full).
    public bool TryAssign(Skill_DataSO data)
    {
        if (data == null || data.category != SkillCategory.Sex) return false;

        // First empty
        if (slots != null)
        {
            foreach (var s in slots)
            {
                if (s != null && !s.HasSkill)
                {
                    s.SetupSkillSlot(data);
                    return true;
                }
            }
        }

        // Replace first if all filled
        if (slots != null && slots.Length > 0 && slots[0] != null)
        {
            slots[0].SetupSkillSlot(data);
            return true;
        }

        return false;
    }

    /// Assign to a specific index (used by click-to-place)
    public bool TryAssignToIndex(int index, Skill_DataSO data)
    {
        if (data == null || data.category != SkillCategory.Sex) return false;
        if (slots == null || index < 0 || index >= slots.Length) return false;

        var s = slots[index];
        if (s == null) return false;
        return s.SetupSkillSlot(data);
    }

    /// Find a slot's index in this hotbar (used when the user clicks a UI_SkillSlot)
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
