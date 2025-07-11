using Rewired;
using UnityEngine;
using UnityEngine.Events;


public class UI_CloseButton : MonoBehaviour
{
    [Header("Rewired")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string rewiredActionName = "Back";

    [Header("Button Action")]
    public UnityEvent onPress;

    private Rewired.Player player;

    private void Awake()
    {
        player = ReInput.players.GetPlayer(playerID);
    }

    private void Update()
    {
        if (player == null) return;

        if (player.GetButtonDown(rewiredActionName))
        {
            Trigger();
        }
    }

    /// <summary>
    /// This can be called by UI Button OnClick() too.
    /// </summary>
    public void Trigger()
    {
        if (onPress != null)
        {
            onPress.Invoke();
        }
    }
}
