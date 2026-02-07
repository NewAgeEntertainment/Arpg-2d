using UnityEngine;

public class BookClosedIdleState : BookUIState
{
    public BookClosedIdleState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        manager.SetClosedIdleImmediate();
    }
}
