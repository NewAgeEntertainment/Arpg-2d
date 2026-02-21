using UnityEngine;
using UnityEngine.EventSystems;

public class KeepUISelectionAfterMouseClick : MonoBehaviour
{
    private GameObject lastSelected;

    void Update()
    {
        var es = EventSystem.current;
        if (es == null) return;

        if (es.currentSelectedGameObject != null)
            lastSelected = es.currentSelectedGameObject;

        // If a click clears selection, restore it so controller navigation still works.
        if (Input.GetMouseButtonDown(0) && es.currentSelectedGameObject == null && lastSelected != null)
            es.SetSelectedGameObject(lastSelected);
    }
}
