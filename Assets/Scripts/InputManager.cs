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
    public InputAction moveAction { get; private set; }
    public InputAction jumpAction { get; private set; }
    public InputAction runAction { get; private set; }
    public InputAction clipboardAction { get; private set; }
    public InputAction interactAction { get; private set; }
    public InputAction cancelAction { get; private set; }
    public Action<string> OnDeviceChanged { get; private set; }

    public static InputDevice curDevice;


    private float resetElaspedTime;
    private float resetThresholdTime = 2;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Player/Movement"];
        jumpAction = playerInput.actions["Player/Jump"];
        runAction = playerInput.actions["Player/Run"];
        clipboardAction = playerInput.actions["Player/Clipboard"];
        interactAction = playerInput.actions["Player/Interact"];
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
        jumpAction.performed += context => soData.spyInputs.jump = true;
        jumpAction.canceled += context => soData.spyInputs.jump = false;


        runAction.performed += context => soData.spyInputs.run = true;
        runAction.canceled += context => soData.spyInputs.run = false;

        clipboardAction.started += context => soData.spyInputs.clipboard = true;

        interactAction.started += context =>
        {
            soData.gameEventData.OnInteract.Raise();
            soData.spyInputs.interact = true;
        };

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
    }

    private void LateUpdate()
    {
        soData.spyInputs.clipboard = false;
        soData.spyInputs.cancel = false;
        soData.spyInputs.interact = false;
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
