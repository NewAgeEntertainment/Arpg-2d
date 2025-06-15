using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sex_IdleState : SexyTimeState
{
    public Sex_IdleState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entering Idle State");
        if (logic.anim != null)
            logic.anim.Play("fuck");
        else
            Debug.LogWarning("Animator is null on logic.");
    }

    public override void UpdateState()
    {
        base.UpdateState();
        stateMachine.ChangeState(new Sex_StrokingState(logic, stateMachine));
    }

    public override void HandleStroke()
    {
        
    }
}
