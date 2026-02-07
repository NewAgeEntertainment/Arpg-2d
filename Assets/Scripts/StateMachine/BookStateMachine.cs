public class BookStateMachine
{
    public BookUIState CurrentState { get; private set; }

    public void Initialize(BookUIState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    public void ChangeState(BookUIState newState)
    {
        if (CurrentState != null) CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void UpdateActiveState()
    {
        CurrentState?.Update();
    }
}
