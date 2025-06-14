using UnityEngine;

public class Sex_ClimaxState : SexState
{
    public Sex_ClimaxState(SexyTimeLogic sex) : base(sex) 
    {
        
    }

    //public override void Enter()
    //{
    //    context.cumTimeElapsed = 0f;

    //    //if (SoundManager.instance != null && context.cummingSound != null)
    //    //    SoundManager.instance.PlaySound(context.cummingSound);

    //    context.anim?.SetTrigger("Climax");
    
    //}

    //public override void Update()
    //{
    //    context.cumTimeElapsed += Time.deltaTime;

    //    if (context.cumTimeElapsed >= context.cumDuration)
    //    {
    //        //context.StatsManager.Instance.AddSexExp(context.sexExpToGive);
    //        //context.unlockedSO.UnlockAnim(context.animID);
    //        context.ChangeState(new Sex_FinishState(context));
    //    }
    //}
}

