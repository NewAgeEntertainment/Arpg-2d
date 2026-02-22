using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ChochosanStudios/Dialogues/CreateDialogueSequence", order = 1)]
public class NPC_Dialogue : ScriptableObject
{
    [SerializeField] private List<DialogueParams> dialogueSequence;

    public List<DialogueParams> DialogueSequence => dialogueSequence;

    [System.Serializable]
    public class DialogueParams
    {
        public string dialogueSentence;
        public string npcName;
        public Sprite npcImage;
    }
}
