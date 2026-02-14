using UnityEngine;

public abstract class BookUIState
{
    protected readonly BookUIStateMachine stateMachine;
    protected readonly BookOpenManager manager;
    protected readonly Animator anim;

    protected BookUIState(BookUIStateMachine stateMachine, BookOpenManager manager, Animator anim)
    {
        this.stateMachine = stateMachine;
        this.manager = manager;
        this.anim = anim;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
