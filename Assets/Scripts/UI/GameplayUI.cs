using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Spy;
public class GameplayUI : MonoBehaviour
{
    const float TICKET_ICON_PADDING = 0.2f;
    const float APPEARING_TIME = 0.5f;

    public static event Action OnIncreaseTraitorCountFirstTime;

    public PlayerInputsSO playerInputs;
    public CameraStatsSO cameraStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    public NotepadData notepadData;
    public TripSO trip;
    public OptionsSO options;
    public TrainStatsSO trainStats;

    public FadeBlack fadeBlack;
    public UnlockPicker unlockPicker;
    public SceneData sceneData;

    public Material fadeBlackMaterial; 

    public Ticket ticket;

    public TicketIcon ticketIconPrefab;

    public AtlasRenderer carriageMap;

    public IconUIElement traitorIcon;
    public IconUIElement redoButton;
    public IconUIElement quitButton;
    
    public AtlasRenderer keyIcon;

    public AtlasTextRenderer traitorCountText;
    public AtlasTextRenderer tutorialRenderer;

    public Transform ticketIconTransform;

    [Header("Generated")]
    public TicketIcon[] ticketIcons;
    
    public Notepad notepad;

    public TicketIcon curTicketIcon;
    
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsTicket;
    public CancellationTokenSource ctsCarriageMap;
    public CancellationTokenSource ctsFadeBlack;

    public IconUIElement curTutorialIcon;

    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public Vector3 carriageMapActivePos;
    public Vector3 carriageMapInactivePos;

    public Vector3 naturalMovePos;

    public UIState curState;
    public UnlockType curUnlockType;

    public int ticketCount;
    public int traitorCount;

    public float naturalMoveClock;
    public float fadeBlackClock;

    public bool canExitState;

    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetNewTicketIcons);
        gameEventData.OnStationLeave.RegisterListener(SetTraitorIcons);

        gameEventData.OnStationArrival.RegisterListener(DisappearTicketIcons);

        SpyBrain.OnTicketCheckHoverDisabled += RevertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverDisabled += HideKeyIcon;
        SpyBrain.OnTicketCheckHoverEnabled += InvertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverEnabledFirstTime += ShowEIcon;
        SpyBrain.OnFoundExteriorSlideDoors += ShowWIcon;
        SpyBrain.OnWalkPastExteriorSlideDoors += HideKeyIcon;
        SpyBrain.OnEnteredTrain += DisappearKeyIcon;
        SpyBrain.OnTicketInspect += DisappearKeyIcon;
        SpyBrain.OnOpenNotepad += SetToNotepadState;
        SpyBrain.OnCloseNotepad += SetToNoneState;
        SpyBrain.OnTicketInspect += SetToTicketState;
        SpyBrain.OnFinishTicketInspect += SetToNoneState;

        NPCBrain.OnTraitorDisembarkedTrain += DecreaseTraitorCount;
        NPCBrain.OnTraitorBoardedTrain += IncreaseTraitorCount;

        Scenes.OnLoadTrip0 += Init;

        UnlockPicker.OnRuleOutAbilityUnlock += MoveRuleOutMarkerTutorialIcon;
        UnlockPicker.OnColorAbilityUnlock += MoveColorMarkerTutorialIcon;

        Notepad.OnWriteColorMarkerFirstTime += SetTutorialTextToColor2;

        ColorPicker.OnSelectClueColorFirstTime += SetTutorialTextToColor3;
    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetNewTicketIcons);
        gameEventData.OnStationLeave.UnregisterListener(SetTraitorIcons);

        gameEventData.OnStationArrival.UnregisterListener(DisappearTicketIcons);

        SpyBrain.OnTicketCheckHoverDisabled -= RevertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverDisabled -= HideKeyIcon;
        SpyBrain.OnTicketCheckHoverEnabled -= InvertCurTicketIcon;
        SpyBrain.OnFoundExteriorSlideDoors -= ShowWIcon;
        SpyBrain.OnTicketCheckHoverEnabledFirstTime -= ShowEIcon;
        SpyBrain.OnWalkPastExteriorSlideDoors -= HideKeyIcon;
        SpyBrain.OnEnteredTrain -= DisappearKeyIcon;
        SpyBrain.OnTicketInspect -= DisappearKeyIcon;
        SpyBrain.OnOpenNotepad -= SetToNotepadState;
        SpyBrain.OnCloseNotepad -= SetToNoneState;
        SpyBrain.OnTicketInspect -= SetToTicketState;
        SpyBrain.OnFinishTicketInspect -= SetToNoneState;
        
        NPCBrain.OnTraitorDisembarkedTrain -= DecreaseTraitorCount;
        NPCBrain.OnTraitorBoardedTrain -= IncreaseTraitorCount;

        UnlockPicker.OnRuleOutAbilityUnlock -= MoveRuleOutMarkerTutorialIcon;
        UnlockPicker.OnColorAbilityUnlock -= MoveColorMarkerTutorialIcon;

        Scenes.OnLoadTrip0 -= Init;

        Notepad.OnWriteColorMarkerFirstTime -= SetTutorialTextToColor2;

        ColorPicker.OnSelectClueColorFirstTime -= SetTutorialTextToColor3;
    }
    private void Update()
    {
        UpdateState();
        fadeBlack.CheckToFadeOutSceneChange();
        HandlePlayAgainButton();
        HandleQuitButton();
    }
    private void Init()
    {
        InitPOVUI();
        InitTicketIcons();
        InitUIElements();
    }
    private void SetState(UIState newState)
    {
        if (curState == newState) return;
        ExitState(newState);
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
                naturalMovePos = Notepad.ACTIVE_POS;
                ctsNotepad?.Cancel();
            }
            break;
            case UIState.Ticket:
            {
                ticket.gameObject.SetActive(true);
                naturalMovePos = ticketActivePos;
                ticket.SetText(spyStats.boardingStationName, spyStats.disembarkingStationName);
                ctsTicket?.Cancel();
            }
            break;
            case UIState.CarriageMap:
            {

                carriageMap.gameObject.SetActive(true);
                ctsCarriageMap?.Cancel();

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
                UpdateNaturalPos(Notepad.ACTIVE_POS, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
                if ((notepad.transform.localPosition - naturalMovePos).sqrMagnitude < 0.05f) notepadData.subState |= Notepad.SubState.InUse;
                switch (spyStats.curTutorialState)
                {
                    case TutorialState.Color1:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                        }
                    }
                    break;
                    case TutorialState.Color2:
                    {

                    }
                    break;
                    case TutorialState.Color3:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            curTutorialIcon.renderer.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 4);
                            curTutorialIcon.renderer.transform.SetParent(unlockPicker.transform, true);

                            fadeBlack.FadeOut();

                            spyStats.tutorialsCompleted |= spyStats.curTutorialState;
                            spyStats.curTutorialState = TutorialState.None;
                            spyStats.playerInputsEnabled = true;
                            tutorialRenderer.SetText("");

                            spyStats.checkingNotepad = false;
                        }
                    }
                    break;
                }
                canExitState = true;
            }
            break;
            case UIState.Ticket:
            {
                UpdateNaturalPos(ticketActivePos, ref naturalMovePos);
                ticket.transform.localPosition = Vector3.Lerp(ticket.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
            }
            break;
            case UIState.CarriageMap:
            {
                carriageMap.transform.localPosition = Vector3.Lerp(carriageMap.transform.localPosition, carriageMapActivePos, Time.deltaTime * MOVE_DAMP);
            }
            break;
            case UIState.None:
            {
                switch(spyStats.curTutorialState)
                {
                    case TutorialState.None:
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
                    break;
                    case TutorialState.Ticket:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                            else
                            {
                                curTutorialIcon.MoveBackTutorialUIElement(tutorialRenderer);
                                fadeBlack.FadeOut();

                                curTicketIcon.RipStubTicket();
                                curTicketIcon.InvertIcon(toggle: false);
                                curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];

                                spyStats.tutorialsCompleted |= spyStats.curTutorialState;
                                spyStats.curTutorialState = TutorialState.None;
                                spyStats.playerInputsEnabled = true;
                            }
                        }
                    }
                    break;
                    case TutorialState.Traitor:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                            else
                            {
                                curTutorialIcon.MoveBackTutorialUIElement(tutorialRenderer);
                                fadeBlack.FadeOut();

                                traitorIcon.renderer.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 1);
                                traitorCountText.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 1);
                            
                                spyStats.tutorialsCompleted |= spyStats.curTutorialState;
                                spyStats.curTutorialState = TutorialState.None;
                                spyStats.playerInputsEnabled = true;
                            }
                        }
                    }
                    break;
                    case TutorialState.RuleOut:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                            else
                            {
                                curTutorialIcon.renderer.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 4);
                                curTutorialIcon.renderer.transform.SetParent(unlockPicker.transform, true);

                                fadeBlack.FadeOut();

                                spyStats.tutorialsCompleted |= spyStats.curTutorialState;
                                spyStats.curTutorialState = TutorialState.None;
                                spyStats.playerInputsEnabled = true;
                                tutorialRenderer.SetText("");
                            }
                        }
                    }
                    break;
                    case TutorialState.Color1:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                        }
                    }
                    break;
                }
                canExitState = true;
            }
            break;
        }
    }
    private void ExitState(UIState newState)
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                MoveUIElement(notepad.transform, notepadData.inactiveLocalPos, ref ctsNotepad, newState);
                notepad.ExitNotepad();
                SceneController.GetClueColorPicker().Close();
            }
            break;
            case UIState.Ticket:
            {
                MoveUIElement(ticket.transform, ticketInactivePos, ref ctsTicket, newState);

                if (!options.skipTutorial && ((spyStats.tutorialsCompleted & TutorialState.Ticket) == 0))
                {
                    curTicketIcon.mainTicket.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.ticketCountTutorialText);
                    curTutorialIcon = curTicketIcon.mainTicket;
                    fadeBlack.FadeInWithSpacebar(value: 0.8f, spacebarWaitTime: 2.5f);
                    spyStats.curTutorialState = TutorialState.Ticket;
                    spyStats.playerInputsEnabled = false;
                }
                else
                {
                    curTicketIcon.RipStubTicket();
                    if (trip.ticketsCheckedSinceLastStation < ticketIcons.Length)
                    {
                        curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];
                    }
                }

            }
            break;
            case UIState.CarriageMap:
            {
                MoveUIElement(carriageMap.transform, carriageMapInactivePos, ref ctsCarriageMap, newState);
            }
            break;
            case UIState.None:
            {

            }
            break;
        }
    }
    private void SetToNotepadState()
    {
        SetState(UIState.Notepad);
    }
    private void SetToTicketState()
    {
        SetState(UIState.Ticket);
    }
    private void SetToCarriageMapState()
    {
        SetState(UIState.CarriageMap);
    }
    private void SetToNoneState()
    {
        SetState(UIState.None);
    }
    private void MoveRuleOutMarkerTutorialIcon(IconUIElement icon)
    {
        if (!options.skipTutorial)
        {
            icon.renderer.transform.SetParent(transform, true);
            icon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.passengerRuleOutTutorialText);
            icon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);
            
            curTutorialIcon = icon;

            fadeBlack.FadeInWithSpacebar(value: 0.8f, spacebarWaitTime: 2.5f);
            spyStats.curTutorialState = TutorialState.RuleOut;
            spyStats.playerInputsEnabled = false;

            SceneController.GetSpy().FinishWithChosenNPC();
            SceneController.GetUnlockPicker().Close();
        }
    }
    private void MoveColorMarkerTutorialIcon(IconUIElement icon)
    {
        if (!options.skipTutorial)
        {
            SetToNotepadState();
            spyStats.checkingNotepad = true;
            notepad.FlipToPage(notepad.colorKeyPage.pageIndex);
            
            icon.renderer.transform.SetParent(transform, true);
            icon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.passengerColorMarkerTutorialText1);
            icon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);

            curTutorialIcon = icon;

            fadeBlack.FadeIn(value: 1, alpha: 0.2f, fadeBlackZPos: FadeBlack.NOTEPAD_DEPTH);
            spyStats.curTutorialState = TutorialState.Color1;
            spyStats.playerInputsEnabled = false;

            SceneController.GetSpy().FinishWithChosenNPC();
            SceneController.GetSpy().SetStateToNotepad();
            SceneController.GetUnlockPicker().Close();
        }
    }
    private void InitPOVUI()
    {
        notepad = SceneController.GetNotepad(transform);
        notepad.transform.localPosition = notepadData.inactiveLocalPos;

        float halfCamWidth = cameraStats.camBounds.extents.x;
        float halfCamHeight = cameraStats.camBounds.extents.y;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, -halfCamHeight + ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMap.transform.localPosition.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;
    }
    private void InitTicketIcons()
    {
        ticketIcons = new TicketIcon[3];

        float ticketIconSpacing = ticketIconPrefab.mainTicket.renderer.bounds.size.x + ticketIconPrefab.stubTicket.renderer.bounds.size.x + TICKET_ICON_PADDING;
        for (int i = 0; i < ticketIcons.Length; i++)
        {
            float xPos = ticketIconTransform.position.x + (ticketIconSpacing * i);
            Vector3 pos = new Vector3(xPos, ticketIconTransform.position.y, ticketIconTransform.position.z);
            TicketIcon ticketIcon = Instantiate(ticketIconPrefab, pos, Quaternion.identity, transform);
            ticketIcon.Init();
            ticketIcon.name = "TicketIcon" + i;
            ticketIcons[i] = ticketIcon;
        }
        curTicketIcon = ticketIcons[0];
    }
    private void InitUIElements()
    {
        traitorIcon.activePos = traitorIcon.renderer.transform.localPosition;
        
        traitorIcon.renderer.SetAlpha(0);
        traitorCountText.SetAppearTextAlpha(0);

        quitButton.activePos = quitButton.renderer.transform.localPosition;
        redoButton.activePos = redoButton.renderer.transform.localPosition;
    }
    private void SetNewTicketIcons()
    {
        curTicketIcon = ticketIcons[0];
        SettingNewTicketIcons().Forget();
    }
    private void SetTraitorIcons()
    {
        if (trainStats.curStationIndex == 1)
        {
            traitorIcon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 4);
            traitorCountText.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 4);
        }
    }
    private void DisappearTicketIcons()
    {
        trip.ticketsCheckedSinceLastStation = 0;
        DisappearingTicketIcons().Forget();
    }
    private void InvertCurTicketIcon()
    {
        curTicketIcon.InvertIcon(toggle: true);
    }
    private void RevertCurTicketIcon()
    {
        if (spyStats.curTutorialState != TutorialState.Ticket)
        {
            curTicketIcon?.InvertIcon(toggle: false);
        }
    }
    private void ShowWIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeySpriteIndices.W);
    }
    private void ShowEIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeySpriteIndices.E);
    }
    private void ShowQIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeySpriteIndices.Q);
    }
    private void ShowSIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeySpriteIndices.S);
    }
    private void HideKeyIcon()
    {
        keyIcon.custom.w = 0;
    }
    private void DisappearKeyIcon()
    {
        if (keyIcon.custom.w == 1)
        {
            DisappearingKeyIcon().Forget();
        }
    }
    private void SetTutorialTextToColor2()
    {
        tutorialRenderer.SetText(options.passengerColorMarkerTutorialText2);
        spyStats.tutorialsCompleted |= spyStats.curTutorialState;
        spyStats.curTutorialState = TutorialState.Color2;
    }
    private void SetTutorialTextToColor3()
    {
        tutorialRenderer.SetText(options.passengerColorMarkerTutorialText3);
        spyStats.tutorialsCompleted |= spyStats.curTutorialState;
        spyStats.curTutorialState = TutorialState.Color3;
        fadeBlack.WaitAndSetSpacebar(waitTime: 2.5f);
    }
    private void IncreaseTraitorCount()
    {
        traitorCount++;
        traitorCountText.SetText("x" + traitorCount);

        if (!options.skipTutorial && ((spyStats.tutorialsCompleted & TutorialState.Traitor) == 0))
        {
            traitorIcon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.traitorCountTutorialText);
            curTutorialIcon = traitorIcon;

            fadeBlack.FadeInWithSpacebar(value: 0.8f, spacebarWaitTime: 2.5f);
            spyStats.curTutorialState = TutorialState.Traitor;
            spyStats.playerInputsEnabled = false;

            traitorIcon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);
            traitorCountText.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);
            OnIncreaseTraitorCountFirstTime?.Invoke();
        }
    }
    private void DecreaseTraitorCount()
    {
        traitorCount--;
        traitorCountText.SetText("x" + traitorCount);
    }
    private void HandlePlayAgainButton()
    {
        if (CursorController.IsInsideBounds(redoButton.renderer.bounds, isClickable: true))
        {
            redoButton.renderer.custom.w = 1;

            if (playerInputs.mouseLeftDown)
            {
                fadeBlack.FadeInChangeScene(options.startTripText, Scenes.SceneType.Trip, sceneIndex: 2);
                gameEventData.OnResetTrip.Raise();
            }
        }
        else
        {
            redoButton.renderer.custom.w = 0;
        }
    }
    private void HandleQuitButton()
    {
        if (CursorController.IsInsideBounds(quitButton.renderer.bounds, isClickable: true))
        {
            quitButton.renderer.custom.w = 1;

            if (playerInputs.mouseLeftDown)
            {
                Application.Quit();
            }
        }
        else
        {
            quitButton.renderer.custom.w = 0;
        }
    }

    private async UniTask DisappearingKeyIcon()
    {
        float elapsedTime = APPEARING_TIME;

        while(elapsedTime < APPEARING_TIME)
        {
            elapsedTime -= Time.deltaTime;
            keyIcon.custom.w = elapsedTime / APPEARING_TIME;
            await UniTask.Yield();
        }
        keyIcon.custom.w = 0;
    }
    private async UniTask SettingNewTicketIcons()
    {
        ticketCount = trip.stationAhead.ticketsToCheckBeforeSpawn;
        int curTicketIconIndex = 0;

        while(curTicketIconIndex < ticketCount)
        {
            ticketIcons[curTicketIconIndex].Appear();
            curTicketIconIndex++;
            await UniTask.WaitForSeconds(APPEARING_TIME);
        }

        SpyBrain.ToggleTicketCheckAbility(toggle: true);


    }
    private async UniTask DisappearingTicketIcons()
    {
        ticketCount = trip.stationAhead.ticketsToCheckBeforeSpawn;
        int curTicketIconIndex = ticketCount - 1;

        while (curTicketIconIndex >= 0)
        {
            ticketIcons[curTicketIconIndex].InvertIcon(toggle: false);
            ticketIcons[curTicketIconIndex].Disappear();
            curTicketIconIndex--;
            await UniTask.WaitForSeconds(APPEARING_TIME);
        }
    }
}
