using UnityEngine;

public class BookTurnRightState : BookUIState
{
    private bool _leftOpenIdle;

    public BookTurnRightState(BookUIStateMachine sm, BookOpenManager mgr, Animator animator)
        : base(sm, mgr, animator) { }

    public override void Enter()
    {
        _leftOpenIdle = false;
        manager.PlayTurnRightAnim();
    }

    public override void Update()
    {
        if (!_leftOpenIdle)
        {
            if (!manager.IsInOpenIdle())
                _leftOpenIdle = true;

            return;
        }

        if (manager.IsInOpenIdle())
        {
            manager.NotifyTurnFinished();
            stateMachine.ChangeState(manager.OpenIdleState);
        }
    }
}
