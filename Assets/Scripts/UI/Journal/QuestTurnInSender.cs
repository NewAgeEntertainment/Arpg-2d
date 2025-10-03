using PixelCrushers;
using UnityEngine;

public class QuestTurnInSender : MonoBehaviour
{
    [SerializeField] private string message = "TurnIn_MyQuest";
    [SerializeField] private string parameter = ""; // optional

    public void SendTurnIn()
    {
        // Matches a Quest Machine "Message" condition with the same message (and parameter if you set one).
        MessageSystem.SendMessage(this, message, parameter);
    }
}
