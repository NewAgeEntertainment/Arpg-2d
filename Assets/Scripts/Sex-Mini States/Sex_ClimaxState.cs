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

        // Start coroutine to end sexy time after climax duration
        logic.StartCoroutine(ClimaxRoutine());
    }

    private IEnumerator ClimaxRoutine()
    {
        float drainDuration = 1.5f;
        float t = 0f;

        float startPlayerArousal = logic.currentArousal;
        float startPartnerArousal = logic.partnerBar?.value ?? 0f;

        while (t < drainDuration)
        {
            t += Time.deltaTime;
            float lerp = 1f - (t / drainDuration);

            logic.currentArousal = startPlayerArousal * lerp;
            if (logic.playerBar != null)
                logic.playerBar.value = logic.currentArousal;

            if (logic.partnerBar != null)
                logic.partnerBar.value = startPartnerArousal * lerp;

            yield return null;
        }

        logic.currentArousal = 0f;

        if (logic.playerBar != null)
            logic.playerBar.value = 0f;

        if (logic.partnerBar != null)
            logic.partnerBar.value = 0f;

        // Optional: Reset internal partner arousal if tracked
        logic.partnerCurrentArousal = 0f;

        // Optional: Reset bar fill texts
        if (logic.playerBarFillText != null)
        {
            float maxPlayer = logic.playerStats.sex.maxArousal.GetValue();
            logic.playerBarFillText.text = $"0 / {Mathf.FloorToInt(maxPlayer)}";
        }

        if (logic.partnerBarFillText != null)
        {
            float partnerMax = logic.partnerStats?.sex.maxArousal.GetValue() ?? logic.partnerBar.maxValue;
            logic.partnerBarFillText.text = $"0 / {Mathf.FloorToInt(partnerMax)}";
        }

        // Optional: Notify climax completion
        // logic.OnClimaxEnded?.Invoke();

        logic.shouldPause = false;
        logic.ResetSexyTime();
        stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
    }

    public override void UpdateState() { } // Empty since coroutine handles everything
}
