using UnityEngine;
using UnityEngine.UI;
using Rewired;

using UnityEngine;

public class Sex_StrokingState : SexyTimeState
{
    private float strokeCooldown = 0.2f;
    private float timeBetweenStrokes;
    private float lastStrokeTime = 1f;
   
    public Sex_StrokingState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entered Stroking State");
        lastStrokeTime = Time.time;
    }

    public override void HandleStroke()
    {
        if (Time.time - lastStrokeTime < 0.2f) // optional cooldown
            return;

        lastStrokeTime = Time.time;

        AnimatorStateInfo stateInfo = logic.anim.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("fuck") || stateInfo.normalizedTime >= 1f)
        {
            logic.anim.Play("fuck", 0, 0f);
        }

        ApplyStrokeBars();
    }

    private void ApplyStrokeBars()
    {
        AnimatorStateInfo stateInfo = logic.anim.GetCurrentAnimatorStateInfo(0);

        // Only play if not already in the "fuck" animation
        if (!stateInfo.IsName("fuck"))
        {
            logic.anim.Play("fuck", 0, 0f);
        }

        logic.anim.SetFloat("speed", GetSpeed());

        float blueGain = logic.playerStats?.GetBlueBarStrokeValue() ?? 0f;
        float pinkGain = logic.playerStats?.GetPinkBarStrokeValue() ?? 0f;

        logic.blueBar.value += blueGain;
        logic.pinkBar.value += pinkGain;

        // Climax trigger
        if (logic.blueBar.value >= logic.blueBarMaxValue || logic.pinkBar.value >= logic.pinkBarMaxValue)
        {
            logic.cumReached = true;
            logic.cumTimeElapsed = 0f; // Reset timer when climax starts
            stateMachine.ChangeState(new Sex_ClimaxState(logic, stateMachine));
        }

        // Milestone triggers
        if (logic.blueBar.value >= logic.blueBarMaxValue && !logic.blueBarReachedOnce)
        {
            logic.blueBarReachedOnce = true;
            Debug.Log("Blue BAR FULL QUEST");
            logic.OnBlueBarFull?.Invoke();
        }

        if (logic.pinkBar.value >= logic.pinkBarMaxValue && !logic.pinkBarReachedOnce)
        {
            logic.pinkBarReachedOnce = true;
            Debug.Log("Pink BAR FULL QUEST");
            logic.OnPinkBarFull?.Invoke();
        }
    }

    private float GetSpeed()
    {
        if (timeBetweenStrokes >= 0.5f)
            return 1f;
        else if (timeBetweenStrokes >= 0.25f)
            return 1.25f;
        else if (timeBetweenStrokes >= 0.15f)
            return 1.5f;
        else
            return 2f;
    }

    public override void UpdateState()
    {
        // Optional: add passive drain or time logic here
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Stroking State");
    }
}


