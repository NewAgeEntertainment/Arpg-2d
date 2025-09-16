using UnityEngine;

/// Add this to the Player root. It creates/configures an Animator dedicated to Timeline.
/// Your gameplay Animator on the child remains the one your code uses.
[DisallowMultipleComponent]
public class TimelineOnlyAnimator : MonoBehaviour
{
    [Tooltip("Leave null to auto-find your gameplay (child) Animator.")]
    public Animator gameplayAnimator;         // the child animator (used by your code)

    [Tooltip("Created/managed automatically. Timeline will bind to this.")]
    public Animator timelineAnimator;         // the parent/root animator (Timeline-only)

    [Header("Safety")]
    public bool disableTimelineAnimatorWhenNotPlaying = true;

    void Awake()
    {
        // Ensure gameplay animator reference (child)
        if (gameplayAnimator == null)
        {
            // Prefer child animator over a root one
            var anims = GetComponentsInChildren<Animator>(true);
            foreach (var a in anims)
            {
                if (a.gameObject != this.gameObject) { gameplayAnimator = a; break; }
            }
        }

        // Ensure / configure the Timeline-only animator on the root
        timelineAnimator = GetComponent<Animator>();
        if (timelineAnimator == null)
            timelineAnimator = gameObject.AddComponent<Animator>();

        // Make sure this root Animator is NOT used by gameplay:
        // - no controller
        // - no avatar
        // - no root motion
        // - stays disabled unless Timeline is playing
        timelineAnimator.runtimeAnimatorController = null;
        timelineAnimator.avatar = null;
        timelineAnimator.applyRootMotion = false;
        timelineAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        if (disableTimelineAnimatorWhenNotPlaying)
            timelineAnimator.enabled = false;
    }

    /// Call when a cutscene starts; TimelineAnimatorBinder will do this automatically.
    public void EnableForTimeline()
    {
        if (timelineAnimator != null) timelineAnimator.enabled = true;
    }

    /// Call when a cutscene ends; TimelineAnimatorBinder will do this automatically.
    public void DisableWhenIdle()
    {
        if (timelineAnimator != null) timelineAnimator.enabled = false;
    }
}
