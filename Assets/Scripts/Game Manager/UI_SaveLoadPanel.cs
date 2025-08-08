using UnityEngine;

public class UI_SaveLoadPanel : MonoBehaviour
{
    public SaveSlotUI[] saveSlots;
    public GameObject panel;

    public void OpenPanel()
    {
        panel.SetActive(true);
        RefreshAllSlots();
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
    }

    public void RefreshAllSlots()
    {
        foreach (var slot in saveSlots)
        {
            slot.UpdateSlotUI();
        }
    }
}
