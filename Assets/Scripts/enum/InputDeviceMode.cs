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

        currentMode = (InputDeviceMode)PlayerPrefs.GetInt(
            PrefKey,
            (int)InputDeviceMode.Keyboard
        );
    }

    public void SetKeyboardMode()
    {
        SetMode(InputDeviceMode.Keyboard);
    }

    public void SetControllerMode()
    {
        SetMode(InputDeviceMode.Controller);
    }

    public void ToggleMode()
    {
        SetMode(currentMode == InputDeviceMode.Keyboard
            ? InputDeviceMode.Controller
            : InputDeviceMode.Keyboard);
    }

    public void SetMode(InputDeviceMode mode)
    {
        if (currentMode == mode)
            return;

        currentMode = mode;

        PlayerPrefs.SetInt(PrefKey, (int)currentMode);
        PlayerPrefs.Save();

        Debug.Log($"[InputDeviceMode] Switched to {currentMode}");

        OnInputDeviceModeChanged?.Invoke(currentMode);
    }
}