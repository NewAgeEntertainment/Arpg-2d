using UnityEngine;

public class OpenSaveMenuButton : MonoBehaviour
{
    public UI_SaveLoadPanel saveMenu;

    public void Open()
    {
        saveMenu.OpenForSave();
    }

    public void Close()
    {
        saveMenu.ClosePanel();
    }
}
