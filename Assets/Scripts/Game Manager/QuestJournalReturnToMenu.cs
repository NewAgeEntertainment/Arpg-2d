using UnityEngine;

public class QuestJournalReturnToMenu : MonoBehaviour
{
    private void OnDisable()
    {
        // Journal was closed/hidden; reopen your main menu UI if desired
        if (UI.Instance != null)
        {
            // If you want to land on the main menu:
            UI.Instance.OpenMainMenuDirect();
        }
    }
}
