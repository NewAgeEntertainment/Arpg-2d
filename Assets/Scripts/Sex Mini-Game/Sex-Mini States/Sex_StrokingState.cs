using UnityEngine;

public class Sex_StrokingState : SexyTimeState
{
    [Header("Timing")]
    private float idleReturnDelay = 0.35f;   // how long without presses before going idle
    private float strokeCooldown = 0.08f;    // prevents accidental double-taps

    private float _nextAllowedPressTime;
    private float _lastPressTime;
    private bool _strokeAnimPlaying;
    private bool _queuedNextStroke;

    public Sex_StrokingState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        _nextAllowedPressTime = Time.time;
        _lastPressTime = Time.time;
        _strokeAnimPlaying = false;
        _queuedNextStroke = false;
    }

    public override void HandleStroke()
    {
        if (logic.shouldPause) return;
        if (Time.time < _nextAllowedPressTime) return;

        _nextAllowedPressTime = Time.time + strokeCooldown;
        _lastPressTime = Time.time;

        // If a stroke is currently playing, DON'T restart it.
        // Just buffer one next stroke.
        if (_strokeAnimPlaying)
        {
            _queuedNextStroke = true;
            return;
        }

        StartStroke();
    }

    public override void UpdateState()
    {
        if (logic.shouldPause) return;
        if (logic.anim == null) return;

        if (_strokeAnimPlaying)
        {
            var st = logic.anim.GetCurrentAnimatorStateInfo(0);

            // Wait until the stroke clip finishes
            if (st.IsName("fuck") && st.normalizedTime >= 1f)
            {
                _strokeAnimPlaying = false;

                // If player pressed during the stroke, chain immediately
                if (_queuedNextStroke)
                {
                    _queuedNextStroke = false;
                    StartStroke();
                    return;
                }

                // No buffered press: only return to idle if player hasn't pressed recently
                if (Time.time - _lastPressTime >= idleReturnDelay)
                    stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
            }
        }
        else
        {
            // Not currently stroking — if player stops pressing for a bit, go idle
            if (Time.time - _lastPressTime >= idleReturnDelay)
                stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
        }
    }

    private void StartStroke()
    {
        _strokeAnimPlaying = true;

        // Play ONE full stroke (no looping!)
        logic.anim.SetFloat("speed", 1f);
        logic.anim.Play("fuck", 0, 0f); // <-- rename if your state is "Stroke"

        ApplyStrokeBars();
    }

    private void ApplyStrokeBars()
    {
        var ui = logic.UI;
        if (ui == null || logic.playerStats == null) return;

        logic.anim.SetFloat("speed", 1f);

        float playerMaxArousal = ui.PlayerBarMax;
        float partnerMaxArousal = ui.PartnerBarMax;

        // BLUE
        float strokeGainBlue = logic.playerStats.GetBlueBarStrokeValue();
        float resilience = logic.playerStats.GetResilienceMitigation(0f);
        float reduction = 1f - (resilience / 100f);
        float blueIncrease = logic.ArousalPerStroke * reduction;
        float newPlayerVal = Mathf.Min(ui.PlayerBarValue + (strokeGainBlue + blueIncrease), playerMaxArousal);

        // PINK
        bool isCrit;
        float strokeDamage = logic.playerStats.GetSexualDamage(out isCrit);
        if (isCrit) logic.ShowCritFeedback();

        float partnerResilience = logic.partnerStats?.GetResilienceMitigation(0f) ?? 0f;
        float partnerRedMult = 1f - (partnerResilience / 100f);
        float basePartnerGain = logic.ArousalPerStroke + strokeDamage;
        float newPartnerVal = Mathf.Min(ui.PartnerBarValue + (basePartnerGain * partnerRedMult), partnerMaxArousal);

        ui.UpdateBars(newPlayerVal, playerMaxArousal, newPartnerVal, partnerMaxArousal);

        logic.CheckDialogueTriggersAfterBars(newPlayerVal, newPartnerVal);
        if (logic.shouldPause) return;

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
}