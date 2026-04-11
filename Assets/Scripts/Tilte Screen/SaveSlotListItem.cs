using UnityEngine;
using UnityEngine.UI;

public class SaveSlotListItem : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    private int slot;

    public void Bind(int slotNumber, System.Action<int> onLoad, System.Action<int> onDelete)
    {
        slot = slotNumber;

        bool exists = PlayerPrefs.GetInt($"SaveSlot_{slot}_exists", 0) == 1;
        string sceneDisplay = PlayerPrefs.GetString($"SaveSlot_{slot}_sceneDisplay", "Unknown");
        string time = PlayerPrefs.GetString($"SaveSlot_{slot}_time", "-");
        int playSeconds = PlayerPrefs.GetInt($"SaveSlot_{slot}_playSeconds", 0);

        string playText = FormatPlayTime(playSeconds);

        if (label != null)
        {
            label.text = exists
                ? $"Slot {slot + 1} — {sceneDisplay}\nTime played: {playText}\nSaved: {time}"
                : $"Slot {slot + 1} — Empty";
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(() => onLoad?.Invoke(slot));
            loadButton.interactable = exists;
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(slot));
            deleteButton.interactable = exists;
        }
    }

    private string FormatPlayTime(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}