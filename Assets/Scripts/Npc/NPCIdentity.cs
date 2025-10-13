using UnityEngine;

[DisallowMultipleComponent]
public class NPCIdentity : MonoBehaviour
{
    [Tooltip("ID used from Dialogue (Lua). Example: NPCFollowStart(\"Elaina\")")]
    public string id;
}

