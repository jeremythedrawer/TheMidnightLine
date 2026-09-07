using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

using static UnityEngine.InputSystem.InputAction;

using static AtlasUI;
using UnityEngine.InputSystem.Controls;
public class InputManager : MonoBehaviour
{
    public InputData inputData;
    public SpyData spyData;
    public PlayerInput playerInput;

    InputAction moveAction;

    InputAction notepadToggleAction;
    InputAction notepadFlipAction;
    InputAction carouselAction;
    InputAction numpadAction;
    InputAction talkAction;
    InputAction interactAction;
    InputAction mouseLeftDownAction;
    InputAction mosueLeftPressAction;
    InputAction mouseRightPressAction;

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
        inputData.notepadToggleKeyDown = false;
        inputData.notepadToggleKeyUp = false;
        inputData.talkKeyDown = false;
        inputData.talkKeyUp = false;
        inputData.interactKeyDown = false;

        inputData.mouseLeftDown = false;
        inputData.mouseLeftUp = false;
        inputData.mouseRightDown = false;
        inputData.mouseRightUp = false;
        inputData.moveKeyUp = false;
        inputData.moveKeyDown = false;

        inputData.carouselKeyDownValue = 0;
        inputData.flipKeyDownValue = 0;
        inputData.numpad = -1;
    }
    private void InitInputs()
    {
        moveAction = playerInput.actions["Player/Movement"];

        notepadToggleAction = playerInput.actions["Player/NotepadToggle"];
        notepadFlipAction = playerInput.actions["Player/NotepadFlipPage"];

        carouselAction = playerInput.actions["Player/Carousel"];
        numpadAction = playerInput.actions["Player/Numpad"];

        talkAction = playerInput.actions["Player/Talk"];
        interactAction = playerInput.actions["Player/Interact"];

        mouseLeftDownAction = playerInput.actions["Player/MouseLeftDown"];
        mosueLeftPressAction = playerInput.actions["Player/MouseLeftPress"];
        mouseRightPressAction = playerInput.actions["Player/MouseRightDown"];

        moveAction.started += OnStartMove;
        moveAction.performed += OnPerformMove;
        moveAction.canceled += OnCancelMove;

        notepadToggleAction.started += OnStartToggleNotepad;
        notepadToggleAction.canceled += OnCancelToggleNotepad;

        notepadFlipAction.started += OnStartNotepadFlip;

        carouselAction.started += OnStartCarousel;

        numpadAction.started += OnStartNumpad;

        talkAction.started += OnStartTalk;
        talkAction.performed += OnPerformTalk;

        talkAction.canceled += OnCancelTalk;

        interactAction.started += OnStartInteract;

        mouseLeftDownAction.started += OnStartLeftMouse;

        mosueLeftPressAction.performed += OnPerformLeftMouse;

        mosueLeftPressAction.canceled += OnCancelLeftMouse;

        mouseRightPressAction.started += OnStartRightMouse;

        mouseRightPressAction.canceled += OnCancelRightMouse;
    }

    private void SetKeybindSpriteIndices()
    {
        inputData.interactSpriteIndex = GetKeybindSpriteIndex(talkAction);
    }

    private void OnStartMove(CallbackContext ctx)
    {
        inputData.moveKeyDown = true;
    }
    private void OnPerformMove(CallbackContext ctx)
    {
        float move = ctx.ReadValue<float>();
        inputData.move = (int)move;
    }
    private void OnCancelMove(CallbackContext ctx)
    {
        inputData.move = 0;
        inputData.moveKeyUp = true;
    }
    private void OnStartToggleNotepad(CallbackContext ctx)
    {
        inputData.notepadToggleKeyDown = true;
    }
    private void OnCancelToggleNotepad(CallbackContext ctx)
    {
        inputData.notepadToggleKeyUp = true;
    }
    private void OnStartNotepadFlip(CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        inputData.flipKeyDownValue = (int)value;
    }
    private void OnStartCarousel(CallbackContext ctx)
    {
        float carouselValue = ctx.ReadValue<float>();
        inputData.carouselKeyDownValue = (int)carouselValue;
    }
    private void OnStartTalk(CallbackContext ctx)
    {
        inputData.talkKeyDown = true;
    }
    private void OnStartNumpad(CallbackContext ctx)
    {
        InputBinding activeBinding = numpadAction.GetBindingForControl(ctx.control).Value;
        inputData.numpad = numpadAction.GetBindingIndex(activeBinding);
    }
    private void OnPerformTalk(CallbackContext ctx)
    {
        inputData.talkKeyHold = true;
    }
    private void OnCancelTalk(CallbackContext ctx)
    {
        inputData.talkKeyUp = true;
        inputData.talkKeyHold = false;
    }
    private void OnStartInteract(CallbackContext ctx)
    {
        inputData.interactKeyDown = true;
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
    private void OnStartRightMouse(CallbackContext ctx)
    {
        inputData.mouseRightDown = true;
    }
    private void OnCancelRightMouse(CallbackContext ctx)
    {
        inputData.mouseRightUp = true;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }

    private KeybindSpriteIndex GetKeybindSpriteIndex(InputAction action)
    {
        string path = action.bindings[0].effectivePath;

        string keyName = path.Replace("<Keyboard>/", "").ToLower();

        if (keyName.StartsWith("digit")) return (KeybindSpriteIndex)int.Parse(keyName.Substring(startIndex: 5));

        if (keyName.Length == 1 && char.IsLetter(keyName[0]))
        {
            int index = 10 + (char.ToUpper(keyName[0]) - 'A');
            return (KeybindSpriteIndex)index;
        }

        if (keyName == "space") return KeybindSpriteIndex.Spacebar;

        throw new ArgumentException($"Unsupported key: {keyName}");
    }

}
