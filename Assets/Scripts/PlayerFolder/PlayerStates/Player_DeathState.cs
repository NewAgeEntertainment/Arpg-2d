using System.Collections;
using UnityEngine;

public class Player_DeathState : PlayerState
{
    private readonly Player player;
    private readonly string animKey;
    private const string triggerName = "death"; // using a const instead of [SerializeField]

    public Player_DeathState(Player player, StateMachine sm, string animBoolName)
        : base(player, sm, animBoolName)
    {
        this.player = player;
        this.animKey = animBoolName;
    }

    public override void Enter()
    {
        base.Enter();

        // Stop movement immediately
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;
        player.SetVelocity(0f, 0f);

        // Disable gameplay input maps so dash/attack etc. can’t fire.
        try
        {
            var rewired = Rewired.ReInput.players.GetPlayer(player.rewiredPlayerId);
            // Use whatever names you actually use for gameplay maps:
            rewired.controllers.maps.SetMapsEnabled(false, "Gameplay");
            rewired.controllers.maps.SetMapsEnabled(false, "Default");
        }
        catch { }

        // Optional: also tell Player to ignore its own custom input
        try
        {
            player.SetInputEnabled(false);
        }
        catch { }

        var anim = player.anim;
        if (anim != null)
        {
            if (HasParam(anim, triggerName)) anim.SetTrigger(triggerName);
            else if (HasParam(anim, "Death")) anim.SetTrigger("Death");
            else if (HasBool(anim, animKey)) anim.SetBool(animKey, true);

            // helpful extra flag if your animator uses it
            if (HasBool(anim, "dead")) anim.SetBool("dead", true);
        }

        // Let the death animation play, then show Game Over
        player.StartCoroutine(WaitThenShowGameOver(anim));
    }

    public override void Update()
    {
        // IMPORTANT:
        // Do NOT call base.Update() here.
        // That base logic is what’s reading input and kicking you back
        // into Dash / Idle / Move, which is why dash can exit your death state.

        // Keep the body frozen.
        player.SetVelocity(0f, 0f);
    }

    public override void Exit()
    {
        var a = player.anim;
        if (a != null)
        {
            // Clear any state flag this state set:
            if (HasBool(a, animKey)) a.SetBool(animKey, false);
            if (HasBool(a, "dead")) a.SetBool("dead", false);

            a.ResetTrigger("die");
            a.ResetTrigger("Death");
            a.ResetTrigger(triggerName);

            // Hard reset animator so it doesn't stay stuck in a partial pose
            a.Rebind();
            a.Update(0f);
        }

        // Re-enable gameplay input when leaving the death state (e.g. on respawn)
        try
        {
            var rewired = Rewired.ReInput.players.GetPlayer(player.rewiredPlayerId);
            rewired.controllers.maps.SetMapsEnabled(true, "Gameplay");
        }
        catch { }

        try
        {
            player.SetInputEnabled(true);
        }
        catch { }

        base.Exit();
    }

    private IEnumerator WaitThenShowGameOver(Animator anim)
    {
        // Let things settle one frame
        yield return null;

        float wait = 1f;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            // give animator a frame to settle
            yield return null;

            if (anim.GetCurrentAnimatorClipInfoCount(0) > 0)
            {
                var clips = anim.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0 && clips[0].clip != null)
                    wait = Mathf.Max(0.25f, clips[0].clip.length);
            }
        }

        yield return new WaitForSeconds(wait);

        // Robust: show regardless of whether an instance already exists
        UI_GameOver.ShowStatic();
    }

    private static bool HasParam(Animator a, string name)
    {
        foreach (var p in a.parameters)
            if (p.name == name)
                return true;
        return false;
    }

    private static bool HasBool(Animator a, string name)
    {
        foreach (var p in a.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                return true;
        return false;
    }
}
