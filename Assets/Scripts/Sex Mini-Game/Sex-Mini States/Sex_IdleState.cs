using UnityEngine;

public class Sex_IdleState : SexyTimeState
{
    public Sex_IdleState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        if (logic.anim != null)
        {
            logic.anim.SetFloat("speed", 0f);
            logic.anim.Play("idle", 0, 0f);
        }
    }

    public override void HandleStroke()
    {
        var s = new Sex_StrokingState(logic, stateMachine);
        stateMachine.ChangeState(s);

        // consume this press as the first stroke
        s.HandleStroke();
    }

    public override void HandleDeepBreathe() => logic.CastDeepBreathe();
}