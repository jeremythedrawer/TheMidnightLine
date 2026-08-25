using Cysharp.Threading.Tasks;
using System;
using System.Threading;

using UnityEngine;

using static AtlasUI;
using static NPC;
using static Scenes;

public class StartUI : MonoBehaviour
{
    const float OVERSHOOT_MULTIPLIER = 0.1f;
    const float OUTCOME_PAGE_X_OFFSET = 0.1f;
    const float MUGSHOT_REVEAL_TIME = 2f;

    const float OUTCOME_PAGE_MOVE_X = 0.5f;
    const float OUTCOME_PAGE_MOVE_Y = 0.5f;

    public static event Action OnPlayAgain;
    public static event Action OnFinishedOutcomeSequence;
    public static event Action OnClickOptions;
    public static event Action OnClickBackFromOptions;
    public static event Action OnStartButtonClicked;

    public AtlasRenderer[] keyIconRenderers;

    public Material fadeBlackMaterial;

    public ColorPicker darkColorPicker;
    public ColorPicker lightColorPicker;

    public TripData trip;
    public GameEventData gameEventData;
    public CameraData camData;
    public InputData inputData;
    public NotepadData notepadData;
    public Options options;
    public SceneData sceneData;
    public SpyData spyStats;
    public NPCsData npcsData;
    public MeridiaTowerData meridiaTowerData;
    public CursorData cursorData;

    public TextButton startButton;
    public TextButton optionsButton;
    public TextButton quitButton;

    public TextButton darkColorButton;
    public TextButton lightColorButton;

    public FadeBlack fadeBlack;

    [Header("Generated")]

    public string curLocationText;
    public Vector3[] outcomePageInactivePositions;

    public TraitorProfile curTraitorProfile;

    public Vector3 naturalMovePos;
    public Vector3 outcomePageActivePos;
    public Vector3 outcomePageStartPos;

    public float outcomePageEndPosX;
    public float outcomePageHoverPosY;

    public int curTraitorsShown;
    public int curTraitorProfilesReviewed;
    public int outcomePageCompletedMask;

    public UIState curState;

    public bool canExitState;
    public bool atOptions;
    public bool outcomeSetUpCompleted;

    public Page[] profilePages;

    public Notepad notepad;
    
    public Page hoveredPage;
    public Page activePage;
    
    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsOutcomePageMove;


    [Header("Editor")]
    public bool skipOutcomeSequence;
    private void OnEnable()
    {        
        AgreementProp.OnSpyEnter += ShowSpaceIcon;
        AgreementProp.OnSpyExit += HideKeyIcons;
        AgreementProp.OnAgreementCollect += HideKeyIcons;
        AgreementProp.OnAgreementCollect += SetFadeForAgreement;
        AgreementProp.OnNotepadReturn += HideKeyIcons;
        AgreementProp.OnNotepadReturn += SetToOutcomeState;

        SpyBrain.OnOpenNotepad += SetToNotepadState;
        SpyBrain.OnCloseNotepad += SetToNoneState;
        SpyBrain.OnMoveFirstTime += HideKeyIcons;

        MeridiaTower.OnSpyEnterTripDoor += ShowSpaceIcon;
        MeridiaTower.OnSpyExitTripDoor += HideKeyIcons;
        MeridiaTower.OnScrollingUp += SetToStartMenuState;

        Scenes.OnLoadScore += ScoreSceneInit;
        Scenes.OnLoadStart += StartSceneInit;

        FadeBlack.OnFinishFadeOut += SetToNoneStateFromOutcome;

        CameraController.OnArrivedAtElevator += SetToEndMenuState;
        gameEventData.OnStartTrip.RegisterListener(StartTrip);
    }
    private void OnDisable()
    {
        AgreementProp.OnSpyEnter -= ShowSpaceIcon;
        AgreementProp.OnSpyExit -= HideKeyIcons;
        AgreementProp.OnAgreementCollect -= SetFadeForAgreement;
        AgreementProp.OnAgreementCollect -= HideKeyIcons;
        AgreementProp.OnNotepadReturn -= HideKeyIcons;
        AgreementProp.OnNotepadReturn -= SetToOutcomeState;

        SpyBrain.OnOpenNotepad -= SetToNotepadState;
        SpyBrain.OnCloseNotepad -= SetToNoneState;
        SpyBrain.OnMoveFirstTime -= HideKeyIcons;

        MeridiaTower.OnSpyEnterTripDoor -= ShowSpaceIcon;
        MeridiaTower.OnSpyExitTripDoor -= HideKeyIcons;
        MeridiaTower.OnScrollingUp -= SetToStartMenuState;

        Scenes.OnLoadStart -= StartSceneInit;
        Scenes.OnLoadScore -= ScoreSceneInit;


        FadeBlack.OnFinishFadeOut -= SetToNoneStateFromOutcome;

        CameraController.OnArrivedAtElevator -= SetToEndMenuState;


        gameEventData.OnStartTrip.UnregisterListener(StartTrip);
    }
    private void Update()
    {
        UpdateState();
        fadeBlack.CheckToFadeOutSceneChange();
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
                startButton.MoveToActive();
                optionsButton.MoveToActive();
                quitButton.MoveToActive();
            }
            break;
            case UIState.OptionsMenu:
            {
                optionsButton.textRenderer.SetText("Back");
                quitButton.MoveAway(Direction.Right);
                startButton.MoveAway(Direction.Right);

                optionsButton.MoveToRight();

                darkColorButton.MoveToActive();
                lightColorButton.MoveToActive();

                spyStats.playerInputsEnabled = false;

                OnClickOptions?.Invoke();
            }
            break;
            case UIState.EndMenu:
            {
                quitButton.MoveToActive();
            }
            break;

            case UIState.Notepad:
            {
                notepad.EnterNotepad();
                naturalMovePos = Notepad.ACTIVE_POS;
                ctsNotepad?.Cancel();
            }
            break;

            case UIState.Outcome:
            {

            }
            break;
            case UIState.None:
            {
                startButton.MoveAway(Direction.Left);
                optionsButton.MoveAway(Direction.Left);
                quitButton.MoveAway(Direction.Left);
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
                UpdateNaturalPos(Notepad.ACTIVE_POS, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
                if ((notepad.transform.localPosition - naturalMovePos).sqrMagnitude < 0.05f) notepadData.subState |= Notepad.SubState.InUse;
            }
            break;

            case UIState.None:
            {
                if (notepadData.collected)
                {
                    if (notepad.transform.parent != transform) return;
                    if (canExitState && cursorData.IsInsideBounds(notepad.activePage.paperRenderer.bounds, isClickable: true))
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

            case UIState.Outcome:
            {
            }
            break;
        }
        canExitState = true;
    }
    private void ExitState()
    {
        switch (curState)
        {
            case UIState.StartMenu:
            {
                spyStats.playerInputsEnabled = true;
            }
            break;
            case UIState.OptionsMenu:
            {
                optionsButton.textRenderer.SetText("Options");
                startButton.MoveToActive();
                quitButton.MoveToActive();
                optionsButton.MoveToActive();

                darkColorButton.MoveAway(Direction.Left);
                lightColorButton.MoveAway(Direction.Left);

                spyStats.playerInputsEnabled = true;
                OnClickBackFromOptions?.Invoke();
            }
            break;
            case UIState.Notepad:
            {
                MoveUIElement(notepad.transform, notepadData.inactiveLocalPos, ref ctsNotepad, curState);
                notepad.ExitNotepad();
            }
            break;
            case UIState.Outcome:
            {

            }
            break;
        }
    }
    private void StartSceneInit()
    {
        startButton.InitButton(StartButtonClicked, EnterTextButton, ExitTextButton);
        optionsButton.InitButton(OptionsButtonClicked, EnterTextButton, ExitTextButton);
        quitButton.InitButton(QuitButtonClicked, EnterTextButton, ExitTextButton);
        
        darkColorButton.InitButton(DarkColorButtonClicked, EnterTextButton, ExitTextButton);
        lightColorButton.InitButton(LightColorButtonClicked, EnterTextButton, ExitTextButton);

        darkColorButton.SetAway(Direction.Left);
        lightColorButton.SetAway(Direction.Left);

        Shader.SetGlobalFloat("_DayNight", 1);

        fadeBlack.SetAlpha(1);
        fadeBlack.FadeOut();

        SetState(UIState.StartMenu);
    }
    private void ScoreSceneInit()
    {
        startButton.InitButton(StartButtonClicked, EnterTextButton, ExitTextButton);
        optionsButton.InitButton(OptionsButtonClicked, EnterTextButton, ExitTextButton);
        quitButton.InitButton(QuitButtonClicked, EnterTextButton, ExitTextButton);
        
        darkColorButton.InitButton(DarkColorButtonClicked, EnterTextButton, ExitTextButton);
        lightColorButton.InitButton(LightColorButtonClicked, EnterTextButton, ExitTextButton);

        startButton.SetAway(Direction.Left);
        optionsButton.SetAway(Direction.Left);
        quitButton.SetAway(Direction.Left);

        darkColorButton.SetAway(Direction.Left);
        lightColorButton.SetAway(Direction.Left);

        fadeBlack.SetAlpha(1);
        fadeBlack.FadeOut();

        Shader.SetGlobalFloat("_DayNight", 1);

        SetState(UIState.None);

        notepad = SceneController.GetAndParentNotepad(transform);
    }
    private void StartTrip()
    {
        fadeBlack.FadeInChangeScene("FIND where the TRAITORS are GOING", SceneType.Trip, sceneIndex: 2);
        for (int i = 0; i < keyIconRenderers.Length; i++)
        {
            keyIconRenderers[i].enabled = false;
        }
        SceneController.KeepNotepad(notepad);
    }
    private void ShowSpaceIcon(Vector2 position)
    {
        ShowKeyIcon(keyIconRenderers[0], position, KeyboardBindingIconIndices.Spacebar, Direction.Up);
    }
    private void HideKeyIcons()
    {
        for (int i = 0; i < keyIconRenderers.Length; i++)
        {
            HideKeyIcon(i);
        }
    }
    private void HideKeyIcon(int index)
    {
        keyIconRenderers[index].enabled = false;
    }
    private void GetNotepad()
    {
        notepad = SceneController.GetAndParentNotepad(transform);
        notepad.PickUpNotepad();
        notepad.transform.localPosition = notepadData.offSceenLocalPos;
    }
    private void SetFadeForAgreement()
    {
        fadeBlack.FadeIn(value: 1, uvPosX: 0.5f, uvPosY: 0, alpha: 0.2f, FadeBlack.NOTEPAD_DEPTH);
    }
    private void SetFadeOut()
    {
        fadeBlack.FadeOut();
    }
    private void SetToNoneStateFromOutcome()
    {
        if(curState == UIState.Outcome)
        {
            SetState(UIState.None);
        }
    }
    private void SetToNoneState()
    {
        SetState(UIState.None);
    }
    private void SetToNotepadState()
    {
        HideKeyIcons();
        SetState(UIState.Notepad);
    }
    private void SetToStartMenuState()
    { 
        SetState(UIState.StartMenu);
    }
    private void SetToOutcomeState()
    {
        SetState(UIState.Outcome);
    }
    private void SetToEndMenuState()
    {
        SetState(UIState.EndMenu);
    }
    private void UpdateMainMenuButtons()
    {
        startButton.UpdateButton();
        quitButton.UpdateButton();
        optionsButton.UpdateButton();
    }
    private void UpdateOptionsButtons()
    {
        optionsButton.UpdateButton();
        darkColorButton.UpdateButton();
        lightColorButton.UpdateButton();
    }
    private void UpdateEndMenuButtons()
    {
        quitButton.UpdateButton();
    }
    private void PlayAgainClicked()
    {
        quitButton.MoveAway(Direction.Left);

        OnPlayAgain?.Invoke();
    }
    private void StartButtonClicked(TextButton text)
    {
        startButton.MoveAway(Direction.Left);
        optionsButton.MoveAway(Direction.Left);
        quitButton.MoveAway(Direction.Left);

        ShowMoveKeyIcons();
        SetState(UIState.None);
        
        OnStartButtonClicked?.Invoke();
    }
    private void OptionsButtonClicked(TextButton text)
    {
        switch(curState)
        {
            case UIState.StartMenu:
            {
                SetState(UIState.OptionsMenu);
            }
            break;
            case UIState.OptionsMenu:
            {
                SetState(UIState.StartMenu);
            }
            break;
        }
    }
    private void QuitButtonClicked(TextButton text)
    {
        Application.Quit();
    }
    private void DarkColorButtonClicked(TextButton text)
    {
        darkColorPicker.Open(darkColorButton.textRenderer.backgroundRenderer, Direction.Right);
        lightColorPicker.Close();
    }
    private void LightColorButtonClicked(TextButton text)
    {
        lightColorPicker.Open(lightColorButton.textRenderer.backgroundRenderer, Direction.Right);
        darkColorPicker.Close();
    }
    private void EnterTextButton(TextButton text)
    {
       // text.textRenderer.Set
    }
    private void ExitTextButton(TextButton button)
    {

    }
    private void ShowMoveKeyIcons()
    {
        if (!spyStats.movedFirstTime)
        {
            ShowKeyIcon(keyIconRenderers[0], new Vector2(spyStats.bounds.min.x, spyStats.bounds.center.y), KeyboardBindingIconIndices.A, Direction.Left);

            ShowKeyIcon(keyIconRenderers[1], new Vector2(spyStats.bounds.max.x, spyStats.bounds.center.y), KeyboardBindingIconIndices.D, Direction.Right);
        }
    }
}
