using System;
using System.Collections;
using UnityEngine;

public class BookOpenManager : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator anim;

    [Header("Animator State Names (must match EXACTLY)")]
    [SerializeField] private string closedIdleStateName = "Book Close idle";
    [SerializeField] private string openIdleStateName = "Book Open idle";

    [Header("Animator Params (optional)")]
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Fail-safe")]
    [SerializeField] private float maxWaitSeconds = 1.25f;

    [Header("Page Turn State Names (must match EXACTLY)")]
    [SerializeField] private string turnLeftStateName = "Book Turn Left";
    [SerializeField] private string turnRightStateName = "Book Turn Right";

    [Header("Page Turn Triggers")]
    [SerializeField] private string turnLeftTriggerName = "TurnLeft";
    [SerializeField] private string turnRightTriggerName = "TurnRight";


    private BookUIStateMachine sm;

    public BookClosedIdleState ClosedIdleState { get; private set; }
    public BookOpeningState OpeningState { get; private set; }
    public BookOpenIdleState OpenIdleState { get; private set; }
    public BookClosingState ClosingState { get; private set; }



    public BookTurnLeftState TurnLeftState { get; private set; }
    public BookTurnRightState TurnRightState { get; private set; }




    public void ShowOpenIdle() => SetOpenIdleImmediate();
    public void ShowClosedIdle() => SetClosedIdleImmediate();

    private Action _onOpened;
    private Action _onClosed;
    private float _timeout;

    private bool _initialized;

    private void OnEnable()
    {
        if (_initialized) return;
        StartCoroutine(InitNextFrame());
    }



    private IEnumerator InitNextFrame()
    {
        // wait 1 frame so Animator state info is valid even if UI objects were just enabled
        yield return null;

        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        if (anim == null)
        {
            Debug.LogError("[BookOpenManager] Animator not found. Assign it in Inspector or ensure it exists under this object.");
            enabled = false;
            yield break;
        }

        sm = new BookUIStateMachine();

        ClosedIdleState = new BookClosedIdleState(sm, this, anim);
        OpeningState = new BookOpeningState(sm, this, anim);
        OpenIdleState = new BookOpenIdleState(sm, this, anim);
        ClosingState = new BookClosingState(sm, this, anim);

        TurnLeftState = new BookTurnLeftState(sm, this, anim);
        TurnRightState = new BookTurnRightState(sm, this, anim);


        // choose a safe starting pose (closed) OR read current pose
        if (IsInOpenIdle())
            sm.Initialize(OpenIdleState);
        else
            sm.Initialize(ClosedIdleState);

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        // tick the state machine
        sm.UpdateActiveState();

        // fail-safe timeout while in any transition state
        if (sm.CurrentState == OpeningState || sm.CurrentState == ClosingState ||
            sm.CurrentState == TurnLeftState || sm.CurrentState == TurnRightState)
        {
            _timeout -= Time.unscaledDeltaTime;

            if (_timeout <= 0f)
            {
                // force to the intended idle if animator never reached it
                if (sm.CurrentState == OpeningState)
                {
                    sm.ChangeState(OpenIdleState);
                }
                else if (sm.CurrentState == ClosingState)
                {
                    sm.ChangeState(ClosedIdleState);
                }
                else
                {
                    // page turns should always end back on open idle
                    sm.ChangeState(OpenIdleState);
                }
            }
        }
    }


    public bool IsInTurnLeft()
    {
        if (anim == null) return false;
        return anim.GetCurrentAnimatorStateInfo(0).IsName(turnLeftStateName);
    }

    public bool IsInTurnRight()
    {
        if (anim == null) return false;
        return anim.GetCurrentAnimatorStateInfo(0).IsName(turnRightStateName);
    }

    public void PlayTurnLeftAnim()
    {
        if (anim == null) return;
        if (!string.IsNullOrEmpty(turnLeftTriggerName))
            anim.SetTrigger(turnLeftTriggerName);
    }

    public void PlayTurnRightAnim()
    {
        if (anim == null) return;
        if (!string.IsNullOrEmpty(turnRightTriggerName))
            anim.SetTrigger(turnRightTriggerName);
    }

    public void TurnLeft()
    {
        if (!_initialized) return;

        // only allow turns while open
        if (!IsInOpenIdle()) return;

        // don't allow another turn while we're already turning
        if (sm.CurrentState == TurnLeftState || sm.CurrentState == TurnRightState) return;

        _timeout = maxWaitSeconds;   // reuse your existing fail-safe
        sm.ChangeState(TurnLeftState);
    }

    public void TurnRight()
    {
        if (!_initialized) return;

        if (!IsInOpenIdle()) return;

        if (sm.CurrentState == TurnLeftState || sm.CurrentState == TurnRightState) return;

        _timeout = maxWaitSeconds;
        sm.ChangeState(TurnRightState);
    }






    // --- rest of your methods unchanged ---
    public bool IsInOpenIdle() => anim && anim.GetCurrentAnimatorStateInfo(0).IsName(openIdleStateName);
    public bool IsInClosedIdle() => anim && anim.GetCurrentAnimatorStateInfo(0).IsName(closedIdleStateName);

    public void PlayOpenThen(Action onOpened) { _onOpened = onOpened; _timeout = maxWaitSeconds; if (IsInOpenIdle()) { NotifyOpened(); return; } sm.ChangeState(OpeningState); }
    public void PlayCloseThen(Action onClosed) { _onClosed = onClosed; _timeout = maxWaitSeconds; if (IsInClosedIdle()) { NotifyClosed(); return; } sm.ChangeState(ClosingState); }

    public void NotifyOpened() { var cb = _onOpened; _onOpened = null; cb?.Invoke(); }
    public void NotifyClosed() { var cb = _onClosed; _onClosed = null; cb?.Invoke(); }

    public void PlayOpenAnim() { if (!anim) return; if (!string.IsNullOrEmpty(openTriggerName)) anim.SetTrigger(openTriggerName); }
    public void PlayCloseAnim() { if (!anim) return; if (!string.IsNullOrEmpty(closeTriggerName)) anim.SetTrigger(closeTriggerName); }

    public void SetOpenIdleImmediate() { if (!anim) return; anim.Play(openIdleStateName, 0, 0f); anim.Update(0f); }
    public void SetClosedIdleImmediate() { if (!anim) return; anim.Play(closedIdleStateName, 0, 0f); anim.Update(0f); }
}
