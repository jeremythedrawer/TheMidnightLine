using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Scenes;
using static Spy;

public class StartUI : MonoBehaviour
{
    public static event Action OnClickOptions;
    public static event Action OnClickBackFromOptions;
    public static event Action OnStartGame;

    public AtlasRenderer keyIconRenderer;

    public Material fadeBlackMaterial;

    public TripSO trip;
    public GameEventDataSO gameEventData;
    public CameraStatsSO camStats;
    public PlayerInputsSO playerInputs;
    public NotepadData notepadData;

    public AtlasTextRenderer startRenderer;
    public AtlasTextRenderer optionsRenderer;
    public AtlasTextRenderer quitRenderer;
    public AtlasTextRenderer darkColorRenderer;
    public AtlasTextRenderer lightColorRenderer;


    public SceneData sceneData;

    public Notepad notepad;

    public FadeBlack fadeBlack;

    [Header("Generated")]
    public Vector3 naturalMovePos;

    public UIState curState;

    public bool canExitState;
    public bool atOptions;

    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;


    private void Start()
    {
        InitPOVUI();
        Shader.SetGlobalFloat("_DayNight", 1);
        fadeBlack.SetValue(1);
        fadeBlack.FadeFromBlack();

        SetState(UIState.StartMenu);
    }
    private void OnEnable()
    {        
        NotepadProp.OnSpyEnter += SetNotepadCollectIcon;
        NotepadProp.OnSpyExit += DisableNotepadCollectIcon;

        gameEventData.OnNotepadCollect.RegisterListener(DisableNotepadCollectIcon);

        gameEventData.OnStartTrip.RegisterListener(StartTrip);
        gameEventData.OnToStartMenu.RegisterListener(SetToStartMenuState);
        gameEventData.OnFromStartMenu.RegisterListener(SetStateToNone);
    }
    private void OnDisable()
    {
        NotepadProp.OnSpyEnter -= SetNotepadCollectIcon;
        NotepadProp.OnSpyExit -= DisableNotepadCollectIcon;
        gameEventData.OnNotepadCollect.UnregisterListener(DisableNotepadCollectIcon);
        gameEventData.OnStartTrip.UnregisterListener(StartTrip);
        gameEventData.OnToStartMenu.UnregisterListener(SetToStartMenuState);
        gameEventData.OnFromStartMenu.UnregisterListener(SetStateToNone);
    }
    private void Update()
    {
        UpdateState();
        fadeBlack.CheckToFadeFromBlack();
    }
    private void StartTrip()
    {
        fadeBlack.FadeToBlack("Find where the Traitors are going.", SceneType.Trip, sceneIndex: 2);
    }
    private void SetNotepadCollectIcon(Vector2 position)
    {
        keyIconRenderer.enabled = true;
        keyIconRenderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.W);
        keyIconRenderer.transform.SetParent(null);
        keyIconRenderer.transform.position = new Vector3(position.x, position.y + keyIconRenderer.bounds.size.y, keyIconRenderer.transform.position.z);
    }
    private void DisableNotepadCollectIcon()
    {
        keyIconRenderer.enabled = false;
    }
    private void ChooseState()
    {
        if ((notepadData.collected && playerInputs.notepadKeyDown) || notepadData.checkingNotepad) // TODO: Set up as action
        {
            SetState(UIState.Notepad);
        }
        else
        {
            SetState(UIState.None);
        }
    }
    private void SetToStartMenuState()
    {
        SetState(UIState.StartMenu);
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
            }
            break;

            case UIState.StartMenu:
            {
                HandleMainMenuButtons();
            }
            break;

            case UIState.OptionsMenu:
            {
                HandleOptionsButtons();

            }
            break;
        }
        canExitState = true;
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
    private void HandleMainMenuButtons()
    {
        if (CursorController.IsInsideBounds(quitRenderer.background_renderer.bounds, isClickable: true))
        {

            quitRenderer.SetColorText(Color.white);
            quitRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                Application.Quit();
            }
        }
        else
        {
            quitRenderer.SetColorText(Color.black);
            quitRenderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(optionsRenderer.background_renderer.bounds, isClickable: true))
        {
            optionsRenderer.SetColorText(Color.white);
            optionsRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                optionsRenderer.SetText("Back");
                OnClickOptions?.Invoke();
                SetState(UIState.OptionsMenu);
            }
        }
        else
        {
            optionsRenderer.SetColorText(Color.black);
            optionsRenderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(startRenderer.background_renderer.bounds, isClickable: true))
        {

            startRenderer.SetColorText(Color.white);
            startRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                OnStartGame?.Invoke();
                SetState(UIState.None);

                startRenderer.SetColorText(Color.black);
                startRenderer.background_renderer.SetSliceCustom(w: 0);
            }
        }
        else
        {
            startRenderer.SetColorText(Color.black);
            startRenderer.background_renderer.SetSliceCustom(w: 0);
        }
    }

    private void HandleOptionsButtons()
    {
        if (CursorController.IsInsideBounds(darkColorRenderer.background_renderer.bounds, isClickable: true))
        {
            darkColorRenderer.SetColorText(Color.white);
            darkColorRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                SceneController.GetMainColorPicker().Open(darkColorRenderer.background_renderer, ColorPicker.SelectType.Dark, ColorPicker.Direction.BottomRight);
            }
        }
        else
        {
            darkColorRenderer.SetColorText(Color.black);
            darkColorRenderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(lightColorRenderer.background_renderer.bounds, isClickable: true))
        {
            lightColorRenderer.SetColorText(Color.white);
            lightColorRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                SceneController.GetMainColorPicker().Open(lightColorRenderer.background_renderer, ColorPicker.SelectType.Light, ColorPicker.Direction.BottomRight);
            }
        }
        else
        {
            lightColorRenderer.SetColorText(Color.black);
            lightColorRenderer.background_renderer.SetSliceCustom(w: 0);
        }


        if (CursorController.IsInsideBounds(optionsRenderer.background_renderer.bounds, isClickable: true))
        {
            optionsRenderer.SetColorText(Color.white);
            optionsRenderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                optionsRenderer.SetText("Options");
                OnClickBackFromOptions?.Invoke();
                SetState(UIState.StartMenu);
            }
        }
        else
        {
            optionsRenderer.SetColorText(Color.black);
            optionsRenderer.background_renderer.SetSliceCustom(w: 0);
        }
    }

    private void SetStateToNone()
    {
        SetState(UIState.None);
    }

}
