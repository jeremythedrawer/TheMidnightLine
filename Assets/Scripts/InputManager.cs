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
        public SpyInputsSO spyInputs;
        public SpyStatsSO spyStats;
        public GameEventDataSO gameEventData;
    }
    [SerializeField] SOData soData;

    PlayerInput playerInput;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction runAction;
    InputAction clipboardAction;
    InputAction selectNPCAction;
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
        selectNPCAction = playerInput.actions["Player/SelectNPC"];
        cancelAction = playerInput.actions["Player/Cancel"];

        moveAction.performed += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            soData.spyInputs.move = Mathf.RoundToInt(move.x);
        };
        moveAction.canceled += context =>
        {
            soData.spyInputs.move = 0;
        };

        clipboardAction.performed += context =>
        {
            Vector2 move = context.ReadValue<Vector2>();
            soData.spyInputs.clipboard = Mathf.RoundToInt(move.y);
        };

        clipboardAction.canceled += context =>
        {
            soData.spyInputs.clipboard = 0;
        };

        interactAction.started += context =>
        {
            soData.gameEventData.OnInteract.Raise();
            soData.spyInputs.interact = true;
        };

        selectNPCAction.performed += context => soData.spyInputs.selectNPC = true;

        jumpAction.performed += context => soData.spyInputs.jump = true;
        jumpAction.canceled += context => soData.spyInputs.jump = false;

        runAction.performed += context => soData.spyInputs.run = true;
        runAction.canceled += context => soData.spyInputs.run = false;

        cancelAction.started += context => soData.spyInputs.cancel = true;
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
        soData.spyInputs.mouseScreenPos = Mouse.current.position.ReadValue();
        soData.spyInputs.mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void LateUpdate()
    {
        soData.spyInputs.cancel = false;
        soData.spyInputs.interact = false;
        soData.spyInputs.selectNPC = false;
    }
    private void CheckDevice(InputControl value, InputEventPtr ptr)
    {
        curDevice = value.device;
        OnDeviceChanged?.Invoke(value.device.displayName);
    }

    private void ResetData()
    {
        soData.spyInputs.jump = false;
    }

    private void OnApplicationQuit()
    {
        soData.gameEventData.OnReset.Raise();
        ResetData();
    }
}
