using UnityEngine;

public class BookClosingState : BookUIState
{
    public BookClosingState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        manager.PlayCloseAnim();
    }

    public override void Update()
    {
        if (manager.IsInClosedIdle())
            stateMachine.ChangeState(manager.ClosedIdleState); // <-- go to closed idle
    }
}
