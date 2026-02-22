using System.Collections;
using UnityEngine;

public class Sex_StartingState : SexyTimeState
{
    private readonly NPC_Dialogue introDialogue;

    public Sex_StartingState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine, NPC_Dialogue introDialogue)
        : base(logic, stateMachine)
    {
        this.introDialogue = introDialogue;
    }

    public override void EnterState()
    {
        logic.StartCoroutine(RunIntro());
    }

    private IEnumerator RunIntro()
    {
        logic.shouldPause = true;

        // Optional: hide sex UI during intro dialogue
        if (logic.UI != null)
            logic.UI.HideAllSexyUIForFinishDialogue();

        // If no dialogue, proceed immediately
        if (introDialogue == null || DialogueTypewriter.Instance == null)
        {
            yield return null;
            BeginPlayable();
            yield break;
        }

        bool done = false;
        DialogueTypewriter.Instance.StartDialogue(introDialogue, () => done = true);
        yield return new WaitUntil(() => done);

        BeginPlayable();
    }

    private void BeginPlayable()
    {
        if (logic.UI != null)
            logic.UI.ShowAllSexyUIAfterFinishDialogue();

        logic.shouldPause = false;
        stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
    }

    public override void UpdateState() { }
    public override void HandleStroke() { }
    public override void HandleDeepBreathe() { }
}