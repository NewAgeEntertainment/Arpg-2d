using UnityEngine;

public class BookOpenIdleState : BookUIState
{
    public BookOpenIdleState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        manager.SetOpenIdleImmediate();

        // ✅ show menu panel holder while book is open idle
        manager.SetInsideMenuVisible(true);

        manager.NotifyOpened();
        manager.TryStartQueuedTurnFromOpenIdle();
    }




    public override void Update()
    {
        // If something queued a turn while we were already idling,
        // start it on the next tick (still only from OpenIdle).
        manager.TryStartQueuedTurnFromOpenIdle();
    }
}
