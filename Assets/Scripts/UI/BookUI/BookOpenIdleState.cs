using UnityEngine;

public class BookOpenIdleState : BookUIState
{
    public BookOpenIdleState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        manager.SetOpenIdleImmediate();
        manager.NotifyOpened(); // fires your callback if any
    }
}
