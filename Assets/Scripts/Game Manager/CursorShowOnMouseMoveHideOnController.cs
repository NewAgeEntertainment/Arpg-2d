using UnityEngine;
using Rewired;

public class CursorShowOnMouseMoveHideOnController : MonoBehaviour
{
    [Header("Rewired")]
    [SerializeField] private int rewiredPlayerId = 0;

    [Header("Behavior")]
    [SerializeField] private float mouseMovePixels = 1.5f;     // movement threshold
    [SerializeField] private float hideDelayAfterMouseIdle = 1.0f;


    private Rewired.Player rwPlayer;
    private Vector3 lastMousePos;
    private float lastMouseMoveTime;
    private bool cursorVisible;

    void Awake()
    {
        rwPlayer = ReInput.players.GetPlayer(rewiredPlayerId);
        lastMousePos = Input.mousePosition;
        SetCursorVisible(false); // start hidden for controller play
    }

    void Update()
    {
        // Always keep cursor unlocked; only toggle visibility:
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;

        // --- Mouse activity ---
        Vector3 mp = Input.mousePosition;
        float mouseDelta = (mp - lastMousePos).magnitude;
        lastMousePos = mp;

        bool mouseMoved = mouseDelta >= mouseMovePixels;
        bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);

        if (mouseMoved || mouseClicked)
        {
            lastMouseMoveTime = Time.unscaledTime;
            SetCursorVisible(true);
            return; // mouse is currently “in control”
        }

        // --- Controller activity ---
        // Any joystick button or axis movement counts as controller intent.
        bool controllerUsed = false;

        // Buttons:
        for (int i = 0; i < ReInput.controllers.Joysticks.Count; i++)
        {
            var joy = ReInput.controllers.Joysticks[i];
            if (joy == null) continue;

            // any button down:
            for (int b = 0; b < joy.buttonCount; b++)
            {
                if (joy.GetButtonDown(b)) { controllerUsed = true; break; }
            }
            if (controllerUsed) break;

            // any axis moved:
            for (int a = 0; a < joy.axisCount; a++)
            {
                if (Mathf.Abs(joy.GetAxis(a)) > 0.25f) { controllerUsed = true; break; }
            }
            if (controllerUsed) break;
        }

        if (controllerUsed)
        {
            SetCursorVisible(false);
            return;
        }

        // If mouse was used recently, keep visible until idle timeout:
        if (cursorVisible && Time.unscaledTime - lastMouseMoveTime > hideDelayAfterMouseIdle)
        {
            // Only hide if controller hasn’t taken over; you can choose to keep it visible.
            // If you want it to remain visible after mouse use until controller input, comment this out.
            SetCursorVisible(false);
        }
    }

    private void SetCursorVisible(bool visible)
    {
        if (cursorVisible == visible) return;
        cursorVisible = visible;
        Cursor.visible = visible;
    }
}
