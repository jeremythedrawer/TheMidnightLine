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
        gameEventData.OnNotepadCollect.RegisterListener(GetNotepad);

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
        gameEventData.OnNotepadCollect.UnregisterListener(GetNotepad);

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
        SceneController.KeepNotepad(notepad);
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
    private void GetNotepad()
    {
        notepad = SceneController.GetNotepad(transform);
        notepad.transform.localPosition = notepadData.inactiveLocalPos;
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
                naturalMovePos = notepadData.activeLocalPos;
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
                UpdateNaturalPos(notepadData.activeLocalPos, ref naturalMovePos);
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
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.hoverLocalPos, Time.deltaTime * MOVE_DAMP);

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
                        notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.inactiveLocalPos, Time.deltaTime * MOVE_DAMP);
                        notepad.activePage.InvertExitButton(invert: false);
                    }
                }
            }
            break;

            case UIState.StartMenu:
            {
                UpdateMainMenuButtons();
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
                MoveUIElement(notepad.transform, notepadData.inactiveLocalPos, ref ctsNotepad, curState);
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

        startButton.Init(StartButtonClicked, UIButtonEnter, UIButtonExit);
        optionsButton.Init(OptionsButtonClicked, UIButtonEnter, UIButtonExit);
        quitButton.Init(QuitButtonClicked, UIButtonEnter, UIButtonExit);
        darkColorButton.Init(DarkColorButtonClicked, UIButtonEnter, UIButtonExit);
        lightColorButton.Init(LightColorButtonClicked, UIButtonEnter, UIButtonExit);
        tutorialButton.Init(TutorialButtonClicked, UIButtonEnter, UIButtonExit);    
    }
    private void InitButton(ref TextUIElement button)
    {
        button.renderer.transform.SetParent(null);
        button.startPos = button.renderer.transform.position;
    }
    private void UpdateMainMenuButtons()
    {
        startButton.UpdateButton(playerInputs);
        quitButton.UpdateButton(playerInputs);
        optionsButton.UpdateButton(playerInputs);
    }
    private void HandleOptionsButtons()
    {
        optionsButton.UpdateButton(playerInputs);
        darkColorButton.UpdateButton(playerInputs);
        lightColorButton.UpdateButton(playerInputs);
        tutorialButton.UpdateButton(playerInputs);
    }
    
    private void UIButtonEnter(TextUIElement textUIElement)
    {
        textUIElement.renderer.SetColorText(Color.white);
        textUIElement.renderer.background_renderer.SetSliceCustom(w: 1);
    }
    private void UIButtonExit(TextUIElement textUIElement)
    {
        textUIElement.renderer.SetColorText(Color.black);
        textUIElement.renderer.background_renderer.SetSliceCustom(w: 0);
    }
    private void StartButtonClicked(TextUIElement textUIElement)
    {
        OnStartGame?.Invoke();
        SetState(UIState.None);
    }
    private void OptionsButtonClicked(TextUIElement textUIElement)
    {
        if (curState == UIState.StartMenu)
        {
            textUIElement.renderer.SetText("Back");
            MoveButtonAway(quitButton);
            MoveButtonAway(startButton);
            MoveButtonToRight(textUIElement);
            OnClickOptions?.Invoke();
            SetState(UIState.OptionsMenu);
        }
        else if (curState == UIState.OptionsMenu)
        {
            optionsButton.renderer.SetText("Options");
            MoveButtonBack(startButton);
            MoveButtonBack(quitButton);
            MoveButtonBack(optionsButton);
            OnClickBackFromOptions?.Invoke();
            SetState(UIState.StartMenu);
        }
    }
    private void QuitButtonClicked(TextUIElement textUIElement)
    {
        Application.Quit();
    }
    private void DarkColorButtonClicked(TextUIElement textUIElement)
    {
        SceneController.GetMainColorPicker().Open(darkColorButton.renderer.background_renderer, ColorPicker.SelectType.Dark, ColorPicker.Direction.BottomRight);
    }
    private void LightColorButtonClicked(TextUIElement textUIElement)
    {
        SceneController.GetMainColorPicker().Open(lightColorButton.renderer.background_renderer, ColorPicker.SelectType.Light, ColorPicker.Direction.BottomRight);
    }
    private void TutorialButtonClicked(TextUIElement textUIElement)
    {
        if (options.skipTutorial)
        {
            tutorialButton.renderer.SetText("Run Tutorial");
            options.skipTutorial = false;
        }
        else
        {
            tutorialButton.renderer.SetText("Skip Tutorial");
            options.skipTutorial = true;
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
