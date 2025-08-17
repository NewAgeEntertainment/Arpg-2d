using UnityEngine;

public class Skill_Thrust : Skill_Base
{
    protected override void Awake()
    {
        base.Awake();
        // Thrust should be usable by default unless you gate it by the tree
        ForceUnlock(true);
    }

    public void OnStartEffect() { }
    public void OnEndEffect() { }
}
