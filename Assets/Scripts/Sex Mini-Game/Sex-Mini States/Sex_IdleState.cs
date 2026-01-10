using UnityEngine;

public class Sex_IdleState : SexyTimeState
{
    public Sex_IdleState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        if (logic.anim != null)
            logic.anim.Play("fuck", 0, 0f);
        else
            Debug.LogWarning("[Sex_IdleState] Animator is null on logic.");
    }

    public override void UpdateState()
    {
        // Immediately switch to stroking (same behavior you had)
        stateMachine.ChangeState(new Sex_StrokingState(logic, stateMachine));
    }

    public override void HandleStroke() { }
}
