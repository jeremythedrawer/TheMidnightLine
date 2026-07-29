using Cysharp.Threading.Tasks;
using System;
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
    public OptionsSO options;

    public Transform titleTransform;

    public TextUIElement startButton;
    public TextUIElement optionsButton;
    public TextUIElement quitButton;

    public TextUIElement darkColorButton;
    public TextUIElement lightColorButton;
    public TextUIElement tutorialButton;

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
        InitButtons();

        titleTransform.SetParent(null);

        Shader.SetGlobalFloat("_DayNight", 1);
        fadeBlack.SetValue(1);
        fadeBlack.FadeFromBlack();
        

        SetState(UIState.StartMenu);
    }
    private void OnEnable()
    {        
        NotepadProp.OnSpyEnter += SetInteractIcon;
        NotepadProp.OnSpyExit += DisableInteractIcon;

        SpyBrain.OnOpenNotepad += SetToNotepadState;
        SpyBrain.OnCloseNotepad += SetSToNoneState;

        MeetingDoor.OnSpyEnter += SetInteractIcon;
        MeetingDoor.OnSpyExit += DisableInteractIcon;



        gameEventData.OnNotepadCollect.RegisterListener(DisableInteractIcon);

        gameEventData.OnStartTrip.RegisterListener(StartTrip);
        gameEventData.OnToStartMenu.RegisterListener(SetToStartMenuState);
        gameEventData.OnFromStartMenu.RegisterListener(SetSToNoneState);
    }
    private void OnDisable()
    {
        NotepadProp.OnSpyEnter -= SetInteractIcon;
        NotepadProp.OnSpyExit -= DisableInteractIcon;

        SpyBrain.OnOpenNotepad -= SetToNotepadState;
        SpyBrain.OnCloseNotepad -= SetSToNoneState;

        MeetingDoor.OnSpyEnter -= SetInteractIcon;
        MeetingDoor.OnSpyExit -= DisableInteractIcon;

        gameEventData.OnNotepadCollect.UnregisterListener(DisableInteractIcon);
        gameEventData.OnStartTrip.UnregisterListener(StartTrip);
        gameEventData.OnToStartMenu.UnregisterListener(SetToStartMenuState);
        gameEventData.OnFromStartMenu.UnregisterListener(SetSToNoneState);
    }
    private void Update()
    {
        UpdateState();
        fadeBlack.CheckToFadeFromBlack();
    }
    private void StartTrip()
    {
        fadeBlack.FadeToBlackChangeScene("Find where the Traitors are going.", SceneType.Trip, sceneIndex: 2);
        keyIconRenderer.enabled = false;
    }
    private void SetInteractIcon(Vector2 position)
    {
        keyIconRenderer.enabled = true;
        keyIconRenderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.W);
        keyIconRenderer.transform.SetParent(null);
        keyIconRenderer.transform.position = new Vector3(position.x, position.y + keyIconRenderer.bounds.size.y, keyIconRenderer.transform.position.z);
    }
    private void DisableInteractIcon()
    {
        keyIconRenderer.enabled = false;
    }
    private void SetSToNoneState()
    {
        SetState(UIState.None);
    }
    private void SetToNotepadState()
    {
        SetState(UIState.Notepad);
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
    private void InitButtons()
    {
        InitButton(ref startButton);
        InitButton(ref optionsButton);
        InitButton(ref quitButton);
        InitButton(ref darkColorButton);
        InitButton(ref lightColorButton);
        InitButton(ref tutorialButton);

        options.skipTutorial = false;
    }
    private void InitButton(ref TextUIElement button)
    {
        button.renderer.transform.SetParent(null);
        button.startPos = button.renderer.transform.position;
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
        if (CursorController.IsInsideBounds(quitButton.renderer.background_renderer.bounds, isClickable: true))
        {
            quitButton.renderer.SetColorText(Color.white);
            quitButton.renderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                Application.Quit();
            }
        }
        else
        {
            quitButton.renderer.SetColorText(Color.black);
            quitButton.renderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(optionsButton.renderer.background_renderer.bounds, isClickable: true))
        {
            optionsButton.renderer.SetColorText(Color.white);
            optionsButton.renderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                optionsButton.renderer.SetText("Back");
                MoveButtonAway(quitButton);
                MoveButtonAway(startButton);
                MoveButtonToRight(optionsButton);
                OnClickOptions?.Invoke();
                SetState(UIState.OptionsMenu);
            }
        }
        else
        {
            optionsButton.renderer.SetColorText(Color.black);
            optionsButton.renderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(startButton.renderer.background_renderer.bounds, isClickable: true))
        {

            startButton.renderer.SetColorText(Color.white);
            startButton.renderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                OnStartGame?.Invoke();
                SetState(UIState.None);

                startButton.renderer.SetColorText(Color.black);
                startButton.renderer.background_renderer.SetSliceCustom(w: 0);
            }
        }
        else
        {
            startButton.renderer.SetColorText(Color.black);
            startButton.renderer.background_renderer.SetSliceCustom(w: 0);
        }
    }
    private void HandleOptionsButtons()
    {
        if (CursorController.IsInsideBounds(darkColorButton.renderer.background_renderer.bounds, isClickable: true))
        {
            darkColorButton.renderer.SetColorText(Color.white);
            darkColorButton.renderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                SceneController.GetMainColorPicker().Open(darkColorButton.renderer.background_renderer, ColorPicker.SelectType.Dark, ColorPicker.Direction.BottomRight);
            }
        }
        else
        {
            darkColorButton.renderer.SetColorText(Color.black);
            darkColorButton.renderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(lightColorButton.renderer.background_renderer.bounds, isClickable: true))
        {
            lightColorButton.renderer.SetColorText(Color.white);
            lightColorButton.renderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                SceneController.GetMainColorPicker().Open(lightColorButton.renderer.background_renderer, ColorPicker.SelectType.Light, ColorPicker.Direction.BottomRight);
            }
        }
        else
        {
            lightColorButton.renderer.SetColorText(Color.black);
            lightColorButton.renderer.background_renderer.SetSliceCustom(w: 0);
        }


        if (CursorController.IsInsideBounds(optionsButton.renderer.background_renderer.bounds, isClickable: true))
        {
            optionsButton.renderer.SetColorText(Color.white);
            optionsButton.renderer.background_renderer.SetSliceCustom(w: 1);
            if (playerInputs.mouseLeftDown)
            {
                optionsButton.renderer.SetText("Options");
                MoveButtonBack(startButton);
                MoveButtonBack(quitButton);
                MoveButtonBack(optionsButton);
                OnClickBackFromOptions?.Invoke();
                SetState(UIState.StartMenu);
            }
        }
        else
        {
            optionsButton.renderer.SetColorText(Color.black);
            optionsButton.renderer.background_renderer.SetSliceCustom(w: 0);
        }

        if (CursorController.IsInsideBounds(tutorialButton.renderer.background_renderer.bounds, isClickable: true))
        {
            if (options.skipTutorial)
            {

                if (playerInputs.mouseLeftDown)
                {
                    tutorialButton.renderer.SetColorText(Color.white);
                    tutorialButton.renderer.background_renderer.SetSliceCustom(w: 1);
                    tutorialButton.renderer.SetText("Run Tutorial");
                    options.skipTutorial = false;
                }
                else
                {

                    tutorialButton.renderer.SetColorText(Color.black);
                    tutorialButton.renderer.background_renderer.SetSliceCustom(w: 0);
                }
            }
            else
            {

                if (playerInputs.mouseLeftDown)
                {
                    tutorialButton.renderer.SetColorText(Color.black);
                    tutorialButton.renderer.background_renderer.SetSliceCustom(w: 0);
                    tutorialButton.renderer.SetText("Skip Tutorial");
                    options.skipTutorial = true;
                }
                else
                {
                    tutorialButton.renderer.SetColorText(Color.white);
                    tutorialButton.renderer.background_renderer.SetSliceCustom(w: 1);
                }
            }
        }
        else
        {
            if (options.skipTutorial)
            {
                tutorialButton.renderer.SetColorText(Color.white);
                tutorialButton.renderer.background_renderer.SetSliceCustom(w: 1);
            }
            else
            {
                tutorialButton.renderer.SetColorText(Color.black);
                tutorialButton.renderer.background_renderer.SetSliceCustom(w: 0);
            }

        }
    }

    private void MoveButtonAway(TextUIElement button)
    {
        button.ctsMove?.Cancel();
        button.ctsMove = new CancellationTokenSource();
        MovingButtonAwayRight(button).Forget();
    }
    private void MoveButtonToRight(TextUIElement button)
    {
        button.ctsMove?.Cancel();
        button.ctsMove = new CancellationTokenSource();
        MovingButtonToRight(button).Forget();
    }
    private void MoveButtonBack(TextUIElement button)
    {
        button.ctsMove?.Cancel();
        button.ctsMove = new CancellationTokenSource();
        MoveButtonBackFromRight(button).Forget();
    }
    private async UniTask MovingButtonAwayRight(TextUIElement button)
    {
        AtlasTextRenderer buttonRend = button.renderer;
        Transform buttonTransform = buttonRend.transform;
        Bounds buttonBounds = buttonRend.background_renderer.GetBounds();

        Vector3 buttonPos = buttonRend.transform.position;
        try
        {
            while(camStats.camBounds.max.x > buttonRend.background_renderer.GetBounds().min.x)
            {
                buttonPos.x = Mathf.Lerp(buttonPos.x, camStats.camBounds.max.x + buttonBounds.size.x, Time.deltaTime * 2);

                buttonTransform.position = buttonPos;

                await UniTask.Yield(button.ctsMove.Token);
            }
        }
        catch(OperationCanceledException)
        {

        }
    }
    private async UniTask MovingButtonToRight(TextUIElement button)
    {
        AtlasTextRenderer buttonRend = button.renderer;
        
        Transform buttonTransform = buttonRend.transform;
        buttonTransform.SetParent(transform);
        Bounds buttonBounds = buttonRend.background_renderer.GetBounds();

        Vector3 buttonPos = buttonRend.transform.localPosition;
        float targetPosX = -buttonPos.x;
        try
        {
            while (buttonPos.x < targetPosX * 0.995f)
            {
                buttonPos.x = Mathf.Lerp(buttonPos.x, targetPosX, Time.deltaTime * 2);
                buttonTransform.localPosition = buttonPos;

                await UniTask.Yield(button.ctsMove.Token);
            }
            buttonTransform.SetParent(null);

        }
        catch (OperationCanceledException)
        {
            buttonTransform.SetParent(null);
        }
    }
    private async UniTask MoveButtonBackFromRight(TextUIElement button)
    {
        AtlasTextRenderer buttonRend = button.renderer;
        Transform buttonTransform = buttonRend.transform;
        Bounds buttonBounds = buttonRend.background_renderer.GetBounds();

        Vector3 buttonPos = buttonRend.transform.position;
        try
        {
            while (buttonPos.x > button.startPos.x)
            {
                buttonPos.x = Mathf.Lerp(buttonPos.x, button.startPos.x - buttonBounds.extents.x, Time.deltaTime * 2);
                buttonTransform.position = buttonPos;

                await UniTask.Yield(button.ctsMove.Token);
            }
            buttonTransform.position = button.startPos;
        }
        catch(OperationCanceledException)
        {

        }
    }
}
