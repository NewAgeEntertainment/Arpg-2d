using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Rewired;

public class MainMenuController : MonoBehaviour
{
    [Header("Order the buttons as you want the d-pad/arrow navigation to move")]
    [SerializeField] private List<Selectable> menuOrder;
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private int rewiredPlayerId = 0;

    [Header("Main Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;

    [Tooltip("Assign your UI_Options panel here. This can be the same options panel or a title-screen copy.")]
    [SerializeField] private UI_Options optionsPanel;

    [Tooltip("Button/selectable to focus when returning from Options.")]
    [SerializeField] private Selectable optionsReturnSelected;

    private Rewired.Player rplayer;
    private GameObject lastSelectedGO;

    private void OnEnable()
    {
        TryCacheRewired();

        if (menuOrder != null && menuOrder.Count > 0)
            UI.WireLinearNav(menuOrder.ToArray(), horizontal: false);

        GameObject target =
            lastSelectedGO != null
                ? lastSelectedGO
                : firstSelected != null
                    ? firstSelected.gameObject
                    : null;

        if (target != null)
            EventSystem.current?.SetSelectedGameObject(target);
    }

    private void OnDisable()
    {
        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedGO = EventSystem.current.currentSelectedGameObject;
        }

        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (rplayer == null)
        {
            TryCacheRewired();
            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedGO = EventSystem.current.currentSelectedGameObject;
        }

        if (rplayer.GetButtonDown("UICancel"))
        {
            HandleCancel();
        }
    }

    public void OpenOptionsFromMainMenu()
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("[MainMenuController] Options panel is not assigned.");
            return;
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        optionsPanel.gameObject.SetActive(true);
        optionsPanel.OpenOptions();

        Debug.Log("[MainMenuController] Opened Options from Main Menu.");
    }

    public void CloseOptionsToMainMenu()
    {
        if (optionsPanel != null)
        {
            optionsPanel.gameObject.SetActive(false);
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        GameObject target =
            optionsReturnSelected != null
                ? optionsReturnSelected.gameObject
                : firstSelected != null
                    ? firstSelected.gameObject
                    : null;

        if (target != null)
            EventSystem.current?.SetSelectedGameObject(target);

        Debug.Log("[MainMenuController] Returned from Options to Main Menu.");
    }

    private void HandleCancel()
    {
        if (optionsPanel != null && optionsPanel.gameObject.activeInHierarchy)
        {
            CloseOptionsToMainMenu();
            return;
        }

        // Main menu cancel behavior.
        // Example: close panel, back to title root, or quit confirmation.
        Debug.Log("[MainMenuController] Cancel pressed on Main Menu.");
    }

    private void TryCacheRewired()
    {
        try
        {
            rplayer = ReInput.players.GetPlayer(rewiredPlayerId);
        }
        catch
        {
            rplayer = null;
        }
    }
}