using UnityEngine;

public class Player_DeathState : PlayerState
{
    private bool popupShown;
    private float fallbackDelay = 0.75f; // used if we can’t read clip length

    public Player_DeathState(Player player, StateMachine sm, string animBoolName)
        : base(player, sm, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        popupShown = false;

        // stop motion & input
        player.SetVelocity(0, 0);
        player.input.Disable();

        // play death anim (animBoolName should map to your “dead” bool)
        // If you're using a trigger, set it here instead.
        // anim.SetBool(animBoolName, true); // base.Enter usually does this

        // try to use current state’s animation length
        float delay = fallbackDelay;
        var info = player.anim.GetCurrentAnimatorStateInfo(0);
        if (info.length > 0f) delay = info.length * Mathf.Max(0.95f, info.normalizedTime < 1f ? 1f : 1f);

        stateTimer = delay; // PlayerState decreases stateTimer each Update
    }

    public override void Update()
    {
        base.Update();

        // keep the body still
        player.SetVelocity(0, 0);

        if (!popupShown && stateTimer <= 0f)
        {
            popupShown = true;
            UI_GameOver.ShowStatic(); // or ShowNow() if you added the alias
        }
    }

    public override void Exit()
    {
        base.Exit();
        // stay dead; no special cleanup here
    }
}
