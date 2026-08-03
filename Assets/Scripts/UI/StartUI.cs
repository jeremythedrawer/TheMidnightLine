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
    public SceneData sceneData;

    public Transform titleTransform;

    public TextUIElement startButton;
    public TextUIElement optionsButton;
    public TextUIElement quitButton;

    public TextUIElement darkColorButton;
    public TextUIElement lightColorButton;
    public TextUIElement tutorialButton;

    public TextUIElement thankYouMessage;
    public TextUIElement playAgainButton;

    public IconUIElement titleIcon1;
    public IconUIElement titleIcon2;


    public FadeBlack fadeBlack;

    [Header("Generated")]
    public Vector3 naturalMovePos;

    public UIState curState;

    public bool canExitState;
    public bool atOptions;

    public Notepad notepad;

    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;
    private void Start()
    {

    }
    private void OnEnable()
    {        
        NotepadProp.OnSpyEnter += SetInteractIcon;
        NotepadProp.OnSpyExit += DisableInteractIcon;

        SpyBrain.OnOpenNotepad += SetToNotepadState;
        SpyBrain.OnCloseNotepad += SetToNoneState;

        MeetingDoor.OnSpyEnter += SetInteractIcon;
        MeetingDoor.OnSpyExit += DisableInteractIcon;

        Scenes.OnLoadScore += ScoreSceneInit;
        Scenes.OnLoadStart += StartSceneInit;

        gameEventData.OnNotepadCollect.RegisterListener(DisableInteractIcon);
        gameEventData.OnNotepadCollect.RegisterListener(GetNotepad);

        gameEventData.OnStartTrip.RegisterListener(StartTrip);
        gameEventData.OnToStartMenu.RegisterListener(SetToStartMenuState);
        gameEventData.OnFromStartMenu.RegisterListener(SetToNoneState);
    }
    private void OnDisable()
    {
        NotepadProp.OnSpyEnter -= SetInteractIcon;
        NotepadProp.OnSpyExit -= DisableInteractIcon;

        SpyBrain.OnOpenNotepad -= SetToNotepadState;
        SpyBrain.OnCloseNotepad -= SetToNoneState;

        MeetingDoor.OnSpyEnter -= SetInteractIcon;
        MeetingDoor.OnSpyExit -= DisableInteractIcon;

        Scenes.OnLoadStart -= StartSceneInit;
        Scenes.OnLoadScore -= ScoreSceneInit;

        gameEventData.OnNotepadCollect.UnregisterListener(DisableInteractIcon);
        gameEventData.OnNotepadCollect.UnregisterListener(GetNotepad);

        gameEventData.OnStartTrip.UnregisterListener(StartTrip);
        gameEventData.OnToStartMenu.UnregisterListener(SetToStartMenuState);
        gameEventData.OnFromStartMenu.UnregisterListener(SetToNoneState);
    }
    private void Update()
    {
        UpdateState();
        fadeBlack.CheckToFadeOutSceneChange();
    }
    private void StartSceneInit()
    {
        startButton.InitButton(StartButtonClicked);
        optionsButton.InitButton(OptionsButtonClicked);
        quitButton.InitButton(QuitButtonClicked);
        
        darkColorButton.InitButton(DarkColorButtonClicked);
        lightColorButton.InitButton(LightColorButtonClicked);
        tutorialButton.InitButton(TutorialButtonClicked);

        titleIcon1.InitImage();
        titleIcon2.InitImage();
        
        darkColorButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        lightColorButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        tutorialButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        thankYouMessage.SetButtonAway(camStats, MoveButtonDirection.Left);
        playAgainButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        
        titleTransform.SetParent(null);

        Shader.SetGlobalFloat("_DayNight", 1);

        fadeBlack.SetAlpha(1);
        fadeBlack.FadeOut();

        options.skipTutorial = false;

        SetState(UIState.StartMenu);
    }
    private void ScoreSceneInit()
    {
        startButton.InitButton(StartButtonClicked);
        optionsButton.InitButton(OptionsButtonClicked);
        quitButton.InitButton(QuitButtonClicked);
        
        darkColorButton.InitButton(DarkColorButtonClicked);
        lightColorButton.InitButton(LightColorButtonClicked);
        tutorialButton.InitButton(TutorialButtonClicked);
        
        thankYouMessage.InitMessage();
        playAgainButton.InitButton(PlayAgainClicked);

        titleIcon1.InitImage();
        titleIcon2.InitImage();

        startButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        optionsButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        quitButton.SetButtonAway(camStats, MoveButtonDirection.Left);

        darkColorButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        lightColorButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        tutorialButton.SetButtonAway(camStats, MoveButtonDirection.Left);
        
        thankYouMessage.SetButtonAway(camStats, MoveButtonDirection.Left);
        playAgainButton.SetButtonAway(camStats, MoveButtonDirection.Left);

        titleIcon1.SetButtonAway(camStats, MoveButtonDirection.Left);
        titleIcon2.SetButtonAway(camStats, MoveButtonDirection.Left);

        titleTransform.SetParent(null);

        fadeBlack.SetAlpha(1);
        fadeBlack.FadeOut();

        Shader.SetGlobalFloat("_DayNight", 1);

        SetState(UIState.None);
    }
    private void StartTrip()
    {
        fadeBlack.FadeInChangeScene("FIND where the TRAITORS are going.", SceneType.Trip, sceneIndex: 2);
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
        notepad.Init();
    }
    private void SetToNoneState()
    {
        startButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
        optionsButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
        quitButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
        SetState(UIState.None);
    }
    private void SetToNotepadState()
    {
        DisableInteractIcon();
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
            case UIState.StartMenu:
            {
                startButton.MoveButtonToActive();
                optionsButton.MoveButtonToActive();
                quitButton.MoveButtonToActive();
            }
            break;

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
            }
            break;

            case UIState.None:
            {
                if (notepadData.collected)
                {
                    if (canExitState && CursorController.IsInsideBounds(notepad.activePage.paperRenderer.bounds, isClickable: true))
                    {
                        ctsNotepad?.Cancel();
                        notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.hoverLocalPos, Time.deltaTime * MOVE_DAMP);
                    }
                    else
                    {
                        notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.inactiveLocalPos, Time.deltaTime * MOVE_DAMP);
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
                UpdateOptionsButtons();
            }
            break;

            case UIState.EndMenu:
            {
                UpdateEndMenuButtons();
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

    private void UpdateMainMenuButtons()
    {
        startButton.UpdateButton(playerInputs);
        quitButton.UpdateButton(playerInputs);
        optionsButton.UpdateButton(playerInputs);
    }
    private void UpdateOptionsButtons()
    {
        optionsButton.UpdateButton(playerInputs);
        darkColorButton.UpdateButton(playerInputs);
        lightColorButton.UpdateButton(playerInputs);
        tutorialButton.UpdateButton(playerInputs);
    }
    private void UpdateEndMenuButtons()
    {
        playAgainButton.UpdateButton(playerInputs);
        quitButton.UpdateButton(playerInputs);
    }
    private void PlayAgainClicked()
    {
        //TODO: Reset stats, seemless transition to start scene
        playAgainButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
        thankYouMessage.MoveButtonAway(camStats, MoveButtonDirection.Left);
        
        startButton.MoveButtonToActive();
        optionsButton.MoveButtonToActive();

        gameEventData.OnPlayAgain.Raise();
        SetState(UIState.StartMenu);
    }
    private void StartButtonClicked()
    {
        startButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
        optionsButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
        quitButton.MoveButtonAway(camStats, MoveButtonDirection.Left);

        OnStartGame?.Invoke();
        SetState(UIState.None);
    }
    private void OptionsButtonClicked()
    {
        switch(curState)
        {
            case UIState.StartMenu:
            {
                optionsButton.renderer.SetText("Back");
                quitButton.MoveButtonAway(camStats, MoveButtonDirection.Right);
                startButton.MoveButtonAway(camStats, MoveButtonDirection.Right);

                optionsButton.MoveButtonToRight();

                darkColorButton.MoveButtonToActive();
                lightColorButton.MoveButtonToActive();
                tutorialButton.MoveButtonToActive();

                OnClickOptions?.Invoke();
                SetState(UIState.OptionsMenu);
            }
            break;
            case UIState.OptionsMenu:
            {
                optionsButton.renderer.SetText("Options");
                startButton.MoveButtonToActive();
                quitButton.MoveButtonToActive();
                optionsButton.MoveButtonToActive();

                darkColorButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
                lightColorButton.MoveButtonAway(camStats, MoveButtonDirection.Left);
                tutorialButton.MoveButtonAway(camStats, MoveButtonDirection.Left);

                OnClickBackFromOptions?.Invoke();
                if (sceneData.activeSceneType == SceneType.Start)
                {
                    SetState(UIState.StartMenu);
                }
                else
                {
                    SetState(UIState.EndMenu);
                }
            }
            break;
        }
    }
    private void QuitButtonClicked()
    {
        Application.Quit();
    }
    private void DarkColorButtonClicked()
    {
        SceneController.GetMainColorPicker().Open(darkColorButton.renderer.background_renderer, ColorPicker.SelectType.Dark, ColorPicker.Direction.BottomRight);
    }
    private void LightColorButtonClicked()
    {
        SceneController.GetMainColorPicker().Open(lightColorButton.renderer.background_renderer, ColorPicker.SelectType.Light, ColorPicker.Direction.BottomRight);
    }
    private void TutorialButtonClicked()
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
}
