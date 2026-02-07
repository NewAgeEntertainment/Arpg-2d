using UnityEngine;

public class BookUIController : MonoBehaviour
{
    [SerializeField] private Animator bookAnimator;
    private static readonly int IsMenuOpen = Animator.StringToHash("IsMenuOpen");

    public void SetMenuOpen(bool open)
    {
        if (!bookAnimator) return;

        // Ensure animator keeps running even when Time.timeScale = 0
        bookAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        bookAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        bookAnimator.SetBool(IsMenuOpen, open);

        // Optional: force immediate visual state (prevents 1-frame delay)
        // Match these names to your animator state names:
        if (open)
            bookAnimator.Play("OpenIdle", 0, 0f);   // or "Open" if you want open->idle
        else
            bookAnimator.Play("ClosedIdle", 0, 0f);
    }
}
