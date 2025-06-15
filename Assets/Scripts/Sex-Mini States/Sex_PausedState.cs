using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sex_PausedState : SexyTimeState
{
    private SexyTimeState previousState;

    public Sex_PausedState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine, SexyTimeState previousState)
        : base(logic, stateMachine)
    {
        this.previousState = previousState;
    }

    public override void EnterState()
    {
        Debug.Log("Paused for Dialogue");
        logic.anim.speed = 0f; // pause animation
        //logic.playerLocked = true; // optional flag to block inputs elsewhere
    }

    public override void ExitState()
    {
        logic.anim.speed = 1f; // resume animation
        //logic.playerLocked = false;
    }

    // Ignore input while paused
    public override void HandleStroke() { }
    public override void HandleDeepBreathe() { }

    public void ResumeFromPause()
    {
        stateMachine.ChangeState(previousState);
    }
}

