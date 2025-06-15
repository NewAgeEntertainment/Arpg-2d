//using UnityEngine;
//using UnityEngine.UI;
//using Rewired;

//public class Sex_StrokingState : SexState
//{
//    public Sex_StrokingState(SexyTimeLogic context) : base(context) { }

////    public override void Enter()
////    {
////        base.Enter();
////        context.anim?.SetTrigger("StartSexyTime");
////    }

////    public override void Update()
////    {
////        context.anim?.SetBool("IsStroking", true); // Fixed reference to 'anim'  
////        context.CastNPCAttackBarFill();
////        context.DepleteBars();
////        context.UpdateUI();

////        if (context.cumReached)
////        {
////            context.anim?.SetBool("IsStroking", false);
////            context.ChangeState(new Sex_ClimaxState(context));
////        }
////    }

////    public override void HandleInput()
////    {
////        if (context.player.GetButtonDown("Stroke"))
////            context.StrokeLogic(default);

////        //if (context.player.GetButtonDown("DeepBreathe"))
////        //    context.CastDeepBreathe(default);
////    }
//}

