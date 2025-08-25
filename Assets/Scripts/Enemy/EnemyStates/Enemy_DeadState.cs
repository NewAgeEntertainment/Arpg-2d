using UnityEngine;
using System.Collections;

public class Enemy_DeadState : EnemyState
{
    // Optional tiny pause after clip finishes
    private const float ExtraDespawnDelay = 0.1f;
    private const int Layer = 0; // base layer

    public Enemy_DeadState(Enemy enemy, StateMachine stateMachine, string animBoolName)
        : base(enemy, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // Stop movement/physics; let the animation play visually
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        // Make the corpse non-interactive while dying
        ToggleAllColliders(enemy, false);

        // Play the death anim from the start. If you prefer, use a "dead" trigger.
        anim.Play(anim.GetCurrentAnimatorStateInfo(Layer).shortNameHash, Layer, 0f);
        enemy.StartCoroutine(DespawnAfterDeathAnim());
        Debug.Log("Entered dead State");
    }

    private IEnumerator DespawnAfterDeathAnim()
    {
        // 1) Wait until Animator actually enters any state tagged "Death"
        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(Layer);
            if (st.IsTag("Death")) break;
            yield return null;
        }

        // 2) Grab current clip length (safer than relying only on normalizedTime)
        float clipLen = GetCurrentClipLength(anim, Layer);
        if (clipLen <= 0f) clipLen = 0.1f;

        // 3) Wait until the state is finished OR the clip length elapses (covers looping mistakes)
        float t = 0f;
        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(Layer);

            // If the state isn't marked as looping and it's finished, we're done
            if (!st.loop && st.normalizedTime >= 1f) break;

            // Fallback timer in case the state is accidentally set to loop
            t += Time.deltaTime * Mathf.Max(0.0001f, anim.speed);
            if (t >= clipLen) break;

            yield return null;
        }

        if (ExtraDespawnDelay > 0f) yield return new WaitForSeconds(ExtraDespawnDelay);

        // If you pool, SetActive(false) instead of Destroy
        Object.Destroy(enemy.gameObject);
    }

    private static float GetCurrentClipLength(Animator animator, int layer)
    {
        var info = animator.GetCurrentAnimatorClipInfo(layer);
        return (info != null && info.Length > 0 && info[0].clip != null) ? info[0].clip.length : 0f;
    }

    private static void ToggleAllColliders(Enemy e, bool enable)
    {
        var cols = e.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = enable;
    }
}
