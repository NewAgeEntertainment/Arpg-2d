using UnityEngine;

public class Sex_PausedState : SexyTimeState
{
    private float _prevAnimSpeed = 1f;

    public Sex_PausedState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        logic.shouldPause = true;

        if (logic.anim != null)
        {
            _prevAnimSpeed = Mathf.Approximately(logic.anim.speed, 0f) ? 1f : logic.anim.speed;
            logic.anim.speed = 0f;
        }
    }

    public override void ExitState()
    {
        logic.shouldPause = false;

        if (logic.anim != null)
            logic.anim.speed = _prevAnimSpeed;
    }
}