using UnityEngine;

public class BookOpeningState : BookUIState
{
    public BookOpeningState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        manager.PlayOpenAnim();
    }

    public override void Update()
    {
        if (manager.IsInOpenIdle())
            stateMachine.ChangeState(manager.OpenIdleState);
    }
}
