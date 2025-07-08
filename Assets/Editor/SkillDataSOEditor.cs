using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Skill_DataSO))]
public class SkillDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        Skill_DataSO skillData = (Skill_DataSO)target;

        // Auto-set category based on skill type
        SkillCategory autoCategory = GetCategoryBySkillType(skillData.skillType);

        if (skillData.category != autoCategory)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Auto-assigned Category: {autoCategory} (was {skillData.category})",
                MessageType.Info
            );

            if (GUILayout.Button("Apply Auto-Category"))
            {
                skillData.category = autoCategory;
                EditorUtility.SetDirty(skillData);
                Debug.Log($"Skill '{skillData.displayName}' category set to {autoCategory}");
            }
        }
    }

    private SkillCategory GetCategoryBySkillType(SkillType type)
    {
        switch (type)
        {
            case SkillType.DeepBreath:
                return SkillCategory.Sex;

            // Add other sex skill types here if needed
            default:
                return SkillCategory.Combat;
        }
    }
}
