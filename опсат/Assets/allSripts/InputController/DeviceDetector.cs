using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class DeviceDetector : MonoBehaviour
{
    public static DeviceDetector Instance;
    public static bool IsGamepad { get; private set; }
    public static event System.Action<bool> OnDeviceChanged;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        bool wasGamepad = IsGamepad;
        bool nowGamepad = device is Gamepad;

        if (nowGamepad != wasGamepad)
        {
            IsGamepad = nowGamepad;
            OnDeviceChanged?.Invoke(IsGamepad);
        }
    }
}