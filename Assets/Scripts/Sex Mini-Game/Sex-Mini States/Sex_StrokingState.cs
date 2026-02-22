using UnityEngine;

public class Sex_StrokingState : SexyTimeState
{
    private float strokeCooldown = 0.2f;
    private float lastStrokeTime;

    public Sex_StrokingState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine)
    {
    }

    public override void EnterState()
    {
        lastStrokeTime = Time.time;
    }

    public override void HandleStroke()
    {
        if (logic.shouldPause) return;   // ✅ add this
        if (Time.time - lastStrokeTime < strokeCooldown)
            return;

        lastStrokeTime = Time.time;

        AnimatorStateInfo stateInfo = logic.anim.GetCurrentAnimatorStateInfo(0);

        // Restart "fuck" animation if not playing or completed
        if (!stateInfo.IsName("fuck") || stateInfo.normalizedTime >= 1f)
            logic.anim.Play("fuck", 0, 0f);

        ApplyStrokeBars();
    }

    private void ApplyStrokeBars()
    {
        var ui = logic.UI;

        logic.anim.SetFloat("speed", GetSpeed());

        float playerMaxArousal = ui.PlayerBarMax;
        float partnerMaxArousal = ui.PartnerBarMax;

        // ------- PLAYER (blue) gain -------
        // You used to use GetBlueBarStrokeValue + arousalPerStroke – keep your formula:
        float strokeGainBlue = logic.playerStats.GetBlueBarStrokeValue();
        float resilience = logic.playerStats.GetResilienceMitigation(0f);
        float reduction = 1f - (resilience / 100f);
        float blueIncrease = logic.ArousalPerStroke * reduction;
        float totalBlueGain = strokeGainBlue + blueIncrease;

        float newPlayerVal = Mathf.Min(ui.PlayerBarValue + totalBlueGain, playerMaxArousal);

        // ------- PARTNER (pink) gain -------
        bool isCrit;
        float strokeDamage = logic.playerStats.GetSexualDamage(out isCrit);
        if (isCrit) logic.ShowCritFeedback();

        float partnerResilience = logic.partnerStats?.GetResilienceMitigation(0f) ?? 0f;
        float partnerRedMult = 1f - (partnerResilience / 100f);
        float basePartnerGain = logic.ArousalPerStroke + strokeDamage;
        float partnerIncrease = basePartnerGain * partnerRedMult;
        float newPartnerVal = Mathf.Min(ui.PartnerBarValue + partnerIncrease, partnerMaxArousal);

        ui.UpdateBars(newPlayerVal, playerMaxArousal, newPartnerVal, partnerMaxArousal);
        logic.CheckDialogueTriggersAfterBars(newPlayerVal, newPartnerVal);
        if (logic.shouldPause) return;

        // ----- Check events / climax -----
        if (newPlayerVal >= playerMaxArousal && !logic.playerBarReachedOnce)
        {
            logic.playerBarReachedOnce = true;
            logic.OnPlayerBarFull?.Invoke();
        }

        if (newPartnerVal >= partnerMaxArousal && !logic.partnerBarReachedOnce)
        {
            logic.partnerBarReachedOnce = true;
            logic.OnPartnerBarFull?.Invoke();
        }

        if (newPlayerVal >= playerMaxArousal || newPartnerVal >= partnerMaxArousal)
        {
            logic.cumReached = true;
            logic.cumTimeElapsed = 0f;
            stateMachine.ChangeState(new Sex_ClimaxState(logic, stateMachine));
        }
    }

    private float GetSpeed()
    {
        float timeSinceLastStroke = Time.time - lastStrokeTime;

        if (timeSinceLastStroke >= 0.5f) return 1f;
        if (timeSinceLastStroke >= 0.25f) return 1.25f;
        if (timeSinceLastStroke >= 0.15f) return 1.5f;
        return 2f;
    }

    public override void UpdateState() { }

    public override void ExitState()
    {
        Debug.Log("Exiting Stroking State");
    }
}
