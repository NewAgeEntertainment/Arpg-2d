using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SexyTimeStateMachine : MonoBehaviour
{
    public SexyTimeLogic logic;
    private SexyTimeState currentState;

    private SexyTimeState _stateBeforePause;

    public void ChangeState(SexyTimeState newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }

    void Update()
    {
        currentState?.UpdateState();
    }

    public void Stroke()
    {
        currentState?.HandleStroke();
    }

    public void DeepBreathe()
    {
        currentState?.HandleDeepBreathe();
    }

    public void PauseForDialogue()
    {
        // Don’t pause twice
        if (currentState is Sex_PausedState) return;

        _stateBeforePause = currentState;
        ChangeState(new Sex_PausedState(logic, this));
    }

    public void ResumeAfterDialogue()
    {
        if (!(currentState is Sex_PausedState)) return;

        // If something went wrong, fallback to Idle
        if (_stateBeforePause == null)
            ChangeState(new Sex_IdleState(logic, this));
        else
            ChangeState(_stateBeforePause);

        _stateBeforePause = null;
    }
}
