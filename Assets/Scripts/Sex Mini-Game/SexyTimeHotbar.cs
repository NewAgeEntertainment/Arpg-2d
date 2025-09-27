using UnityEngine;

/// Simple container for sex-only skill slots.
public class SexyTimeHotbar : MonoBehaviour
{
    [Tooltip("This bar only accepts Sex-category skills.")]
    public SkillCategory category = SkillCategory.Sex;

    [SerializeField] private UI_SkillSlot[] slots;

    public UI_SkillSlot[] Slots => slots;

    /// Assign the skill into the first empty slot (or replace the first slot if all full).
    public bool TryAssign(Skill_DataSO data)
    {
        if (data == null || data.category != SkillCategory.Sex) return false;

        // First empty
        foreach (var s in slots)
        {
            if (s != null && !s.HasSkill)
            {
                s.SetupSkillSlot(data);
                return true;
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

    public void ClearAll()
    {
        if (slots == null) return;
        foreach (var s in slots)
            if (s != null) s.ClearSlot();
    }
}
