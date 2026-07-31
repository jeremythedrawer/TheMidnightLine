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


    public PlayerInputsSO playerInputs;
    public CameraStatsSO cameraStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    public NotepadData notepadData;
    public TripSO trip;
    public OptionsSO options;
    public TrainStatsSO trainStats;

    public FadeBlack fadeBlack;

    public SceneData sceneData;

    public Material fadeBlackMaterial; 

    public Notepad notepad;

    public Ticket ticket;

    public TicketIcon ticketIconPrefab;

    public AtlasRenderer carriageMap;

    public IconElement traitorIcon;
    public IconElement redoButton;
    public IconElement quitButton;
    
    public AtlasRenderer keyIcon;

    public AtlasTextRenderer traitorCountText;
    public AtlasTextRenderer tutorialText;

    public Transform ticketIconTransform;

    [Header("Generated")]
    public TicketIcon[] ticketIcons;
    
    public TicketIcon curTicketIcon;
    
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsTicket;
    public CancellationTokenSource ctsCarriageMap;
    public CancellationTokenSource ctsFadeBlack;

    public IconElement curTutorialIcon;

    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public Vector3 carriageMapActivePos;
    public Vector3 carriageMapInactivePos;

    public Vector3 naturalMovePos;

    public UIState curState;
    public UnlockType curUnlockType;
    public TutorialState curTutorialState;

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
        gameEventData.OnFinishTripScene.RegisterListener(SetFadeToBlack);

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

    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetNewTicketIcons);
        gameEventData.OnStationLeave.UnregisterListener(SetTraitorIcons);

        gameEventData.OnStationArrival.UnregisterListener(DisappearTicketIcons);
        gameEventData.OnFinishTripScene.UnregisterListener(SetFadeToBlack);


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

        Scenes.OnLoadTrip0 -= Init;
    }
    private void Update()
    {
        UpdateState();
        fadeBlack.CheckToFadeFromBlack();
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
                naturalMovePos = notepadData.activeLocalPos;
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
                UpdateNaturalPos(notepadData.activeLocalPos, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);

                if (playerInputs.notepadKeyDown && canExitState)
                {
                    notepadData.checkingNotepad = false;
                }

                if (playerInputs.notepadKeyUp)
                {
                    canExitState = true;
                }


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
                switch(curTutorialState)
                {
                    case TutorialState.None:
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
                            notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.inactiveLocalPos, Time.deltaTime * MOVE_DAMP);
                            notepad.activePage.InvertExitButton(invert: false);
                        }
                    }
                    break;

                    case TutorialState.Ticket:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            MoveBackTutorialUIElement(curTutorialIcon);
                            spyStats.tutorialState |= curTutorialState;

                            curTicketIcon.RipStubTicket();
                            curTicketIcon.InvertIcon(toggle: false);
                            curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];

                            curTutorialState = TutorialState.None;
                        }
                    }
                    break;

                    case TutorialState.Traitor:
                    {
                        if (canExitState && playerInputs.spacebarDown)
                        {
                            MoveBackTutorialUIElement(curTutorialIcon);
                            traitorIcon.renderer.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 1);
                            traitorCountText.ChangeCustom(time: 0.8f, newValue: 0, customChannel: 1);
                            spyStats.tutorialState |= curTutorialState;

                            curTutorialState = TutorialState.None;
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

                if (!options.skipTutorial && ((spyStats.tutorialState & TutorialState.Ticket) == 0))
                {
                    MoveTutorialUIElement(curTicketIcon.mainTicket, options.ticketCountTutorialText);
                    curTutorialState = TutorialState.Ticket;
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
        traitorIcon.startPos = traitorIcon.renderer.transform.localPosition;
        
        traitorIcon.renderer.SetAlpha(0);
        traitorCountText.SetAppearTextAlpha(0);

        quitButton.startPos = quitButton.renderer.transform.localPosition;
        redoButton.startPos = redoButton.renderer.transform.localPosition;
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
        if (curTutorialState != TutorialState.Ticket)
        {
            curTicketIcon?.InvertIcon(toggle: false);
        }
    }
    private void SetFadeToBlack()
    {
        fadeBlack.FadeToBlackChangeScene("Results", Scenes.SceneType.Score, sceneIndex: 3);
    }
    private void ShowWIcon(Vector2 position)
    {
        ShowKeyIcon(position, KeySpriteIndices.W);
    }
    private void ShowEIcon(Vector2 position)
    {
        if (trip.ticketsCheckedTotal == 0)
        {
            ShowKeyIcon(position, KeySpriteIndices.E);
        }
    }
    private void ShowKeyIcon(Vector2 position, KeySpriteIndices keySpriteIndex)
    {
        keyIcon.transform.SetParent(null);
        keyIcon.custom.w = 1;
        keyIcon.UpdateSpriteInputsByIndex((int)keySpriteIndex);
        keyIcon.transform.position = new Vector3(position.x, position.y + keyIcon.bounds.size.y, keyIcon.transform.position.z);
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
    private void IncreaseTraitorCount()
    {
        traitorCount++;
        traitorCountText.SetText("x" + traitorCount);

        if (!options.skipTutorial && ((spyStats.tutorialState & TutorialState.Traitor) == 0))
        {
            MoveTutorialUIElement(traitorIcon, options.traitorCountTutorialText);
            curTutorialState = TutorialState.Traitor;

            traitorIcon.renderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);
            traitorCountText.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 1);
            
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
                fadeBlack.FadeToBlackChangeScene("Find where the Traitors are going.", Scenes.SceneType.Trip, sceneIndex: 2);
                gameEventData.OnReset.Raise();
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
    private void MoveTutorialUIElement(IconElement uiIcon, string text)
    {
        uiIcon.ctsMove?.Cancel();
        uiIcon.ctsMove = new CancellationTokenSource();

        curTutorialIcon = uiIcon;

        MovingTutorialUIElement(uiIcon, text).Forget();
        fadeBlack.FadeToBlack(0.8f);
    }
    private void MoveBackTutorialUIElement(IconElement uiElement)
    {
        uiElement.ctsMove?.Cancel();
        uiElement.ctsMove = new CancellationTokenSource();
        MovingBackTutorialUIElement(uiElement).Forget();
        fadeBlack.FadeFromBlack();
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
    private async UniTask MovingTutorialUIElement(IconElement uiElement, string text)
    {
        AtlasRenderer iconRenderer = uiElement.renderer;
        Transform iconTransform = iconRenderer.transform;
        
        Vector2 targetPos = new Vector2();
        targetPos.x = 0;

        float localPivotPosY = iconRenderer.bounds.size.y * iconRenderer.sprite.uvPivot.y;
        targetPos.y = cameraStats.camBounds.extents.y - localPivotPosY - UI_POSITION_BUFFER;

        Vector2 curPos = new Vector2();
        curPos.x = iconTransform.localPosition.x;
        curPos.y = iconTransform.localPosition.y;

        try
        {
            while ((curPos - targetPos).sqrMagnitude > 0.05f)
            {
                curPos = Vector2.Lerp(curPos, targetPos, Time.deltaTime * 2);
                iconTransform.localPosition = new Vector3(curPos.x, curPos.y, 1);
                await UniTask.Yield(uiElement.ctsMove.Token);
            }
            float tutTextLocaPosY = iconTransform.localPosition.y - localPivotPosY - tutorialText.background_renderer.worldPivotsAndSizes[8].w - 0.1f;
            tutorialText.transform.localPosition = new Vector3(iconTransform.localPosition.x, tutTextLocaPosY, 1);
            tutorialText.SetText(text);
        }
        catch(OperationCanceledException)
        {

        }
    }
    private async UniTask MovingBackTutorialUIElement(IconElement uiElement)
    {
        Transform iconTransform = uiElement.renderer.transform;

        Vector3 curPos = iconTransform.localPosition;

        try
        {
            tutorialText.SetText("");
            while ((curPos - uiElement.startPos).sqrMagnitude > 0.01f)
            {
                curPos = Vector3.Lerp(curPos, uiElement.startPos, Time.deltaTime * 2);

                iconTransform.localPosition = curPos;
                await UniTask.Yield(uiElement.ctsMove.Token);
            }
            iconTransform.localPosition = uiElement.startPos;
        }
        catch (OperationCanceledException)
        {

        }
    }
}
