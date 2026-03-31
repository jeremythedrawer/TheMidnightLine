using Proselyte.Sigils;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
public class InputManager : MonoBehaviour
{
    public PlayerInputsSO playerInputs;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;

    PlayerInput playerInput;

    InputAction moveAction;
    InputAction jumpAction;
    InputAction runAction;
    InputAction notepadActiveAction;
    InputAction notepadFlipPageAction;
    InputAction mouseLeftDownAction;
    InputAction mouseLeftPressAction;
    InputAction interactAction;
    InputAction cancelAction;
    InputAction ticketAction;
    Action<string> OnDeviceChanged;

    public static InputDevice curDevice;


    private float resetElaspedTime;
    private float resetThresholdTime = 2;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Player/Movement"];
        jumpAction = playerInput.actions["Player/Jump"];
        runAction = playerInput.actions["Player/Run"];
        interactAction = playerInput.actions["Player/Interact"];
        notepadActiveAction = playerInput.actions["Player/NotepadActive"];
        notepadFlipPageAction = playerInput.actions["Player/NotepadFlipPage"];
        mouseLeftDownAction = playerInput.actions["Player/MouseLeftDown"];
        mouseLeftPressAction = playerInput.actions["Player/MouseLeftPress"];

        cancelAction = playerInput.actions["Player/Cancel"];
        ticketAction = playerInput.actions["Player/Ticket"];

        moveAction.performed += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerInputs.move = Mathf.RoundToInt(move.x);
        };
        moveAction.canceled += context =>
        {
            playerInputs.move = 0;
        };

        notepadActiveAction.started += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerInputs.notepad.x = Mathf.RoundToInt(move.x);
        };

        notepadFlipPageAction.started += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerInputs.notepad.y = Mathf.RoundToInt(move.y);
        };

        interactAction.started += context =>
        {
            gameEventData.OnInteract.Raise();
            playerInputs.interact = true;
        };

        mouseLeftDownAction.started += context => playerInputs.mouseLeftDown = true;

        mouseLeftPressAction.performed += context => playerInputs.mouseLeftPress = true;
        mouseLeftPressAction.canceled += context =>
        {
            playerInputs.mouseLeftUp = true;
            playerInputs.mouseLeftPress = false;
        };

        jumpAction.performed += context => playerInputs.jump = true;
        jumpAction.canceled += context => playerInputs.jump = false;

        runAction.performed += context => playerInputs.run = true;
        runAction.canceled += context => playerInputs.run = false;

        cancelAction.started += context => playerInputs.cancel = true;
        ticketAction.started += context => playerInputs.ticket = true;
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

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.RightShift))
        {
            resetElaspedTime += Time.deltaTime;
            if (resetElaspedTime > resetThresholdTime)
            {
                resetElaspedTime = 0;
                gameEventData.OnReset.Raise();
            }
        }
        playerInputs.mouseScreenPos = Mouse.current.position.ReadValue();
        playerInputs.mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (playerInputs.mouseLeftDown)
        {
            playerInputs.startDragMouseScreenPos = playerInputs.mouseScreenPos;
        }
        if (playerInputs.mouseLeftUp)
        {
            playerInputs.endDragMouseScreenPos = playerInputs.mouseScreenPos;
        }
    }

    private void LateUpdate()
    {
        playerInputs.cancel = false;
        playerInputs.interact = false;
        playerInputs.mouseLeftDown = false;
        playerInputs.mouseLeftUp = false;
        playerInputs.notepad.x = 0;
        playerInputs.notepad.y = 0;
        playerInputs.ticket = false;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }
    private void ResetData()
    {
        playerInputs.jump = false;
    }
    private void OnApplicationQuit()
    {
        gameEventData.OnReset.Raise();
        ResetData();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(Camera.main.ScreenToWorldPoint(playerInputs.startDragMouseScreenPos), Camera.main.ScreenToWorldPoint(playerInputs.mouseScreenPos));
    }
}
