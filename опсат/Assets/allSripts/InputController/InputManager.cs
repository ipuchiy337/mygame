using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    private PlayerInputActions _actions;

    private bool _jumpDown, _attackDown, _interactDown, _dashDown;
    private bool _uiLeftDown, _uiRightDown, _uiConfirmDown, _uiBackDown;
    private bool _uiLeftHeld, _uiRightHeld;
    private bool _uiUpDown, _uiDownDown;
    private bool _pauseDown;
    private bool _rollDown;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        _actions = new PlayerInputActions();
        _actions.Enable();
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        _actions.Player.Jump.performed += _ => _jumpDown = true;
   
        _actions.Player.Interact.performed += _ => _interactDown = true;
        _actions.Player.Dash.performed += _ => _dashDown = true;
        _actions.Player.UIConfirm.performed += _ => _uiConfirmDown = true;
        _actions.Player.UIBack.performed += _ => _uiBackDown = true;

        _actions.Player.UILeft.performed += _ => { if (!_uiLeftHeld) { _uiLeftDown = true; _uiLeftHeld = true; } };
        _actions.Player.UILeft.canceled += _ => _uiLeftHeld = false;
        _actions.Player.UIRight.performed += _ => { if (!_uiRightHeld) { _uiRightDown = true; _uiRightHeld = true; } };
        _actions.Player.UIRight.canceled += _ => _uiRightHeld = false;
        _actions.Player.UIUp.performed += _ => _uiUpDown = true;
        _actions.Player.UIDown.performed += _ => _uiDownDown = true;

        _actions.Player.Pause.performed += _ => _pauseDown = true;
        _actions.Player.Roll.performed += _ => _rollDown = true;

    }

    void OnDestroy() => _actions?.Disable();

    void LateUpdate()
    {
        _jumpDown = _attackDown = _interactDown = _dashDown = false;
        _uiLeftDown = _uiRightDown = _uiConfirmDown = _uiBackDown = false;
        _uiUpDown = _uiDownDown = false;
        _pauseDown = false;
        _rollDown = false;
    }
    public bool Roll => _rollDown;
    public bool Pause => _pauseDown;
    public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();
    public bool SprintHeld => _actions.Player.Dash.IsPressed();
    public bool JumpHeld => _actions.Player.Jump.IsPressed();
    public bool UIUp => _uiUpDown;
    public bool UIDown => _uiDownDown;
    public bool Jump => _jumpDown;
    public bool Attack => _attackDown;
    public bool Interact => _interactDown
     && !UpgradeUIManager.Instance.IsOpen
     && !ShopCameraManager.Instance.isOpen;
    public bool Dash => _dashDown;
    public bool UILeft => _uiLeftDown;
    public bool UIRight => _uiRightDown;
    public bool UIConfirm => _uiConfirmDown;
    public bool UIBack => _uiBackDown;
}