using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Status : MonoBehaviour
{
    public bool HandleCancel()
    {
        // Close the status panel
        gameObject.SetActive(false);

        var ui = FindObjectOfType<UI>();
        if (ui != null)
        {
            ui.OpenMainMenuDirect();
        }

        return true; // Indicate that cancel was handled
    }

}
