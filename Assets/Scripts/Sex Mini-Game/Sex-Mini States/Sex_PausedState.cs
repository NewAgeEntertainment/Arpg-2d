using UnityEngine;

public class Sex_PausedState : SexyTimeState
{
    public Sex_PausedState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        logic.shouldPause = true;
        if (logic.anim != null)
            logic.anim.speed = 0f;
    }

    public override void ExitState()
    {
        logic.shouldPause = false;
        if (logic.anim != null)
            logic.anim.speed = 1f;
    }

    public override void HandleStroke() { }
    public override void UpdateState() { }
}
