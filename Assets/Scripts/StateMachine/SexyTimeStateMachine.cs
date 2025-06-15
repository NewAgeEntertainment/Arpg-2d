using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SexyTimeStateMachine : MonoBehaviour 
{
    public SexyTimeLogic logic;
    private SexyTimeState currentState;

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
        //currentState = new SexyTimeDialoguePauseState(logic, this, currentState);
        currentState.EnterState();
    }

    public void ResumeAfterDialogue()
    {
        //if (currentState is SexyTimeDialoguePauseState pauseState)
        {
            //pauseState.ResumeFromPause();
        }
    }

}
