//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class SexStateMachine
//{
//    public SexState currentState { get; private set; }
//    protected SexyTimeLogic sexyTimeLogic;

//    protected Animator anim;

//    public Sex_IdleState idleState { get; private set; }

//    //public SexStateMachine(SexyTimeLogic sexyTimeLogic)
//    //{
//    //    idleState = new Sex_IdleState(sexyTimeLogic, this, "idle");
//    //}




//    public void Initialize()
//    {
//        currentState = idleState;
//        currentState.Enter();
//    }

//    public void ChangeState()
//    {
//        currentState.Exit();
//        currentState = newState;
//        currentState.Enter();
//    }

//    public void UpdateActiveState()
//    {
//        currentState.Update();
//    }

//    //public void OnStrokeInput()
//    //{
//    //    currentState?.HandleStroke();
//    //}

//    //public void OnDeepBreatheInput()
//    //{
//    //    currentState?.HandleDeepBreathe();
//    //}
//}
