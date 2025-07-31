using UnityEngine;

public abstract class UI_Panel : MonoBehaviour
{
    // Called when ESC or Back is triggered
    public virtual bool HandleCancel()
    {
        Debug.Log($"{this.GetType().Name} HandleCancel not overridden.");
        return false;  // Return false if not handled
    }
}
