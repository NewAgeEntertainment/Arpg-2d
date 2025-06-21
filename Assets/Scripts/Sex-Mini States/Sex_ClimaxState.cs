using System.Collections;
using UnityEngine;

public class Sex_ClimaxState : SexyTimeState
{

    public Sex_ClimaxState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entering Climax State");

        logic.anim.Play("sperm shot", 0, 0f);
        logic.shouldPause = true;
        logic.cumReached = true;

        // Start coroutine to end sexy time after climax duration
        logic.StartCoroutine(ClimaxRoutine());
    }

    private IEnumerator ClimaxRoutine()
    {
        float drainDuration = 1.5f;
        float t = 0f;
        float startPlayerArousal = logic.currentArousal;
        float startPartnerBar = logic.partnerBar.value;

        while (t < drainDuration)
        {
            t += Time.deltaTime;
            float lerp = 1f - (t / drainDuration);

            logic.currentArousal = startPlayerArousal * lerp;
            logic.playerBar.value = logic.currentArousal;

            logic.partnerBar.value = startPartnerBar * lerp;

            yield return null;
        }

        logic.currentArousal = 0f;
        logic.playerBar.value = 0f;
        logic.partnerBar.value = 0f;

        logic.shouldPause = false;
        logic.ResetSexyTime();
        stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
    }



    public override void UpdateState() { } // Empty since coroutine handles everything

}

