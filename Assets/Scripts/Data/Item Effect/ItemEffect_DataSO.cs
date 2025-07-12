using UnityEngine;

public abstract class ItemEffect_DataSO : ScriptableObject
{
    [TextArea] public string effectDescription;

    // TEMPORARY reference if needed (used by Subscribe/Unsubscribe systems)
    protected Player player;

    public virtual bool CanBeUsed(Player player) => true;

    // ✳️ NEW: Use this for most item usage
    public virtual void ExecuteEffect(Player target)
    {
        this.player = target;
    }

    public virtual void Subscribe(Player player) => this.player = player;
    public virtual void Unsubscribe() => player = null;
}
