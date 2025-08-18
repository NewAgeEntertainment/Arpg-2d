using UnityEngine;

public class SexyTimeStarter : MonoBehaviour
{
    [Header("Assign the root that has SexyTimeLogic")]
    [SerializeField] private GameObject sexyTimeLogicObject;

    [Header("Fallback Key (optional)")]
    [SerializeField] private bool allowKeyboardFallback = true;
    [SerializeField] private KeyCode fallbackKey = KeyCode.L;

    private SexyTimeLogic logic;
    private SexyTimeInputRouter router;

    void Awake()
    {
        if (sexyTimeLogicObject == null)
        {
            Debug.LogError("[SexyTimeStarter] 'sexyTimeLogicObject' is not assigned.");
            return;
        }

        logic = sexyTimeLogicObject.GetComponent<SexyTimeLogic>();
        if (logic == null)
        {
            Debug.LogError("[SexyTimeStarter] No SexyTimeLogic found on the assigned object.");
            return;
        }

        router = logic.GetComponent<SexyTimeInputRouter>();
        if (router == null)
        {
            // Not fatal; we can still use fallback key
            Debug.Log("[SexyTimeStarter] No SexyTimeInputRouter found; will rely on fallback key only.");
        }
    }

    void Update()
    {
        if (logic == null) return;

        bool trigger =
            (router != null && router.StartPressed()) ||
            (allowKeyboardFallback && Input.GetKeyDown(fallbackKey));

        if (!trigger) return;

        // Ensure it is visible/active first
        if (!sexyTimeLogicObject.activeSelf)
            sexyTimeLogicObject.SetActive(true);

        // Always attempt to start (handles its own guards)
        logic.StartSexyTime();
    }
}
