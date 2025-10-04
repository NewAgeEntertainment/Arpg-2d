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

    // ---------- Added helpers/aliases (nothing removed) ----------

    /// <summary>
    /// Alias so you can call QuestTurnIn() directly from Dialogue/Buttons.
    /// Example (Dialogue System Sequence): SendMessage(Dialogue Manager, QuestTurnIn)
    /// </summary>
    public void QuestTurnIn()
    {
        SendTurnIn();
    }

    /// <summary>
    /// Same as QuestTurnIn but lets you supply a parameter (e.g., specific quest ID).
    /// </summary>
    public void QuestTurnInParam(string param)
    {
        MessageSystem.SendMessage(this, message, param ?? "");
    }

    /// <summary>
    /// Send using a custom message and/or parameter at callsite.
    /// </summary>
    public void SendTurnInWith(string customMessage, string customParameter = "")
    {
        MessageSystem.SendMessage(
            this,
            string.IsNullOrEmpty(customMessage) ? message : customMessage,
            string.IsNullOrEmpty(customParameter) ? parameter : customParameter
        );
    }

    /// <summary>
    /// Fire after a small delay (handy from animations/timeline).
    /// </summary>
    public void SendTurnInDelayed(float seconds)
    {
        if (seconds <= 0f) { SendTurnIn(); }
        else { StartCoroutine(Delayed(seconds)); }
    }

    private System.Collections.IEnumerator Delayed(float t)
    {
        yield return new WaitForSeconds(t);
        SendTurnIn();
    }

    /// <summary>
    /// Static convenience if you need to trigger from code without a component instance.
    /// </summary>
    public static void Fire(string msg, string param = "")
    {
        MessageSystem.SendMessage(null, msg, param);
    }
}
