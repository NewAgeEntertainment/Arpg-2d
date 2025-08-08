using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class SaveSlotUI : MonoBehaviour
{
    public int slotID; // 0-based index
    public TextMeshProUGUI slotLabel;
    public TextMeshProUGUI slotInfo;
    public Button saveButton;
    public Button loadButton;

    private void Start()
    {
        UpdateSlotUI();
    }

    public void UpdateSlotUI()
    {
        slotLabel.text = $"Slot {slotID + 1}";

        if (SaveSystem.HasSavedGameInSlot(slotID))
        {
            slotInfo.text = "Saved Game Available";
        }
        else
        {
            slotInfo.text = "Empty Slot";
        }
    }

    public void SaveGame()
    {
        SaveSystem.SaveToSlot(slotID);
        UpdateSlotUI();
    }

    public void LoadGame()
    {
        if (SaveSystem.HasSavedGameInSlot(slotID))
        {
            SaveSystem.LoadFromSlot(slotID);
        }
        else
        {
            Debug.LogWarning($"[SaveSlotUI] No save found in slot {slotID}");
        }
    }
}
