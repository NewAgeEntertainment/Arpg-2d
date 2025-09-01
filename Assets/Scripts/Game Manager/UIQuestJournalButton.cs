using UnityEngine;

public class UIQuestJournalButton : MonoBehaviour
{
    // Hook this to your Unity UI Button's OnClick
    public void OnClick()
    {
        if (UI.Instance != null)
            UI.Instance.ToggleQuestJournalFromUI();
    }
}
