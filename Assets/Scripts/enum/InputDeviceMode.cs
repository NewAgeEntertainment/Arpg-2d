using System;
using UnityEngine;

public enum InputDeviceMode
{
    Keyboard,
    Controller
}

public class InputDeviceModeManager : MonoBehaviour
{
    public static InputDeviceModeManager Instance { get; private set; }

    public static event Action<InputDeviceMode> OnInputDeviceModeChanged;

    private const string PrefKey = "InputDeviceMode";

    [SerializeField] private InputDeviceMode currentMode = InputDeviceMode.Keyboard;

    public InputDeviceMode CurrentMode => currentMode;
    public bool IsKeyboard => currentMode == InputDeviceMode.Keyboard;
    public bool IsController => currentMode == InputDeviceMode.Controller;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadMode();
    }

    public void LoadMode()
    {
        currentMode = (InputDeviceMode)PlayerPrefs.GetInt(
            PrefKey,
            (int)InputDeviceMode.Keyboard
        );

        Debug.Log($"[InputDeviceMode] Loaded mode: {currentMode}");

        OnInputDeviceModeChanged?.Invoke(currentMode);
    }

    public void SetKeyboardMode()
    {
        SetMode(InputDeviceMode.Keyboard);
    }

    public void SetControllerMode()
    {
        SetMode(InputDeviceMode.Controller);
    }

    // ✅ Use this directly from your Toggle.
    // Toggle OFF = Keyboard
    // Toggle ON = Controller
    public void SetModeFromToggle(bool isController)
    {
        Debug.Log($"[InputDeviceMode] Toggle changed. IsController={isController}");

        if (isController)
            SetControllerMode();
        else
            SetKeyboardMode();
    }

    public void SetMode(InputDeviceMode mode)
    {
        currentMode = mode;

        PlayerPrefs.SetInt(PrefKey, (int)currentMode);
        PlayerPrefs.Save();

        Debug.Log($"[InputDeviceMode] Switched to {currentMode}");

        OnInputDeviceModeChanged?.Invoke(currentMode);
    }
}