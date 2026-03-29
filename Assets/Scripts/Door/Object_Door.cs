using System.Collections;
using UnityEngine;
using PixelCrushers;

public class Object_Door : MonoBehaviour
{
    private Animator anim => GetComponentInChildren<Animator>();

    [Header("Door State")]
    [SerializeField] private bool oneUseOnly = true;
    [SerializeField] private bool disableColliderAfterUse = true;
    [SerializeField] private bool hasBeenUsed = false;

    [Header("Audio")]
    [SerializeField] private string doorOpenSfx = "DoorOpen";

    [Header("Animation")]
    [SerializeField] private bool playOpenAnimation = true;
    [SerializeField] private string openBoolName = "doorOpen";
    [SerializeField] private string openTriggerName = "";
    [SerializeField] private float sceneTransitionDelay = 0.35f;

    [Header("Scene Transition")]
    [SerializeField] private bool transitionToScene = true;
    [SerializeField] private string targetSceneName = "";

    [Header("Optional Spawn / Entry Info")]
    [SerializeField] private string targetSpawnPointId = "";



    private bool isTransitioning = false;

    // Called by InteractionTooltipTrigger2D
    public void OnUse(Transform actor)
    {
        TryOpenDoor(actor);
    }

    public void TryOpenDoor(Transform actor = null)
    {
        if (isTransitioning) return;
        if (oneUseOnly && hasBeenUsed) return;

        StartCoroutine(OpenDoorCo(actor));
    }

    private IEnumerator OpenDoorCo(Transform actor)
    {
        isTransitioning = true;
        hasBeenUsed = true;

        PlayDoorOpenSfx();
        PlayDoorAnimation();

        if (disableColliderAfterUse)
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
        }

        if (sceneTransitionDelay > 0f)
            yield return new WaitForSeconds(sceneTransitionDelay);

        if (transitionToScene && !string.IsNullOrWhiteSpace(targetSceneName))
        {
            if (!string.IsNullOrWhiteSpace(targetSpawnPointId))
            {
                PlayerPrefs.SetString("PendingSpawnPointId", targetSpawnPointId);
                PlayerPrefs.Save();
            }

            SaveSystem.LoadScene(targetSceneName);
            yield break;
        }

        isTransitioning = false;
    }

    private void PlayDoorOpenSfx()
    {
        if (AudioManager.instance == null) return;
        if (string.IsNullOrWhiteSpace(doorOpenSfx)) return;

        AudioManager.instance.PlayGlobalSFX(doorOpenSfx);
    }

    private void PlayDoorAnimation()
    {
        if (!playOpenAnimation || anim == null)
            return;

        if (!string.IsNullOrWhiteSpace(openTriggerName))
            anim.SetTrigger(openTriggerName);

        if (!string.IsNullOrWhiteSpace(openBoolName))
            anim.SetBool(openBoolName, true);
    }
}