using UnityEngine;

public abstract class ItemEffect_DataSO : ScriptableObject
{
    [TextArea] public string effectDescription;

    protected Player player;

    public virtual bool CanBeUsed(Player player) => true;

    public virtual void ExecuteEffect(Player target)
    {
        this.player = target;
    }

    public virtual void Subscribe(Player player) => this.player = player;
    public virtual void Unsubscribe() => player = null;

    // ✅ NEW: Indicates if the effect needs a target (like a player) to function
    public virtual bool RequiresTarget => true;
}
//public override bool RequiresTarget => false;

