using System;
using System.Collections;
using UnityEngine;

public class BookOpenManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator bookAnimator; // drag your Animator here

    [Tooltip("Book open state name (match your Animator)")]
    [SerializeField] private string openStateName = "Open"; // match your Animator state name!

    [Tooltip("Book open animation length fallback (seconds)")]
    [SerializeField] private float openDuration = 1.0f;

    /// <summary>
    /// Call this to open the book if needed, then run your callback after it's open.
    /// </summary>
    public void OpenBookIfNeeded(Action onOpened)
    {
        if (bookAnimator == null)
        {
            Debug.LogWarning("[BookOpenManager] No animator found!");
            onOpened?.Invoke();
            return;
        }

        bool isOpen = bookAnimator.GetBool("Open");

        if (isOpen)
        {
            Debug.Log("[BookOpenManager] Book already open, running callback immediately.");
            onOpened?.Invoke();
        }
        else
        {
            Debug.Log("[BookOpenManager] Opening book...");
            bookAnimator.SetBool("Open", true);
            StartCoroutine(WaitForOpenThenRun(onOpened));
        }
    }

    private IEnumerator WaitForOpenThenRun(Action callback)
    {
        // Wait until Animator transitions into the Open state
        while (!bookAnimator.GetCurrentAnimatorStateInfo(0).IsName(openStateName))
            yield return null;

        // Wait until that state finishes
        while (bookAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        callback?.Invoke();
    }

    /// <summary>
    /// Optional: manually toggle open/close.
    /// </summary>
    public void ToggleBook()
    {
        bool newState = !bookAnimator.GetBool("Open");
        bookAnimator.SetBool("Open", newState);
        Debug.Log($"[BookOpenManager] Book now {(newState ? "OPEN" : "CLOSED")}.");
    }
}
