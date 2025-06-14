using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SexState
{


    protected SexyTimeLogic context;

    public SexState(SexyTimeLogic context)
    {
        this.context = context;
    }

    

    public virtual void Enter()
    {

    }
    public virtual void Exit() 
    {
        
    }
    public virtual void Update() 
    {
    
    }
    public virtual void HandleInput() 
    {
    
    }
}
