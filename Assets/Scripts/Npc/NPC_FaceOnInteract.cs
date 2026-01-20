using UnityEngine;

public class NPC_FaceOnInteract : MonoBehaviour
{
    private NPC npc;

    private void Awake()
    {
        npc = GetComponent<NPC>();
    }

    public void OnSelect(Transform actor)
    {
        npc?.OnSelect(actor);
    }

    public void OnUse(Transform actor)
    {
        npc?.OnUse(actor);
    }

    public void OnDeselect()
    {
        npc?.OnDeselect();
    }

    // Dialogue System messages:
    public void OnConversationStart(Transform actor)
    {
        npc?.OnConversationStart(actor);
    }

    public void OnConversationEnd(Transform actor)
    {
        npc?.OnConversationEnd(actor);
    }
}
