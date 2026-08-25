using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
public class InputManager : MonoBehaviour
{
    public InputData playerInputs;
    public SpyData spyStats;
    public GameEventData gameEventData;
    public SceneData sceneData;

    PlayerInput playerInput;

    InputAction moveAction;

    InputAction notepadToggleAction;
    InputAction notepadFlipPageAction;

    InputAction writeAction;
    InputAction carouselAction;
    InputAction numpadAction;

    InputAction ticketAction;
    InputAction interactAction;

    InputAction mouseLeftDownAction;
    InputAction mosueLeftPressAction;
    InputAction mouseRightPressAction;

    Action<string> OnDeviceChanged;

    public static InputDevice curDevice;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Player/Movement"];

        notepadToggleAction = playerInput.actions["Player/NotepadToggle"];
        notepadFlipPageAction = playerInput.actions["Player/NotepadFlipPage"];

        writeAction = playerInput.actions["Player/Writing"];
        carouselAction = playerInput.actions["Player/Carousel"];
        numpadAction = playerInput.actions["Player/Numpad"];

        ticketAction = playerInput.actions["Player/Ticket"];
        interactAction = playerInput.actions["Player/Interact"];

        mouseLeftDownAction = playerInput.actions["Player/MouseLeftDown"];
        mosueLeftPressAction = playerInput.actions["Player/MouseLeftPress"];
        mouseRightPressAction = playerInput.actions["Player/MouseRightDown"];

        moveAction.started += context =>
        {
            playerInputs.moveKeyDown = true;
        };
        moveAction.performed += context =>
        {
            float move = context.ReadValue<float>();
            playerInputs.move = (int)move;
        };
        moveAction.canceled += context =>
        {
            playerInputs.move = 0;
            playerInputs.moveKeyUp = true;
        };

        notepadToggleAction.started += context => playerInputs.notepadToggleKeyDown = true;
        notepadToggleAction.canceled += context => playerInputs.notepadToggleKeyUp = true;

        notepadFlipPageAction.started += context =>
        {
            float value = context.ReadValue<float>();
            playerInputs.flipKeyDownValue = (int)value;
        };

        writeAction.started += context => playerInputs.writeKeyDown = true;

        carouselAction.started += context =>
        {
            float carouselValue = context.ReadValue<float>();
            playerInputs.carouselKeyDownValue = (int)carouselValue;
        };

        numpadAction.started += context =>
        {
            InputBinding activeBinding = numpadAction.GetBindingForControl(context.control).Value;
            playerInputs.numpad = numpadAction.GetBindingIndex(activeBinding);
        };

        ticketAction.started += context => playerInputs.ticketCheckKeyDown = true;
        ticketAction.performed += context => playerInputs.ticketCheckKeyHold = true;

        ticketAction.canceled += context =>
        {
            playerInputs.ticketCheckKeyUp = true;
            playerInputs.ticketCheckKeyHold = false;
        };

        interactAction.started += context =>
        {
            playerInputs.interactKeyDown = true;
        };

        mouseLeftDownAction.started += context => playerInputs.mouseLeftDown = true;

        mosueLeftPressAction.performed += context => playerInputs.mouseLeftHold = true;

        mosueLeftPressAction.canceled += context =>
        {
            playerInputs.mouseLeftUp = true;
            playerInputs.mouseLeftHold = false;
        };

        mouseRightPressAction.started += context => playerInputs.mouseRightDown = true;

        mouseRightPressAction.canceled += context =>
        {
            playerInputs.mouseRightUp = true;
        };
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
        playerInputs.mouseScreenPos.z = 0.25f;
        SceneController.SetInputManager(this);
    }
    private void Update()
    {
        if (!sceneData.sceneLoaded) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        playerInputs.mouseScreenPos.x = Mathf.Clamp(screenPos.x, 0f, Screen.width);
        playerInputs.mouseScreenPos.y = Mathf.Clamp(screenPos.y, 0f, Screen.height);
        playerInputs.mouseWorldPos = Camera.main.ScreenToWorldPoint(playerInputs.mouseScreenPos);
    }

    private void LateUpdate()
    {
        playerInputs.notepadToggleKeyDown = false;
        playerInputs.notepadToggleKeyUp = false;
        playerInputs.writeKeyDown = false;
        playerInputs.ticketCheckKeyDown = false;
        playerInputs.ticketCheckKeyUp = false;
        playerInputs.interactKeyDown = false;

        playerInputs.mouseLeftDown = false;
        playerInputs.mouseLeftUp = false;
        playerInputs.mouseRightDown = false;
        playerInputs.mouseRightUp = false;
        playerInputs.moveKeyUp = false;
        playerInputs.moveKeyDown = false;

        playerInputs.carouselKeyDownValue = 0;
        playerInputs.flipKeyDownValue = 0;
        playerInputs.numpad = -1;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }
    private void OnApplicationQuit()
    {
        gameEventData.OnResetTrip?.Raise();
    }
}
