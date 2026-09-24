using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

using static UnityEngine.InputSystem.InputAction;

using static AtlasUI;
public class InputManager : MonoBehaviour
{
    public InputData inputData;
    public SpyData spyData;
    public PlayerInput playerInput;

    public InputAction horizontalAction;
    public InputAction verticalAction;
    public InputAction primaryInteractAction;
    public InputAction secondaryInteractAction;
    public InputAction focusAction;
    public InputAction numpadAction;
    public InputAction cursorLeftDownAction;

    Action<string> OnDeviceChanged;

    public static InputDevice curDevice;

    private void Awake()
    {
        InitInputs();
        SetKeybindSpriteIndices();
    }

    private void OnEnable()
    {
        ++InputUser.listenForUnpairedDeviceActivity;
        InputUser.onUnpairedDeviceUsed += CheckDevice;
    }
    private void OnDisable()
    {
        InputUser.onUnpairedDeviceUsed -= CheckDevice;
    }

    private void Start()
    {
        inputData.mouseScreenPos.z = 0.25f;
    }
    private void Update()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();

        inputData.mouseScreenPos.x = Mathf.Clamp(screenPos.x, 0f, Screen.width);
        inputData.mouseScreenPos.y = Mathf.Clamp(screenPos.y, 0f, Screen.height);

        inputData.mouseWorldPos = Camera.main.ScreenToWorldPoint(inputData.mouseScreenPos);
    }

    private void LateUpdate()
    {
        inputData.horizontalInputAxis.keyDownValue = 0;
        inputData.horizontalInputAxis.keyUpValue = 0;

        inputData.verticalInputAxis.keyDownValue = 0;
        inputData.verticalInputAxis.keyUpValue = 0;

        inputData.primaryInteractInputTrigger.keyDown = false;
        inputData.primaryInteractInputTrigger.keyUp = false;

        inputData.secondaryInteractInputTrigger.keyDown = false;
        inputData.secondaryInteractInputTrigger.keyUp = false;

        inputData.focusInputTrigger.keyDown = false;
        inputData.focusInputTrigger.keyUp = false;

        inputData.numpad = -1;
        inputData.mouseLeftDown = false;
        inputData.mouseLeftUp = false;
    }
    private void InitInputs()
    {
        horizontalAction = playerInput.actions["Player/Horizontal"];
        verticalAction = playerInput.actions["Player/Vertical"];
        primaryInteractAction = playerInput.actions["Player/PrimaryInteract"];
        secondaryInteractAction = playerInput.actions["Player/SecondaryInteract"];
        focusAction = playerInput.actions["Player/Focus"];
        numpadAction = playerInput.actions["Player/Numpad"];
        cursorLeftDownAction = playerInput.actions["Player/CursorLeftDown"];

        horizontalAction.started += inputData.horizontalInputAxis.OnStart;
        horizontalAction.canceled += inputData.horizontalInputAxis.OnCancel;

        verticalAction.started += inputData.verticalInputAxis.OnStart;
        verticalAction.canceled += inputData.verticalInputAxis.OnCancel;

        primaryInteractAction.started += inputData.primaryInteractInputTrigger.OnStart;
        primaryInteractAction.canceled += inputData.primaryInteractInputTrigger.OnCancel;

        secondaryInteractAction.started += inputData.secondaryInteractInputTrigger.OnStart;
        secondaryInteractAction.canceled += inputData.secondaryInteractInputTrigger.OnCancel;

        focusAction.started += inputData.focusInputTrigger.OnStart;
        focusAction.canceled += inputData.focusInputTrigger.OnCancel;

        numpadAction.started += OnStartNumpad;

        cursorLeftDownAction.started += OnStartLeftMouse;
        cursorLeftDownAction.performed += OnPerformLeftMouse;
        cursorLeftDownAction.canceled += OnCancelLeftMouse;
    }

    private void SetKeybindSpriteIndices()
    {
        inputData.horizontalInputAxis.postiveKeybindSpriteIndex = GetAxisKeybindSpriteIndex(horizontalAction, isPositive: true);
        inputData.horizontalInputAxis.negativeKeybindSpriteIndex = GetAxisKeybindSpriteIndex(horizontalAction, isPositive: false);
    }

    private void OnStartNumpad(CallbackContext ctx)
    {
        InputBinding activeBinding = numpadAction.GetBindingForControl(ctx.control).Value;
        inputData.numpad = numpadAction.GetBindingIndex(activeBinding);
    }
    private void OnStartLeftMouse(CallbackContext ctx)
    {
        inputData.mouseLeftDown = true;
    }
    private void OnPerformLeftMouse(CallbackContext ctx)
    {
        inputData.mouseLeftHold = true;
    }
    private void OnCancelLeftMouse(CallbackContext ctx)
    {
        inputData.mouseLeftUp = true;
        inputData.mouseLeftHold = false;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }

    [ContextMenu("Axis Test")] public KeybindSpriteIndex GetAxisKeybindSpriteIndex(InputAction action, bool isPositive)
    {
        string path = action.bindings[0].effectivePath;

        string keyName = path.Replace("<Keyboard>/", "").ToLower();

        return KeybindSpriteIndex.Q;
    }

}
