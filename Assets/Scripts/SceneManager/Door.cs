using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string boolName = "IsOpen";

    private bool isOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            FindObjectOfType<Door>().ToggleDoor();
        }
    }

    private void Reset()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        animator.SetBool(boolName, isOpen);
    }

    public void OpenDoor()
    {
        isOpen = true;
        animator.SetBool(boolName, true);
    }

    public void CloseDoor()
    {
        isOpen = false;
        animator.SetBool(boolName, false);
    }
}
