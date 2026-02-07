using UnityEngine;

public class BookTurnRightState : BookUIState
{
    public BookTurnRightState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        if (!manager.IsInOpenIdle())
        {
            stateMachine.ChangeState(manager.OpenIdleState);
            return;
        }

        manager.PlayTurnRightAnim();
    }

    public override void Update()
    {
        if (manager.IsInOpenIdle())
            stateMachine.ChangeState(manager.OpenIdleState);
    }
}
