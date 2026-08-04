using Cysharp.Threading.Tasks;
using System;
using System.Threading;

using UnityEngine;

using static AtlasUI;
using static Scenes;
using static Spy;

public class StartUI : MonoBehaviour
{
    const float OUTCOME_PAGE_X_OFFSET = 0.3f;
    const float OUTCOME_PAGE_MOVE_X = 0.5f;
    const float OUTCOME_PAGE_MOVE_Y = 0.5f;

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
    public SpyStatsSO spyStats;

    public Transform titleTransform;

    public TextUIElement startButton;
    public TextUIElement optionsButton;
    public TextUIElement quitButton;

    public TextUIElement darkColorButton;
    public TextUIElement lightColorButton;
    public TextUIElement tutorialButton;

    public TextUIElement thankYouMessage;
    public TextUIElement playAgainButton;

    public TextUIElement traitorsFoundMessage;
    public TextUIElement outcomeMessage;

    public IconUIElement titleIcon1;
    public IconUIElement titleIcon2;


    public FadeBlack fadeBlack;

    [Header("Generated")]
    public Vector3[] outcomePageInactivePositions;
    public Vector3[] outcomePageHoverPositions;
    public Vector3[] outcomePageActivePositions;

    public Vector3 naturalMovePos;
    public Vector3 outcomePageStartPos;


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
    public CancellationTokenSource ctsPagesStartOutcome;

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

        NotepadProp.OnNotepadCollect += DisableInteractIcon;
        NotepadProp.OnNotepadCollect += GetNotepad;
        NotepadProp.OnNotepadReturn += DisableInteractIcon;
        NotepadProp.OnNotepadReturn += SetToOutcomeState;

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

        NotepadProp.OnNotepadCollect -= DisableInteractIcon;
        NotepadProp.OnNotepadCollect -= GetNotepad;
        NotepadProp.OnNotepadReturn -= DisableInteractIcon;
        NotepadProp.OnNotepadReturn -= SetToOutcomeState;

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
        
        darkColorButton.SetAway(camStats, MoveButtonDirection.Left);
        lightColorButton.SetAway(camStats, MoveButtonDirection.Left);
        tutorialButton.SetAway(camStats, MoveButtonDirection.Left);
        thankYouMessage.SetAway(camStats, MoveButtonDirection.Left);
        playAgainButton.SetAway(camStats, MoveButtonDirection.Left);
        traitorsFoundMessage.SetAway(camStats, MoveButtonDirection.Left);
        outcomeMessage.SetAway(camStats, MoveButtonDirection.Left);

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

        traitorsFoundMessage.InitMessage();
        outcomeMessage.InitMessage();
        
        titleIcon1.InitImage();
        titleIcon2.InitImage();

        startButton.SetAway(camStats, MoveButtonDirection.Left);
        optionsButton.SetAway(camStats, MoveButtonDirection.Left);
        quitButton.SetAway(camStats, MoveButtonDirection.Left);

        darkColorButton.SetAway(camStats, MoveButtonDirection.Left);
        lightColorButton.SetAway(camStats, MoveButtonDirection.Left);
        tutorialButton.SetAway(camStats, MoveButtonDirection.Left);
        
        thankYouMessage.SetAway(camStats, MoveButtonDirection.Left);
        playAgainButton.SetAway(camStats, MoveButtonDirection.Left);

        traitorsFoundMessage.SetAway(camStats, MoveButtonDirection.Left);
        outcomeMessage.SetAway(camStats, MoveButtonDirection.Left);

        titleIcon1.SetButtonAway(camStats, MoveButtonDirection.Left);
        titleIcon2.SetButtonAway(camStats, MoveButtonDirection.Left);

        titleTransform.SetParent(null);

        fadeBlack.SetAlpha(1);
        fadeBlack.FadeOut();

        Shader.SetGlobalFloat("_DayNight", 1);

        SetState(UIState.None);

        notepad = SceneController.GetNotepad(transform);
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
    private void SetToOutcomeState()
    {
        SetState(UIState.Outcome);
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
                startButton.MoveElementToActive();
                optionsButton.MoveElementToActive();
                quitButton.MoveElementToActive();
            }
            break;

            case UIState.Notepad:
            {
                notepad.EnterNotepad();
                naturalMovePos = notepadData.activeLocalPos;
                ctsNotepad?.Cancel();
            }
            break;

            case UIState.Outcome:
            {
                notepad.SkipToPage(0);
                traitorsFoundMessage.MoveElementToActive(snap: false);
                outcomeMessage.MoveElementToActive(snap: false);
                spyStats.playerInputsEnabled = false;

                profilePages = new Page[trip.traitorProfiles.Length];


                outcomePageHoverPositions = new Vector3[trip.traitorProfiles.Length];
                outcomePageActivePositions = new Vector3[trip.traitorProfiles.Length];
                outcomePageInactivePositions = new Vector3[trip.traitorProfiles.Length];

                outcomePageStartPos.x = -notepad.pages[1].paperRenderer.bounds.size.x - OUTCOME_PAGE_X_OFFSET;
                outcomePageStartPos.y = notepadData.offSceenLocalPos.y;

                for (int i = 0; i < trip.traitorProfiles.Length; i++)
                {
                    Page profilePage = notepad.pages[i + 1];

                    int index = i;

                    void OnEnter(IconUIElement icon)
                    {
                        hoveredPage = profilePages[index];
                        icon.renderer.custom.x = 1;
                    }
                    void OnExit(IconUIElement icon)
                    {
                        icon.renderer.custom.x = 0;
                    }
                    void OnClick(IconUIElement icon)
                    {
                        activePage = profilePages[index];
                        icon.renderer.custom.x = 0;
                    }
                    profilePage.exitButton.InitButton(OnClick, OnEnter, OnExit);
                    
                    profilePage.transform.SetParent(transform, worldPositionStays: true);
                    outcomePageStartPos.z = notepadData.activeLocalPos.z + (i * 3);
                    profilePage.transform.localPosition = outcomePageStartPos;

                    profilePage.gameObject.SetActive(true);

                    profilePages[i] = profilePage;

                    Page samplePage = profilePages[0];
                    Bounds exitButtonBounds = samplePage.exitButton.renderer.bounds;
                    float exitButtonDistFromLeftEdge = exitButtonBounds.min.x - samplePage.paperRenderer.bounds.min.x;
                    float xOffset = exitButtonBounds.size.x + (exitButtonDistFromLeftEdge * 2);

                    outcomePageInactivePositions[i].x = outcomePageStartPos.x - (xOffset * i);
                    outcomePageInactivePositions[i].y = notepadData.inactiveLocalPos.y + ((i % 2) * 0.02f);
                    outcomePageInactivePositions[i].z = outcomePageStartPos.z;

                    outcomePageHoverPositions[i].x = outcomePageInactivePositions[i].x;
                    outcomePageHoverPositions[i].y = notepadData.hoverLocalPos.y;
                    outcomePageHoverPositions[i].z = outcomePageStartPos.z;

                    outcomePageActivePositions[i].x = outcomePageInactivePositions[i].x;
                    outcomePageActivePositions[i].y = notepadData.activeLocalPos.y;
                    outcomePageActivePositions[i].z = outcomePageStartPos.z;
                }

                MoveProfilePagesToStartOutComePosition();
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
                    if (notepad.transform.parent != transform) return;
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

            case UIState.Outcome:
            {
                if (notepad == null || notepad.transform.parent != transform) return;
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.offSceenLocalPos, Time.deltaTime * MOVE_DAMP);

                if (outcomeSetUpCompleted)
                {
                    if (activePage == null)
                    {
                        for (int i = 0; i < profilePages.Length; i++)
                        {
                            Page page = profilePages[i];

                            page.exitButton.UpdateButton(playerInputs);
                    
                            if (page == hoveredPage)
                            {
                                if (CursorController.IsInsideBounds(page.paperRenderer.bounds, isClickable:  false))
                                {
                                    float curPosY = Mathf.Lerp(page.transform.localPosition.y, notepadData.hoverLocalPos.y, Time.deltaTime * MOVE_DAMP);
                                    page.transform.localPosition = new Vector3(page.transform.localPosition.x, curPosY, page.transform.localPosition.z);
                                }
                                else
                                {
                                    hoveredPage = null;
                                }
                            }
                            else
                            {
                                float curPosY = Mathf.Lerp(page.transform.localPosition.y, notepadData.inactiveLocalPos.y, Time.deltaTime * MOVE_DAMP);
                                page.transform.localPosition = new Vector3(page.transform.localPosition.x, curPosY, page.transform.localPosition.z);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < profilePages.Length; i++)
                        {
                            Page page = profilePages[i];

                            if (page == activePage)
                            {
                                float curPosY = Mathf.Lerp(page.transform.localPosition.y, notepadData.activeLocalPos.y, Time.deltaTime * MOVE_DAMP);
                                page.transform.localPosition = new Vector3(page.transform.localPosition.x, curPosY, page.transform.localPosition.z);
                            }
                            else
                            {
                                float curPosY = Mathf.Lerp(page.transform.localPosition.y, notepadData.inactiveLocalPos.y, Time.deltaTime * MOVE_DAMP);
                                page.transform.localPosition = new Vector3(page.transform.localPosition.x, curPosY, page.transform.localPosition.z);
                            }
                        }
                    }

                }
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
        
        startButton.MoveElementToActive();
        optionsButton.MoveElementToActive();

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

                darkColorButton.MoveElementToActive();
                lightColorButton.MoveElementToActive();
                tutorialButton.MoveElementToActive();

                OnClickOptions?.Invoke();
                SetState(UIState.OptionsMenu);
            }
            break;
            case UIState.OptionsMenu:
            {
                optionsButton.renderer.SetText("Options");
                startButton.MoveElementToActive();
                quitButton.MoveElementToActive();
                optionsButton.MoveElementToActive();

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
    private void MoveProfilePagesToStartOutComePosition()
    {
        ctsPagesStartOutcome?.Cancel();
        ctsPagesStartOutcome = new CancellationTokenSource();
        MovingProfilePagesToStartOutComePosition().Forget();
    }
    private async UniTask MovingProfilePagesToStartOutComePosition()
    {
        try
        {
            float elapsedTime = 0;

            while(elapsedTime < OUTCOME_PAGE_MOVE_Y)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / OUTCOME_PAGE_MOVE_Y;

                for (int i = 0; i < profilePages.Length; i++)
                {
                    Page page = profilePages[i];
                    float posY = Mathf.Lerp(outcomePageStartPos.y, outcomePageInactivePositions[i].y, t);
                    page.transform.localPosition = new Vector3(page.transform.localPosition.x, posY, page.transform.localPosition.z);
                }
                await UniTask.Yield(ctsPagesStartOutcome.Token);
            }
            
            elapsedTime = 0;

            while(elapsedTime < OUTCOME_PAGE_MOVE_X)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / OUTCOME_PAGE_MOVE_X;

                for (int i = 0; i < profilePages.Length; i++)
                {
                    Page page = profilePages[i];
                    float posX = Mathf.Lerp(outcomePageStartPos.x, outcomePageInactivePositions[i].x, t);
                    page.transform.localPosition = new Vector3(posX, page.transform.localPosition.y, page.transform.localPosition.z);
                }
                await UniTask.Yield(ctsPagesStartOutcome.Token);
            }
            outcomeSetUpCompleted = true;
        }
        catch (OperationCanceledException)
        {
            Page samplePage = profilePages[0];
            Bounds exitButtonBounds = samplePage.exitButton.renderer.bounds;
            float exitButtonDistFromLeftEdge = exitButtonBounds.min.x - samplePage.paperRenderer.bounds.min.x;
            float xOffset = exitButtonBounds.size.x + (exitButtonDistFromLeftEdge * 2);

            for (int i = 0; i < profilePages.Length; i++)
            {
                Page page = profilePages[i];
                page.transform.localPosition = outcomePageInactivePositions[i];
            }

            outcomeSetUpCompleted = true;
        }
    }
}
