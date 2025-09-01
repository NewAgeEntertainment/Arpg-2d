// QM_JournalBridge.cs
using UnityEngine;
using PixelCrushers.QuestMachine.Wrappers; // for UnityUIQuestJournalUI

public class QM_JournalBridge : MonoBehaviour
{
    [SerializeField] private UnityUIQuestJournalUI journalUI;
    [SerializeField] private UI ui;

    private void Reset()
    {
        if (journalUI == null) journalUI = GetComponent<UnityUIQuestJournalUI>();
        if (ui == null) ui = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
    }

    //private void OnEnable()
    //{
    //    // Journal opened
    //    if (ui != null) ui.OnQuestJournalOpenedFromOutside();
    //}

    //private void OnDisable()
    //{
    //    // Journal closed (via X/Close button or code)
    //    if (ui != null) ui.OnQuestJournalClosedFromOutside();
    //}
}
