using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Scenes;
using static Spy;

public class StartUI : MonoBehaviour
{
    public Material fadeBlackMaterial;

    public TripSO trip;
    public GameEventDataSO gameEventData;
    public CameraStatsSO camStats;
    public PlayerInputsSO playerInputs;
    public NotepadData notepadData;

    public SceneData sceneData;

    public Notepad notepad;

    public FadeBlack fadeBlack;

    [Header("Generated")]
    public Vector3 naturalMovePos;

    public UIState curState;

    public bool canExitState;

    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;


    private void Start()
    {
        InitPOVUI();
        Shader.SetGlobalFloat("_DayNight", 1);
        fadeBlack.SetValue(1);
        fadeBlack.FadeFromBlack();
    }
    private void OnEnable()
    {
        FadeBlack.OnFinishFadeFromBlack += SetToNoneState;
        gameEventData.OnStartGame.RegisterListener(StartGame);
    }
    private void OnDisable()
    {
        FadeBlack.OnFinishFadeFromBlack -= SetToNoneState;
        gameEventData.OnStartGame.UnregisterListener(StartGame);
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
        fadeBlack.CheckToFadeFromBlack();
    }
    private void StartGame()
    {
        fadeBlack.FadeToBlack("Find where the Traitors are going.", SceneType.Trip, sceneIndex: 2);
    }
    private void SetToNoneState()
    {
        SetState(UIState.None);
    }
    private void ChooseState()
    {
        if (playerInputs.notepadKeyDown || notepadData.checkingNotepad)
        {
            SetState(UIState.Notepad);
        }
        else
        {
            SetState(UIState.None);
        }
    }
    private void SetState(UIState newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    public void EnterState()
    {
        canExitState = false;
        switch (curState)
        {
            case UIState.Notepad:
            {
                notepad.EnterNotepad();
                naturalMovePos = NotepadActiveLocalPos;
                ctsNotepad?.Cancel();
            }
            break;
            case UIState.None:
            {

            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                UpdateNaturalPos(NotepadActiveLocalPos, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);

                if (playerInputs.notepadKeyDown && canExitState)
                {
                    notepadData.checkingNotepad = false;
                }
                canExitState = true;
            }
            break;

            case UIState.None:
            {
                if (canExitState && CursorController.IsInsideBounds(notepad.activePage.paperRenderer.bounds, isClickable: true))
                {
                    ctsNotepad?.Cancel();
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NotepadHoverPos, Time.deltaTime * MOVE_DAMP);

                    notepad.activePage.InvertExitButton(invert: true);
                    if (playerInputs.mouseLeftUp)
                    {
                        notepad.activePage.InvertExitButton(invert: false);
                        notepadData.checkingNotepad = true;
                    }
                }
                else
                {
                    if (notepadData.collected)
                    {
                        notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NotepadInactiveLocalPos, Time.deltaTime * MOVE_DAMP);
                        notepad.activePage.InvertExitButton(invert: false);
                    }
                }
                canExitState = true;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                MoveUIElement(notepad.transform, NotepadInactiveLocalPos, ref ctsNotepad, curState);
                notepad.ExitNotepad();
            }
            break;
        }
    }
    private void InitPOVUI()
    {
        float halfCamWidth = camStats.camBounds.extents.x;
        float halfCamHeight = camStats.camBounds.extents.y;

        NotepadActiveLocalPos = notepad.transform.localPosition;
        float binderBoundsOffsetX = notepad.bindingRingsRend.bounds.max.x - notepad.transform.position.x;

        NotepadInactiveLocalPos = new Vector3(halfCamWidth - binderBoundsOffsetX, -halfCamHeight + NOTEPAD_INACTIVE_OFFSET, notepad.transform.localPosition.z);

        float bindingRingsHeight = notepad.bindingRingsRend.bounds.size.y;

        NotepadHoverPos = new Vector3(NotepadInactiveLocalPos.x, NotepadInactiveLocalPos.y + bindingRingsHeight, NotepadInactiveLocalPos.z);

        Vector3 startPos = new Vector3();
        startPos.x = NotepadInactiveLocalPos.x;
        startPos.y = -halfCamHeight - bindingRingsHeight;
        startPos.z = NotepadInactiveLocalPos.z;

        notepad.transform.localPosition = startPos;

        notepadData.collected = false;

        Shader.SetGlobalVector("_CameraSizeAndPos", new Vector4(camStats.camBounds.size.x, camStats.camBounds.size.y, camStats.camBounds.center.x, camStats.camBounds.center.y));
    }
}
