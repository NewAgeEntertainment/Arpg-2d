using UnityEngine;
using PixelCrushers;
using PixelCrushers.QuestMachine.Wrappers; // QuestGiver wrapper

[DisallowMultipleComponent]
[RequireComponent(typeof(QuestGiver))]
public class QuestGiverDialogueOpener : MonoBehaviour, IMessageHandler
{
    [Header("Open via Message (optional)")]
    [SerializeField] private string listenForMessage = "QM_OpenGiverDialogue";
    [Tooltip("If set, only open when the message's parameter matches this (case-insensitive).")]
    [SerializeField] private string questIdFilter = "";

    private QuestGiver giver;

    private void Awake()
    {
        giver = GetComponent<QuestGiver>();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(listenForMessage))
            MessageSystem.AddListener(this, listenForMessage, (string)null); // cast removes ambiguity
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(listenForMessage))
            MessageSystem.RemoveListener(this, listenForMessage, (string)null);
    }


    // IMessageHandler implementation. MessageArgs is a struct (never null).
    public void OnMessage(MessageArgs args)
    {
        // If you set a filter, require parameter match:
        if (!string.IsNullOrEmpty(questIdFilter))
        {
            var param = args.parameter as string;
            if (!string.Equals(questIdFilter, param, System.StringComparison.OrdinalIgnoreCase))
                return;
        }

        giver?.StartDialogueWithPlayer();
    }

    // Hook this in a Usable's On Use, or call from Sequencer/Animation events:
    public void OnUse() => giver?.StartDialogueWithPlayer();

    // Convenience if you want to trigger from code:
    public void OpenNow() => giver?.StartDialogueWithPlayer();
}
