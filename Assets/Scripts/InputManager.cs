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

    InputAction move_action;

    InputAction notepadToggle_action;
    InputAction notepadFlipPage_action;

    InputAction write_action;
    InputAction preview_action;
    InputAction numpad_action;

    InputAction ticket_action;
    InputAction interact_action;

    InputAction mouseLeftDown_action;
    InputAction mouseLeftPress_action;
    InputAction mouseRightDown_action;

    Action<string> OnDeviceChanged;

    public static InputDevice curDevice;

    private float resetElaspedTime;
    private float resetThresholdTime = 2;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        move_action = playerInput.actions["Player/Movement"];

        notepadToggle_action = playerInput.actions["Player/NotepadToggle"];
        notepadFlipPage_action = playerInput.actions["Player/NotepadFlipPage"];

        write_action = playerInput.actions["Player/Writing"];
        preview_action = playerInput.actions["Player/Preview"];
        numpad_action = playerInput.actions["Player/Numpad"];

        ticket_action = playerInput.actions["Player/Ticket"];
        interact_action = playerInput.actions["Player/Interact"];

        mouseLeftDown_action = playerInput.actions["Player/MouseLeftDown"];
        mouseLeftPress_action = playerInput.actions["Player/MouseLeftPress"];

        mouseRightDown_action = playerInput.actions["Player/MouseRightDown"];

        move_action.performed += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerInputs.move = Mathf.RoundToInt(move.x);
        };
        move_action.canceled += context =>
        {
            playerInputs.move = 0;
        };

        notepadToggle_action.started += context => playerInputs.notepadKeyDown = true;
        notepadFlipPage_action.started += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerInputs.notepadPreviewAnswerAndFlip.y = Mathf.RoundToInt(move.y);
        };

        write_action.started += context => playerInputs.notepadConfirmAnswer = true;
        preview_action.started += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerInputs.notepadPreviewAnswerAndFlip.x = Mathf.RoundToInt(move.x);
        };

        numpad_action.started += context =>
        {
            InputBinding activeBinding = numpad_action.GetBindingForControl(context.control).Value;
            playerInputs.numpad = numpad_action.GetBindingIndex(activeBinding);
        };

        ticket_action.started += context => playerInputs.ticketCheckKeyDown = true;

        interact_action.started += context =>
        {
            gameEventData.OnInteract.Raise();
            playerInputs.interact = true;
        };

        mouseLeftDown_action.started += context => playerInputs.mouseLeftDown = true;
        mouseRightDown_action.started += context => playerInputs.mouseRightDown = true;


        mouseLeftPress_action.performed += context => playerInputs.mouseLeftHold = true;
        mouseLeftPress_action.canceled += context =>
        {
            playerInputs.mouseLeftUp = true;
            playerInputs.mouseLeftHold = false;
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
        playerInputs.mouseScreenPos.z = 1;
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
        Vector2 screenPos = Mouse.current.position.ReadValue();
        playerInputs.mouseScreenPos.x = Mathf.Clamp(screenPos.x, 0f, Screen.width);
        playerInputs.mouseScreenPos.y = Mathf.Clamp(screenPos.y, 0f, Screen.height);
        playerInputs.mouseWorldPos = Camera.main.ScreenToWorldPoint(playerInputs.mouseScreenPos);

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
        playerInputs.notepadKeyDown = false;
        playerInputs.notepadConfirmAnswer = false;
        playerInputs.ticketCheckKeyDown = false;
        playerInputs.interact = false;
        playerInputs.mouseLeftDown = false;
        playerInputs.mouseLeftUp = false;
        playerInputs.mouseRightDown = false;
        playerInputs.notepadPreviewAnswerAndFlip.x = 0;
        playerInputs.notepadPreviewAnswerAndFlip.y = 0;
        playerInputs.numpad = -1;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }
    private void OnApplicationQuit()
    {
        gameEventData.OnReset.Raise();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(Camera.main.ScreenToWorldPoint(playerInputs.startDragMouseScreenPos), Camera.main.ScreenToWorldPoint(playerInputs.mouseScreenPos));
    }
}
