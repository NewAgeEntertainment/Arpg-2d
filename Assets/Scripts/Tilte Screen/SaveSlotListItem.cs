// Assets/Scripts/Title/SaveSlotListItem.cs
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;

public class SaveSlotListItem : MonoBehaviour
{
    [SerializeField] private Text label;    // e.g. "Slot 3 — Scene: Town"
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    private int slot;

    public void Bind(int slotNumber, System.Action<int> onLoad, System.Action<int> onDelete)
    {
        slot = slotNumber;
        string sceneName = "?";
        try
        {
            // We can peek the SavedGameData to show scene name:
            var data = SaveSystem.storer.RetrieveSavedGameData(slot);
            if (data != null && !string.IsNullOrEmpty(data.sceneName)) sceneName = data.sceneName;
        }
        catch { /* storer may be async-backed; Retrieve is sync here */ }

        if (label != null) label.text = $"Slot {slot} — Scene: {sceneName}";

        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(() => onLoad?.Invoke(slot));
        }
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(slot));
        }
    }
}
