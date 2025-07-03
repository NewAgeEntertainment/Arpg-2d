using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Manager : MonoBehaviour
{
    private Animator animator;  // Animator reference
    public GameObject animatedBook;  // Drag your animated book GameObject here in the Inspector

    void Start()
    {
        // Ensure the Animator is correctly linked to the animated book GameObject
        animator = animatedBook.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator component not found on the AnimatedBook GameObject!");
        }
    }

    void Update()
    {
        // Detect player input to start the animation
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayAnimation();  // Start the animation when input is detected
        }
    }

    void PlayAnimation()
    {
        // Check if the animator is not null and then set the animation to play
        if (animator != null)
        {
            animator.SetBool("isPlaying", true);  // Assuming you have an "isPlaying" bool in the Animator
        }
    }
}


