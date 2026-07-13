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

    public StartNotepad notepad;

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
    }
    private void OnEnable()
    {
        FadeBlack.OnFinishFadeFromBlack += SetToNoneState;
        StartNotepad.OnStartGame += StartGame;
    }
    private void OnDisable()
    {
        FadeBlack.OnFinishFadeFromBlack -= SetToNoneState;
        StartNotepad.OnStartGame -= StartGame;
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
        fadeBlack.CheckToFadeFromBlack();
    }
    private void StartGame()
    {
        fadeBlack.FadeToBlack("April 27, 1992, Meridia", SceneType.Trip, sceneIndex: 2);
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
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NotepadInactiveLocalPos, Time.deltaTime * MOVE_DAMP);
                    notepad.activePage.InvertExitButton(invert: false);
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
        NotepadHoverPos = new Vector3(NotepadInactiveLocalPos.x, NotepadInactiveLocalPos.y + notepad.bindingRingsRend.bounds.size.y, NotepadInactiveLocalPos.z);

        float ySize = notepad.startPage.paperRenderer.bounds.size.y;
        Vector3 hoverSize = new Vector3(notepad.bindingRingsRend.bounds.size.x, ySize, 0.2f);
        notepad.transform.localPosition = NotepadInactiveLocalPos;

        Shader.SetGlobalVector("_CameraSizeAndPos", new Vector4(camStats.camBounds.size.x, camStats.camBounds.size.y, camStats.camBounds.center.x, camStats.camBounds.center.y));
    }
}
