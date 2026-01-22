using Proselyte.Sigils;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

public class InputManager : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public PlayerInputsSO playerInputs;
        public SpyStatsSO spyStats;
        public GameEventDataSO gameEventData;
    }
    [SerializeField] SOData soData;

    PlayerInput playerInput;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction runAction;
    InputAction clipboardAction;
    InputAction mouseLeftDownAction;
    InputAction mouseLeftPressAction;
    InputAction interactAction;
    InputAction cancelAction;
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
        clipboardAction = playerInput.actions["Player/Clipboard"];
        mouseLeftDownAction = playerInput.actions["Player/MouseLeftDown"];
        mouseLeftPressAction = playerInput.actions["Player/MouseLeftPress"];

        cancelAction = playerInput.actions["Player/Cancel"];

        moveAction.performed += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            soData.playerInputs.move = Mathf.RoundToInt(move.x);
        };
        moveAction.canceled += context =>
        {
            soData.playerInputs.move = 0;
        };

        clipboardAction.performed += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            soData.playerInputs.mouseScroll = Mathf.RoundToInt(move.y);
        };

        clipboardAction.canceled += context =>
        {
            soData.playerInputs.mouseScroll = 0;
        };

        interactAction.started += context =>
        {
            soData.gameEventData.OnInteract.Raise();
            soData.playerInputs.interact = true;
        };

        mouseLeftDownAction.started += context => soData.playerInputs.mouseLeftDown = true;

        mouseLeftPressAction.performed += context => soData.playerInputs.mouseLeftPress = true;
        mouseLeftPressAction.canceled += context =>
        {
            soData.playerInputs.mouseLeftUp = true;
            soData.playerInputs.mouseLeftPress = false;
        };

            jumpAction.performed += context => soData.playerInputs.jump = true;
        jumpAction.canceled += context => soData.playerInputs.jump = false;

        runAction.performed += context => soData.playerInputs.run = true;
        runAction.canceled += context => soData.playerInputs.run = false;

        cancelAction.started += context => soData.playerInputs.cancel = true;
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
                soData.gameEventData.OnReset.Raise();
            }
        }
        soData.playerInputs.mouseScreenPos = Mouse.current.position.ReadValue();
        soData.playerInputs.mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (soData.playerInputs.mouseLeftDown)
        {
            soData.playerInputs.startDragMouseScreenPos = soData.playerInputs.mouseScreenPos;
        }
        if (soData.playerInputs.mouseLeftUp)
        {
            soData.playerInputs.endDragMouseScreenPos = soData.playerInputs.mouseScreenPos;
        }
    }

    private void LateUpdate()
    {
        soData.playerInputs.cancel = false;
        soData.playerInputs.interact = false;
        soData.playerInputs.mouseLeftDown = false;
        soData.playerInputs.mouseLeftUp = false;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }
    private void ResetData()
    {
        soData.playerInputs.jump = false;
    }
    private void OnApplicationQuit()
    {
        soData.gameEventData.OnReset.Raise();
        ResetData();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(Camera.main.ScreenToWorldPoint(soData.playerInputs.startDragMouseScreenPos), Camera.main.ScreenToWorldPoint(soData.playerInputs.mouseScreenPos));
    }
}
