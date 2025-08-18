// Assets/Scripts/Save System/SkillTreeSaver.cs
using PixelCrushers;
using UnityEngine;

[DisallowMultipleComponent]
public class SkillTreeSaver : Saver
{
    [SerializeField] private UI_SkillTree skillTree;

    private void Awake()
    {
        if (skillTree == null)
            skillTree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);
    }

    public override string RecordData()
    {
        if (skillTree == null)
        {
            skillTree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);
            if (skillTree == null) return string.Empty;
        }

        var state = skillTree.CreateSaveState();
        return SaveSystem.Serialize(state);
    }

    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;

        var state = SaveSystem.Deserialize<UI_SkillTree.SkillTreeState>(s);
        if (state == null) return;

        if (skillTree == null)
            skillTree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);

        // If the tree exists now, apply directly; otherwise queue it to load when UI_SkillTree comes alive.
        if (skillTree != null)
        {
            // Ensure Start/UnlockDefault ran before applying; if you're calling from a bootstrap scene, you can
            // start a coroutine to wait one frame here. Usually ApplyData is called after Start by SaveSystem.
            skillTree.ApplySaveState(state);
        }
        else
        {
            UI_SkillTree.SetPendingState(state);
        }
    }
}
