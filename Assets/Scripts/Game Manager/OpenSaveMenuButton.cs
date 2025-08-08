using UnityEngine;

public class OpenSaveMenuButton : MonoBehaviour
{
    public UI_SaveLoadPanel saveMenu;

    public void Open()
    {
        saveMenu.OpenPanel();
    }

    public void Close()
    {
        saveMenu.ClosePanel();
    }
}
