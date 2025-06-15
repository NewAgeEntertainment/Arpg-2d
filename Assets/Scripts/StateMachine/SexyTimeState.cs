using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SexyTimeState
{
    protected SexyTimeLogic logic;
    protected SexyTimeStateMachine stateMachine;
    

    public SexyTimeState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
    {
        this.logic = logic;
        this.stateMachine = stateMachine;
        
    }

    public virtual void EnterState() 
    {

    }
    public virtual void ExitState() 
    {
        
    }
    public virtual void UpdateState() 
    {
        
    }
    public virtual void HandleStroke() 
    {
        
    }
    public virtual void HandleDeepBreathe() 
    {
        
    }
}
