using UnityEngine;
using Rewired;

public class UI_BasePanel : MonoBehaviour
{
    [Header("Rewired Input")]
    [SerializeField] protected int playerID = 0;
    [SerializeField] protected string cancelAction = "Cancel";

    protected Rewired.Player rPlayer;

    protected virtual void Start()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
    }

    protected void CheckCancelInput()
    {
        if (rPlayer != null && rPlayer.GetButtonDown(cancelAction))
        {
            HandleCancel();
        }
    }

    // To be overridden by child panels
    public virtual bool HandleCancel()
    {
        Debug.Log("[UI_BasePanel] Cancel input detected but HandleCancel not overridden.");
        return false;
    }
}
