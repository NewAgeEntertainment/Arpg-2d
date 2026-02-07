using UnityEngine;

public class BookTurnLeftState : BookUIState
{
    public BookTurnLeftState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        // Ensure we’re open
        if (!manager.IsInOpenIdle())
        {
            stateMachine.ChangeState(manager.OpenIdleState);
            return;
        }

        manager.PlayTurnLeftAnim();
    }

    public override void Update()
    {
        // When the animation returns to OpenIdle, go back to OpenIdleState
        if (manager.IsInOpenIdle())
            stateMachine.ChangeState(manager.OpenIdleState);
    }
}
