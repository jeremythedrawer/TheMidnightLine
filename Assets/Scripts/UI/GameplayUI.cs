using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Spy;
public class GameplayUI : MonoBehaviour
{
    const float APPEARING_TIME = 0.5f;
    const float RESET_TIME = 2;
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

    public RailMap railMap;

    public AtlasRenderer carriageMap;

    public IconUIElement traitorIcon;
    public IconUIElement redoButton;
    
    public AtlasRenderer keyIcon;

    public AtlasTextRenderer traitorCountText;
    public AtlasTextRenderer tutorialRenderer;

    [Header("Generated")]
    
    public Notepad notepad;
    
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
    
    public float resetClock;

    public bool canExitState;
    public bool uiAtNightMode;
    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetTraitorIcons);
        gameEventData.OnStationLeave.RegisterListener(HideKeyIcon);

        SpyBrain.OnTicketCheckHoverDisabled += HideKeyIcon;
        SpyBrain.OnTicketCheckHoverEnabledFirstTime += ShowEIcon;
        SpyBrain.OnAtSlideDoors += ShowSpaceIcon;
        SpyBrain.OnWalkPastSlideDoors += HideKeyIcon;
        SpyBrain.OnEnteredTrain += AppearRailMap;
        SpyBrain.OnExitTrain += DissappearRailMap;
        SpyBrain.OnEnteredTrain += DisappearKeyIcon;
        SpyBrain.OnTicketInspect += DisappearKeyIcon;
        SpyBrain.OnTicketInspect += MoveRailMap;
        SpyBrain.OnOpenNotepad += SetToNotepadState;
        SpyBrain.OnCloseNotepad += SetToNoneState;
        SpyBrain.OnTicketInspect += SetToTicketState;
        SpyBrain.OnFinishTicketInspect += SetToNoneState;
        SpyBrain.OnUncheckCarriageMap += SetToNoneState;
        SpyBrain.OnCheckCarriageMap += SetToCarriageMapState;

        NPCBrain.OnTraitorDisembarkedTrain += DecreaseTraitorCount;
        NPCBrain.OnTraitorBoardedTrain += IncreaseTraitorCount;

        Scenes.OnLoadTrip0 += Init;

        UnlockPicker.OnRuleOutAbilityUnlock += MoveRuleOutMarkerTutorialIcon;
        UnlockPicker.OnColorAbilityUnlock += MoveColorMarkerTutorialIcon;
        UnlockPicker.OnMutliColorAbilityUnlock += MoveMulticolorMarkerTutorialIcon;

        Notepad.OnWriteColorMarkerFirstTime += SetTutorialTextToColor2;
        Notepad.OnRevertTutorial += SetTutorialTextBack;

        ColorPicker.OnSelectClueColorFirstTime += SetTutorialTextToColor3;
        ColorPicker.OnSelectSecondClueColorFirstTime += SetTutorialTextToMultiColor2;

        CursorController.OnClickExamplePassenger += FinishTutorial;
    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetTraitorIcons);
        gameEventData.OnStationLeave.UnregisterListener(HideKeyIcon);

        SpyBrain.OnTicketCheckHoverDisabled -= HideKeyIcon;
        SpyBrain.OnAtSlideDoors -= ShowSpaceIcon;
        SpyBrain.OnTicketCheckHoverEnabledFirstTime -= ShowEIcon;
        SpyBrain.OnWalkPastSlideDoors -= HideKeyIcon;
        SpyBrain.OnEnteredTrain -= DisappearKeyIcon;
        SpyBrain.OnExitTrain -= DissappearRailMap;
        SpyBrain.OnTicketInspect -= DisappearKeyIcon;
        SpyBrain.OnTicketInspect -= MoveRailMap;
        SpyBrain.OnOpenNotepad -= SetToNotepadState;
        SpyBrain.OnCloseNotepad -= SetToNoneState;
        SpyBrain.OnTicketInspect -= SetToTicketState;
        SpyBrain.OnFinishTicketInspect -= SetToNoneState;
        SpyBrain.OnUncheckCarriageMap -= SetToNoneState;
        SpyBrain.OnCheckCarriageMap -= SetToCarriageMapState;
        SpyBrain.OnEnteredTrain += AppearRailMap;
        
        NPCBrain.OnTraitorDisembarkedTrain -= DecreaseTraitorCount;
        NPCBrain.OnTraitorBoardedTrain -= IncreaseTraitorCount;

        UnlockPicker.OnRuleOutAbilityUnlock -= MoveRuleOutMarkerTutorialIcon;
        UnlockPicker.OnColorAbilityUnlock -= MoveColorMarkerTutorialIcon;
        UnlockPicker.OnMutliColorAbilityUnlock -= MoveMulticolorMarkerTutorialIcon;

        Scenes.OnLoadTrip0 -= Init;

        Notepad.OnWriteColorMarkerFirstTime -= SetTutorialTextToColor2;
        Notepad.OnRevertTutorial -= SetTutorialTextBack;

        ColorPicker.OnSelectClueColorFirstTime -= SetTutorialTextToColor3;
        ColorPicker.OnSelectSecondClueColorFirstTime -= SetTutorialTextToMultiColor2;

        CursorController.OnClickExamplePassenger -= FinishTutorial;
    }
    private void Update()
    {
        UpdateState();
        redoButton.UpdateButton(playerInputs);
        fadeBlack.CheckToFadeOutSceneChange();

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.RightShift))
        {
            resetClock += Time.deltaTime;
            if (resetClock > RESET_TIME)
            {
                Redo();
                resetClock = 0;
            }
        }
        else if (resetClock != 0)
        {
            resetClock = 0;
        }
    }
    private void Init()
    {
        InitPOVUI();
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
                        if (canExitState && playerInputs.writeKeyDown)
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
                        if (canExitState && playerInputs.writeKeyDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                            else
                            {
                                FinishTutorial();
                            }
                        }
                    }
                    break;
                    case TutorialState.MultiColor1:
                    {
                        if (canExitState && playerInputs.writeKeyDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                        }
                    }
                    break;
                    case TutorialState.MultiColor2:
                    {

                        if (canExitState && playerInputs.writeKeyDown)
                        {
                            FinishTutorial();
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
                        if(notepad.transform.parent == transform)
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

                    case TutorialState.Traitor:
                    {
                        if (canExitState && playerInputs.writeKeyDown)
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
                    case TutorialState.RuleOut1:
                    {
                        if (canExitState && playerInputs.writeKeyDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                            else
                            {
                                tutorialRenderer.SetText(options.passengerRuleOutTutorialText2);
                                spyStats.tutorialsCompleted |= TutorialState.RuleOut1;
                                spyStats.curTutorialState = TutorialState.RuleOut2;
                                NPCBrain.ExamplePassenger.SetCustomDepth(-39);
                                NPCBrain.ExamplePassenger.atlasRenderer.customBit |= (int)ColorBits.Outline;
                            }
                        }
                    }
                    break;
                    case TutorialState.RuleOut2:
                    {
                        if (canExitState && playerInputs.writeKeyDown)
                        {
                            if (!tutorialRenderer.hasText)
                            {
                                curTutorialIcon.ctsMove?.Cancel();
                            }
                            else
                            {
                                FinishTutorial();
                            }
                        }
                    }
                    break;
                    case TutorialState.Color1:
                    {
                        if (canExitState && playerInputs.writeKeyDown)
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
        icon.renderer.transform.SetParent(transform, true);
        icon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.passengerRuleOutTutorialText1);
        icon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);
            
        curTutorialIcon = icon;

        fadeBlack.FadeInWithSpacebar(value: 0.8f, spacebarWaitTime: 2.5f, uvPosX: 0, uvPosY: 0.5f);
        spyStats.curTutorialState = TutorialState.RuleOut1;
        spyStats.playerInputsEnabled = false;
        SceneController.GetSpy().SetStateToIdle();
        SceneController.GetSpy().FinishWithChosenNPC();
        SceneController.GetUnlockPicker().Close();
    }
    private void MoveColorMarkerTutorialIcon(IconUIElement icon)
    {
        SetToNotepadState();
        spyStats.checkingNotepad = true;
        notepad.FlipToPage(notepad.colorKeyPage.pageIndex);
            
        icon.renderer.transform.SetParent(transform, true);
        icon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.passengerColorMarkerTutorialText1);
        icon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);

        curTutorialIcon = icon;

        fadeBlack.FadeIn(value: 1, alpha: 0.2f, uvPosY: 0.3f, fadeBlackZPos: FadeBlack.NOTEPAD_DEPTH);
        spyStats.curTutorialState = TutorialState.Color1;
        spyStats.playerInputsEnabled = false;

        SceneController.GetSpy().FinishWithChosenNPC();
        SceneController.GetSpy().SetStateToNotepad();
        SceneController.GetUnlockPicker().Close();
    }
    private void MoveMulticolorMarkerTutorialIcon(IconUIElement icon)
    {
        SetToNotepadState();
        spyStats.checkingNotepad = true;
        notepad.FlipToPage(notepad.colorKeyPage.pageIndex);

        icon.renderer.transform.SetParent(transform, true);
        icon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.passengerMulticolorMarkerTutorialText1);
        icon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);

        curTutorialIcon = icon;

        fadeBlack.FadeIn(value: 1, alpha: 0.2f, uvPosY: 0.3f, fadeBlackZPos: FadeBlack.NOTEPAD_DEPTH);
        spyStats.curTutorialState = TutorialState.MultiColor1;
        spyStats.playerInputsEnabled = false;

        SceneController.GetSpy().FinishWithChosenNPC();
        SceneController.GetSpy().SetStateToNotepad();
        SceneController.GetUnlockPicker().Close();
    }
    private void InitPOVUI()
    {
        notepad = SceneController.GetAndParentNotepad(transform);
        notepad.transform.localPosition = notepadData.inactiveLocalPos;

        float halfCamWidth = cameraStats.camBounds.extents.x;
        float halfCamHeight = cameraStats.camBounds.extents.y;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, -halfCamHeight + ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMapActivePos.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;
    }
    private void InitUIElements()
    {
        traitorIcon.activePos = traitorIcon.renderer.transform.localPosition;
        
        traitorIcon.renderer.SetAlpha(0);
        traitorCountText.SetAppearTextAlpha(0);
        redoButton.activePos = redoButton.renderer.transform.localPosition;
    }
    private void SetTraitorIcons()
    {
        if (trainStats.curStationIndex == 1)
        {
            traitorIcon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 4);
            traitorCountText.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 4);
        }
    }
    private void ShowSpaceIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeySpriteIndices.Space, Direction.Up);
    }
    private void ShowEIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeySpriteIndices.E, Direction.Up);
    }
    private void AppearRailMap()
    {
        railMap.Appear();
    }
    private void DissappearRailMap()
    {
        railMap.Dissappear();
    }
    private void MoveRailMap()
    {
        railMap.MoveToNextPosition();
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
    private void FinishTutorial()
    {
        switch(spyStats.curTutorialState)
        {
            case TutorialState.Color3:
            {
                spyStats.checkingNotepad = false;
            }
            break;

            case TutorialState.MultiColor2:
            {
                spyStats.checkingNotepad = false;
            }
            break;
        }

        curTutorialIcon.renderer.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 4);
        curTutorialIcon.renderer.transform.SetParent(unlockPicker.transform, true);

        fadeBlack.FadeOut();

        spyStats.tutorialsCompleted |= spyStats.curTutorialState;
        spyStats.curTutorialState = TutorialState.None;
        spyStats.playerInputsEnabled = true;
        tutorialRenderer.SetText("");
        NPCBrain.ExamplePassenger.ReturnExamplePassenger();

    }
    private void SetTutorialTextBack()
    {
        if (spyStats.curTutorialState == TutorialState.Color2 || spyStats.curTutorialState == TutorialState.Color3)
        {
            spyStats.tutorialsCompleted &= ~(TutorialState.Color1 | TutorialState.Color2 | TutorialState.Color3);
            spyStats.curTutorialState = TutorialState.Color1;
            tutorialRenderer.SetText(options.passengerColorMarkerTutorialText1);
        }
        else if (spyStats.curTutorialState == TutorialState.MultiColor2)
        {
            spyStats.tutorialsCompleted &= ~(TutorialState.MultiColor1 | TutorialState.MultiColor2);
            spyStats.curTutorialState = TutorialState.MultiColor1;
            tutorialRenderer.SetText(options.passengerMulticolorMarkerTutorialText1);
        }
    }
    private void SetTutorialTextToColor2()
    {
        tutorialRenderer.SetText(options.passengerColorMarkerTutorialText2);
        spyStats.tutorialsCompleted |= TutorialState.Color1;
        spyStats.curTutorialState = TutorialState.Color2;
    }
    private void SetTutorialTextToMultiColor2()
    {
        tutorialRenderer.SetText(options.passengerMulticolorMarkerTutorialText2);

        NPCBrain.ExamplePassenger.SetCustomDepth(-39);
        NPCBrain.ExamplePassenger.atlasRenderer.customBit |= (int)ColorBits.Outline;

        fadeBlack.CancelFadeBlack();

        spyStats.tutorialsCompleted |= spyStats.curTutorialState;
        spyStats.curTutorialState = TutorialState.MultiColor2;
    }
    private void SetTutorialTextToColor3()
    {
        tutorialRenderer.SetText(options.passengerColorMarkerTutorialText3);

        NPCBrain.ExamplePassenger.SetCustomDepth(-39);
        NPCBrain.ExamplePassenger.atlasRenderer.customBit |= (int)ColorBits.Outline;

        fadeBlack.CancelFadeBlack();

        spyStats.tutorialsCompleted |= spyStats.curTutorialState;
        spyStats.curTutorialState = TutorialState.Color3;
    }
    private void IncreaseTraitorCount()
    {
        traitorCount++;
        traitorCountText.SetText("x" + traitorCount);

        if (((spyStats.tutorialsCompleted & TutorialState.Traitor) == 0))
        {
            traitorIcon.MoveTutorialUIElement(cameraStats, tutorialRenderer, options.traitorCountTutorialText);
            curTutorialIcon = traitorIcon;

            fadeBlack.FadeInWithSpacebar(value: 0.8f, spacebarWaitTime: 2.5f, uvPosX: 0, uvPosY: 0.5f);
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

    private void Redo()
    {
        fadeBlack.FadeInChangeScene("New Game", Scenes.SceneType.Start, sceneIndex: 1);
        SceneController.KeepNotepad(notepad);
        notepad.gameObject.SetActive(false);
    }
    private void EnterButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;

    }
    private void ExitButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
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
}
