using UnityEngine;

public class Sex_IdleState : SexyTimeState
{
    public Sex_IdleState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        if (logic.anim != null)
            logic.anim.Play("idle", 0, 0f);   // use idle, not "fuck"
    }

    public override void UpdateState()
    {
        // Do nothing. Idle is a real idle now.
    }

    public override void HandleStroke()
    {
        // Only transition when player actually strokes
        stateMachine.ChangeState(new Sex_StrokingState(logic, stateMachine));
    }

    public override void HandleDeepBreathe()
    {
        logic.CastDeepBreathe();
    }
}