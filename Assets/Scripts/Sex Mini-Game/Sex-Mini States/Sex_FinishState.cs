using System.Collections;
using UnityEngine;

public class Sex_FinishState : SexyTimeState
{
    public Sex_FinishState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        logic.StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        // small beat after climax ends (optional)
        yield return new WaitForSeconds(0.25f);

        var typewriter = DialogueTypewriter.Instance;
        var dialogue = logic.GetFinishDialogueForWinner();

        // Hide SexyTime UI so only dialogue is visible
        if (logic.UI != null)
            logic.UI.HideAllSexyUIForFinishDialogue();

        if (typewriter != null && dialogue != null)
        {
            // Pause SexyTime while dialogue runs (mini-game stays active)
            logic.shouldPause = true;

            // Ensure we’re at normal animator speed during dialogue
            if (logic.anim != null) logic.anim.speed = 1f;

            bool done = false;
            typewriter.StartDialogue(dialogue, () => done = true);

            yield return new WaitUntil(() => done);

            logic.shouldPause = false;
        }

        // Optional: show sexy UI again briefly (usually not needed since we reset next)
        // if (logic.UI != null)
        //     logic.UI.ShowAllSexyUIAfterFinishDialogue();

        // Now close the mini-game cleanly
        logic.ResetSexyTime();
    }

    public override void UpdateState() { }
    public override void ExitState() { }
    public override void HandleStroke() { }
    public override void HandleDeepBreathe() { }
}