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

    [SerializeField] private GameObject insideMenuRoot; // drag your MenuPanel holder here



    private BookUIStateMachine sm;

    public BookClosedIdleState ClosedIdleState { get; private set; }
    public BookOpeningState OpeningState { get; private set; }
    public BookOpenIdleState OpenIdleState { get; private set; }
    public BookClosingState ClosingState { get; private set; }

    private bool _pendingTurn;
    private bool _pendingTurnRight;      // true = right, false = left
    private System.Action _pendingTurnCallback;


    public BookTurnLeftState TurnLeftState { get; private set; }
    public BookTurnRightState TurnRightState { get; private set; }

    private Action _onTurnFinished;
    public bool HasPendingTurn => _pendingTurn;
    public bool PendingTurnRight => _pendingTurnRight;

    public bool IsBusy()
    {
        if (sm == null) return false;
        return sm.CurrentState == OpeningState
            || sm.CurrentState == ClosingState
            || sm.CurrentState == TurnLeftState
            || sm.CurrentState == TurnRightState;
    }


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

    public void SetInsideMenuVisible(bool visible)
    {
        if (insideMenuRoot != null)
            insideMenuRoot.SetActive(visible);
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
                    NotifyTurnFinished();
                    // page turns should always end back on open idle
                    sm.ChangeState(OpenIdleState);
                }
            }
        }
    }


    public void PlayTurnLeftThen(System.Action onDone) => QueueTurnLeft(onDone);
    public void PlayTurnRightThen(System.Action onDone) => QueueTurnRight(onDone);

    public void PlayTurnCallbackFromState(System.Action onDone)
    {
        _onTurnFinished = onDone;
        _timeout = maxWaitSeconds;
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



    public void NotifyTurnFinished()
    {
        var cb = _onTurnFinished;
        _onTurnFinished = null;
        cb?.Invoke();
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


    public System.Action ConsumePendingTurnCallback()
    {
        var cb = _pendingTurnCallback;
        _pendingTurnCallback = null;
        return cb;
    }

    public void ClearPendingTurn()
    {
        _pendingTurn = false;
        _pendingTurnCallback = null;
    }

    public void QueueTurnLeft(System.Action onDone = null)
    {
        if (!_initialized) { onDone?.Invoke(); return; }
        if (IsBusy()) return;

        _pendingTurn = true;
        _pendingTurnRight = false;
        _pendingTurnCallback = onDone;

        // Ensure we end up in OpenIdle so it can fire the turn from there
        _timeout = maxWaitSeconds;

        if (!IsInOpenIdle())
        {
            // If we're closed, open first. When open idle enters, it will start the turn.
            sm.ChangeState(OpeningState);
        }
        else
        {
            // already open idle -> OpenIdleState.Update will pick it up immediately next tick
            sm.ChangeState(OpenIdleState); // safe "poke"
        }
    }

    public void QueueTurnRight(System.Action onDone = null)
    {
        if (!_initialized) { onDone?.Invoke(); return; }
        if (IsBusy()) return;

        _pendingTurn = true;
        _pendingTurnRight = true;
        _pendingTurnCallback = onDone;

        _timeout = maxWaitSeconds;

        if (!IsInOpenIdle())
        {
            sm.ChangeState(OpeningState);
        }
        else
        {
            sm.ChangeState(OpenIdleState);
        }
    }

    public void TryStartQueuedTurnFromOpenIdle()
    {
        if (!_initialized) return;

        // Only start queued turns from the open idle animator pose
        if (!IsInOpenIdle()) return;

        // Nothing queued
        if (!_pendingTurn) return;

        // Don’t start if state machine is currently in a transition/turn
        if (IsBusy()) return;

        // Consume queue ONCE (so it can’t fire twice)
        bool right = _pendingTurnRight;
        var cb = ConsumePendingTurnCallback();
        ClearPendingTurn();

        // Attach the callback for the turn states to invoke when they reach open idle again
        PlayTurnCallbackFromState(cb);

        // Start the appropriate turn state
        sm.ChangeState(right ? TurnRightState : TurnLeftState);
    }



    // --- rest of your methods unchanged ---
    public bool IsInOpenIdle() => anim && anim.GetCurrentAnimatorStateInfo(0).IsName(openIdleStateName);
    public bool IsInClosedIdle() => anim && anim.GetCurrentAnimatorStateInfo(0).IsName(closedIdleStateName);

    public void PlayOpenThen(Action onOpened)
    {
        _onOpened = onOpened;
        _timeout = maxWaitSeconds;

        if (!_initialized)
        {
            NotifyOpened();
            return;
        }

        // If already open idle, just re-enter OpenIdleState so it fires NotifyOpened once.
        if (IsInOpenIdle())
        {
            sm.ChangeState(OpenIdleState);
            return;
        }

        sm.ChangeState(OpeningState);
    }




    public void PlayCloseThen(Action onClosed)
    {
        _onClosed = onClosed;
        _timeout = maxWaitSeconds;

        if (!_initialized)
        {
            NotifyClosed();
            return;
        }

        // If already closed idle, re-enter ClosedIdleState so it fires NotifyClosed once.
        if (IsInClosedIdle())
        {
            sm.ChangeState(ClosedIdleState);
            return;
        }

        sm.ChangeState(ClosingState);
    }

    public void NotifyOpened() { var cb = _onOpened; _onOpened = null; cb?.Invoke(); }
    public void NotifyClosed() { var cb = _onClosed; _onClosed = null; cb?.Invoke(); }

    public void PlayOpenAnim() { if (!anim) return; if (!string.IsNullOrEmpty(openTriggerName)) anim.SetTrigger(openTriggerName); }
    public void PlayCloseAnim() { if (!anim) return; if (!string.IsNullOrEmpty(closeTriggerName)) anim.SetTrigger(closeTriggerName); }

    public void SetOpenIdleImmediate() { if (!anim) return; anim.Play(openIdleStateName, 0, 0f); anim.Update(0f); }
    public void SetClosedIdleImmediate() { if (!anim) return; anim.Play(closedIdleStateName, 0, 0f); anim.Update(0f); }
}
