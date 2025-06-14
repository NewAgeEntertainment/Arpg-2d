using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sex_PausedState : SexState
{
    public Sex_PausedState(SexyTimeLogic context) : base(context) { }

    public override void Enter()
    {
        //context.shouldPause = true;
        Time.timeScale = 0f; // Optional: Freeze time
    }

    public override void Exit()
    {
        //context.shouldPause = false;
        Time.timeScale = 1f; // Resume time
    }
}

