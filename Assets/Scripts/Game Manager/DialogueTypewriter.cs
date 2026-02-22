using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using rewired = Rewired;

public class DialogueTypewriter : MonoBehaviour
{
    public static DialogueTypewriter Instance;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image image;

    [SerializeField] private float waitBetweenLetters;
    [SerializeField] private float waitBetweenSentences;
    [SerializeField] private float waitTimeAfterLastSentence;

    private bool isInDialogue;
    public delegate void DialogueCallBackDelegate();
    private float currentWaitBetweenLetters;

    public bool IsInDialogue => isInDialogue;

    [SerializeField] private int playerID = 0;
    private rewired.Player player;

    // NEW:
    private bool goToNextLine = false;
    private bool isCurrentlyTyping = false;
    private bool _skipToEndOfLine = false;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnhookInput();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
            return;
        }

        ResetDialogueTypewriter();
    }

    private void Update()
    {
        if (!isInDialogue) return;

        if (player != null && player.GetButtonDown("Interact"))
            SetGoToNextLineTrue();
    }

    void PressGoToNextLineWithKey(rewired.InputActionEventData data)
    {
        SetGoToNextLineTrue();
        // NOTE: calling GetButtonDown here doesn't "consume" input; it's harmless but not needed.
    }

    public void StartDialogue(NPC_Dialogue dialogue, DialogueCallBackDelegate callback)
    {
        if (IsInDialogue)
        {
            Debug.Log("Can't start dialogue: one is already running.");
            return;
        }

        player = rewired.ReInput.players.GetPlayer(playerID);
        player.AddInputEventDelegate(
            PressGoToNextLineWithKey,
            rewired.UpdateLoopType.Update,
            rewired.InputActionEventType.ButtonJustPressed,
            "Interact"
        );

        ResetDialogueTypewriter();
        dialoguePanel.SetActive(true);

        isInDialogue = true;
        StartCoroutine(Typing(dialogue.DialogueSequence, callback));
    }

    public void ResetDialogueTypewriter()
    {
        if (dialogueText != null) dialogueText.text = "";
        goToNextLine = false;
        isCurrentlyTyping = false;
        _skipToEndOfLine = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    public void SetGoToNextLineTrue()
    {
        if (isCurrentlyTyping)
        {
            // Finish the current sentence instantly
            currentWaitBetweenLetters = 0f;
            _skipToEndOfLine = true;
        }
        else
        {
            // Advance to the next sentence / close at the end
            goToNextLine = true;
        }
    }

    private void UnhookInput()
    {
        if (player == null) return;
        player.RemoveInputEventDelegate(PressGoToNextLineWithKey);
    }

    IEnumerator Typing(List<NPC_Dialogue.DialogueParams> dialogueSequence, DialogueCallBackDelegate callback)
    {
        goToNextLine = false;
        bool hasFirstSentencePassed = false;

        foreach (var part in dialogueSequence)
        {
            currentWaitBetweenLetters = waitBetweenLetters;

            // Between sentences: REQUIRE press to continue (no timers)
            if (hasFirstSentencePassed)
            {
                // Wait for press to move to next line
                goToNextLine = false;
                yield return new WaitUntil(() => goToNextLine);
                goToNextLine = false;

                // Clear text for the next line
                if (dialogueText != null) dialogueText.text = "";
            }

            // Header / portrait
            if (nameText != null) nameText.text = $"[{part.npcName}]";

            if (image != null)
            {
                if (part.npcImage != null)
                {
                    image.gameObject.SetActive(true);
                    image.sprite = part.npcImage;
                }
                else image.gameObject.SetActive(false);
            }

            // Type / skip line
            isCurrentlyTyping = true;
            _skipToEndOfLine = false;

            string line = part.dialogueSentence ?? "";

            for (int i = 0; i < line.Length; i++)
            {
                if (_skipToEndOfLine)
                {
                    if (dialogueText != null) dialogueText.text = line; // dump full line
                    _skipToEndOfLine = false;
                    break;
                }

                if (dialogueText != null) dialogueText.text += line[i];
                yield return new WaitForSeconds(currentWaitBetweenLetters);
            }

            isCurrentlyTyping = false;
            hasFirstSentencePassed = true;
        }

        // ✅ No more sentences: REQUIRE press to close/return
        goToNextLine = false;
        yield return new WaitUntil(() => goToNextLine);
        goToNextLine = false;

        // Close panel cleanly
        isInDialogue = false;
        UnhookInput();

        if (dialogueText != null) dialogueText.text = "";
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        callback?.Invoke();
    }
}