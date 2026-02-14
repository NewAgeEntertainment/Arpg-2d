using UnityEngine;

public class BookTurnLeftState : BookUIState
{
    private bool _leftOpenIdle; // we must leave OpenIdle once before we can finish

    public BookTurnLeftState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        _leftOpenIdle = false;
        manager.PlayTurnLeftAnim();
    }

    public override void Update()
    {
        // Wait until the animator actually leaves OpenIdle at least once
        if (!_leftOpenIdle)
        {
            if (!manager.IsInOpenIdle())
                _leftOpenIdle = true;

            return;
        }

        // Now we can treat "back to OpenIdle" as completion
        if (manager.IsInOpenIdle())
        {
            manager.NotifyTurnFinished();
            stateMachine.ChangeState(manager.OpenIdleState);
        }
    }
}
