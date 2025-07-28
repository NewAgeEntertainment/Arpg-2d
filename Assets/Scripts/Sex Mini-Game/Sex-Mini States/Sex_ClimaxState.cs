using System.Collections;
using UnityEngine;

public class Sex_ClimaxState : SexyTimeState
{
    public Sex_ClimaxState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entering Climax State");

        logic.anim.Play("sperm shot", 0, 0f);
        logic.shouldPause = true;
        logic.cumReached = true;

        logic.StartCoroutine(ClimaxRoutine());
    }

    private IEnumerator ClimaxRoutine()
    {
        var ui = logic.UI;

        float drainDuration = 1.5f;
        float t = 0f;

        float startPlayer = ui.PlayerBarValue;
        float startPartner = ui.PartnerBarValue;

        float pMax = ui.PlayerBarMax;
        float partnerMax = ui.PartnerBarMax;

        while (t < drainDuration)
        {
            t += Time.deltaTime;
            float lerp = 1f - (t / drainDuration);

            float newPlayer = startPlayer * lerp;
            float newPartner = startPartner * lerp;

            ui.UpdateBars(newPlayer, pMax, newPartner, partnerMax);

            yield return null;
        }

        ui.UpdateBars(0f, pMax, 0f, partnerMax);

        logic.shouldPause = false;
        logic.ResetSexyTime();
        stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
    }

    public override void UpdateState() { }
}
