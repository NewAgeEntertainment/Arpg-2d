using System.Collections;
using UnityEngine;

public class Player_DeathState : PlayerState
{
    private readonly Player player;
    private readonly string animKey;
    [SerializeField] private string triggerName = "death";

    public Player_DeathState(Player player, StateMachine sm, string animBoolName)
        : base(player, sm, animBoolName)
    {
        this.player = player;
        this.animKey = animBoolName;
    }

    

    public override void Enter()
    {
        base.Enter();

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;

        // Disable inputs safely
        try
        {
            var rewired = Rewired.ReInput.players.GetPlayer(player.rewiredPlayerId);
            rewired.controllers.maps.SetMapsEnabled(false, "Default");
        }
        catch { }

        var anim = player.anim;
        if (anim != null)
        {
            if (HasParam(anim, triggerName)) anim.SetTrigger(triggerName);
            else if (HasParam(anim, "Death")) anim.SetTrigger("Death");
            else if (HasBool(anim, animKey)) anim.SetBool(animKey, true);
        }

        player.StartCoroutine(WaitThenShowGameOver(anim));
    }

    public override void Exit()
    {
        var a = player.anim;
        if (a != null)
        {
            // Clear any state flag this state set:
            if (HasBool(a, animKey)) a.SetBool(animKey, false);
            a.SetBool("dead", false);           // in case your animKey isn’t “dead”
            a.ResetTrigger("die");              // if you used a trigger too

            a.Rebind();                         // << force animator back to defaults
            a.Update(0f);                       // << apply immediately
        }
    }

    private IEnumerator WaitThenShowGameOver(Animator anim)
    {
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

        // ✅ Robust: show regardless of whether an instance already exists
        UI_GameOver.ShowStatic();
    }

    private static bool HasParam(Animator a, string name)
    {
        foreach (var p in a.parameters) if (p.name == name) return true;
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
