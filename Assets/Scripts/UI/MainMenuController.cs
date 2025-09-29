using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Rewired;

public class MainMenuController : MonoBehaviour
{
    [Header("Order the buttons as you want the d-pad/arrow navigation to move")]
    [SerializeField] private List<Selectable> menuOrder;   // Buttons or other Selectables in visual order
    [SerializeField] private Selectable firstSelected;      // Fallback if we have no last selection
    [SerializeField] private int rewiredPlayerId = 0;       // Usually 0

    private Rewired.Player rplayer;
    private GameObject lastSelectedGO;

    private void OnEnable()
    {
        // Rewired player for Cancel/Submit etc.
        rplayer = ReInput.players.GetPlayer(rewiredPlayerId);

        // Wire explicit nav so the d-pad/left stick behaves predictably
        UI.WireLinearNav(menuOrder.ToArray(), horizontal: false); // vertical list (change to true for a row)

        // Restore last focus or select the fallback
        var target = lastSelectedGO ?? (firstSelected != null ? firstSelected.gameObject : null);
        if (target != null) EventSystem.current?.SetSelectedGameObject(target);
    }

    private void OnDisable()
    {
        // Clear selected so we don’t “stick” to a disabled object
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            lastSelectedGO = EventSystem.current.currentSelectedGameObject;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (rplayer == null) return;

        // Remember wherever the user moved selection
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            lastSelectedGO = EventSystem.current.currentSelectedGameObject;

        // Back / Cancel closes or goes back
        if (rplayer.GetButtonDown("UICancel"))
        {
            // Whatever “back” means in your app:
            // Close this panel and return to gameplay or previous menu
            gameObject.SetActive(false);
            UI.Instance.HandleBackAction(); // or a custom method
        }
    }
}
