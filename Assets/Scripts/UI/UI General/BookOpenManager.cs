using System;
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

    private BookUIStateMachine sm;

    public BookClosedIdleState ClosedIdleState { get; private set; }
    public BookOpeningState OpeningState { get; private set; }
    public BookOpenIdleState OpenIdleState { get; private set; }
    public BookClosingState ClosingState { get; private set; }

    // Backwards-compatible names used by UI.cs
    public void ShowOpenIdle() => SetOpenIdleImmediate();
    public void ShowClosedIdle() => SetClosedIdleImmediate();


    private Action _onOpened;
    private Action _onClosed;

    private float _timeout;

    private void Awake()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        sm = new BookUIStateMachine();

        ClosedIdleState = new BookClosedIdleState(sm, this, anim);
        OpeningState = new BookOpeningState(sm, this, anim);
        OpenIdleState = new BookOpenIdleState(sm, this, anim);
        ClosingState = new BookClosingState(sm, this, anim);

        // Initialize based on current animator pose
        if (IsInOpenIdle())
            sm.Initialize(OpenIdleState);
        else
            sm.Initialize(ClosedIdleState);
    }

    private void Start()
    {
        StartCoroutine(ResyncInitialStateNextFrame());
    }

    private System.Collections.IEnumerator ResyncInitialStateNextFrame()
    {
        yield return null; // let Animator evaluate
        if (IsInOpenIdle()) sm.Initialize(OpenIdleState);
        else sm.Initialize(ClosedIdleState);
    }


    private void Update()
    {
        // tick the state machine
        sm.UpdateActiveState();

        // fail-safe timeout while in transition states
        if (sm.CurrentState == OpeningState || sm.CurrentState == ClosingState)
        {
            _timeout -= Time.unscaledDeltaTime;
            if (_timeout <= 0f)
            {
                // force to the intended idle if animator never reached it
                if (sm.CurrentState == OpeningState)
                    sm.ChangeState(OpenIdleState);
                else
                    sm.ChangeState(ClosedIdleState);
            }
        }
    }

    // ---------- Public API ----------
    public void PlayOpenThen(Action onOpened)
    {
        _onOpened = onOpened;
        _timeout = maxWaitSeconds;

        // already open?
        if (IsInOpenIdle())
        {
            NotifyOpened();
            return;
        }

        sm.ChangeState(OpeningState);
    }

    public void PlayCloseThen(Action onClosed)
    {
        _onClosed = onClosed;
        _timeout = maxWaitSeconds;

        // already closed?
        if (IsInClosedIdle())
        {
            NotifyClosed();
            return;
        }

        sm.ChangeState(ClosingState);
    }

    // ---------- Called by states ----------
    public void NotifyOpened()
    {
        var cb = _onOpened;
        _onOpened = null;
        cb?.Invoke();
    }

    public void NotifyClosed()
    {
        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }

    // ---------- Animator helpers ----------
    public bool IsInOpenIdle()
    {
        if (anim == null) return false;
        return anim.GetCurrentAnimatorStateInfo(0).IsName(openIdleStateName);
    }

    public bool IsInClosedIdle()
    {
        if (anim == null) return false;
        return anim.GetCurrentAnimatorStateInfo(0).IsName(closedIdleStateName);
    }

    public void PlayOpenAnim()
    {
        if (anim == null) return;
        if (!string.IsNullOrEmpty(openTriggerName))
            anim.SetTrigger(openTriggerName);
    }

    public void PlayCloseAnim()
    {
        if (anim == null) return;
        if (!string.IsNullOrEmpty(closeTriggerName))
            anim.SetTrigger(closeTriggerName);
    }

    public void SetOpenIdleImmediate()
    {
        if (anim == null) return;
        anim.Play(openIdleStateName, 0, 0f);
        anim.Update(0f);
    }

    public void SetClosedIdleImmediate()
    {
        if (anim == null) return;
        anim.Play(closedIdleStateName, 0, 0f);
        anim.Update(0f);
    }
}
