using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Spy;
using static AtlasUI;
public class GameplayUI : MonoBehaviour
{
    const float TICKET_ICON_PADDING = 0.2f;
    const float APPEARING_TIME = 0.5f;
    const float ABILITY_ICON_APPEAR_TIME = 1f;


    public PlayerInputsSO playerInputs;
    public CameraStatsSO cameraStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    public NotepadData notepadData;
    public TripSO trip;

    public SceneData sceneData;

    public Material fadeBlackMaterial; 

    public Notepad notepad;

    public Ticket ticket;

    public TicketIcon ticketIcon_prefab;

    public AtlasRenderer carriageMap;
    public AtlasRenderer ruleOutAbilityIcon;
    public AtlasRenderer colorAbilityIcon;
    public AtlasRenderer multiColorAbilityIcon;
    public AtlasRenderer keyIcon;

    public Transform ticketIconTransform;

    [Header("Generated")]
    public TicketIcon[] ticketIcons;
    
    public TicketIcon curTicketIcon;
    
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsTicket;
    public CancellationTokenSource ctsCarriageMap;
    public CancellationTokenSource ctsFadeBlack;

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
    
    public float naturalMoveClock;
    public float fadeBlackClock;

    public bool canExitState;

    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.RegisterListener(DisappearTicketIcons);
        gameEventData.OnFinishTripScene.RegisterListener(SetFadeToBlack);
        gameEventData.OnFinishTripScene.RegisterListener(KeepNotepad);

        SpyBrain.OnTicketCheckHoverDisabled += RevertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverDisabled += HideKeyIcon;
        SpyBrain.OnTicketCheckHoverEnabled += InvertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverEnabledFirstTime += ShowEIcon;
        SpyBrain.OnFoundExteriorSlideDoors += ShowWIcon;
        SpyBrain.OnWalkPastExteriorSlideDoors += HideKeyIcon;
        SpyBrain.OnEnteredTrain += DisappearKeyIcon;
        SpyBrain.OnTicketInspect += DisappearKeyIcon;

        UnlockPicker.OnNewAbilityUnlocked += AppearNewAbilityIcon;

        Scenes.OnLoadTrip0 += Init;

    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.UnregisterListener(DisappearTicketIcons);
        gameEventData.OnFinishTripScene.UnregisterListener(SetFadeToBlack);
        gameEventData.OnFinishTripScene.UnregisterListener(KeepNotepad);

        SpyBrain.OnTicketCheckHoverDisabled -= RevertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverDisabled -= HideKeyIcon;
        SpyBrain.OnTicketCheckHoverEnabled -= InvertCurTicketIcon;
        SpyBrain.OnFoundExteriorSlideDoors -= ShowWIcon;
        SpyBrain.OnTicketCheckHoverEnabledFirstTime -= ShowEIcon;
        SpyBrain.OnWalkPastExteriorSlideDoors -= HideKeyIcon;
        SpyBrain.OnEnteredTrain -= DisappearKeyIcon;
        SpyBrain.OnTicketInspect -= DisappearKeyIcon;
        
        UnlockPicker.OnNewAbilityUnlocked -= AppearNewAbilityIcon;
        
        Scenes.OnLoadTrip0 -= Init;
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void Init()
    {
        InitPOVUI();
        InitTicketIcons();
        InitAbiltiyIcons();
        FadeFromBlack();
    }
    private void KeepNotepad()
    {
        SceneController.KeepNotepad(notepad);
    }
    private void ChooseState()
    {
        if (playerInputs.notepadKeyDown || notepadData.checkingNotepad)
        {
            SetState(UIState.Notepad);
        }
        else if (spyStats.curState == SpyState.TicketCheck)
        {
            SetState(UIState.Ticket);
        }
        else if (spyStats.curState == SpyState.CarriageMap)
        {
            SetState(UIState.CarriageMap);
        }
        else
        {
            SetState(UIState.None);
        }
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
                naturalMovePos = NotepadActiveLocalPos;
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
                UpdateNaturalPos(NotepadActiveLocalPos, ref naturalMovePos);
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
                if (CursorController.IsInsideBounds(notepad.activePage.paperRenderer.bounds, isClickable: true))
                {
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
                MoveUIElement(notepad.transform, NotepadInactiveLocalPos, ref ctsNotepad, newState);
                notepad.ExitNotepad();
                SceneController.GetColorPicker().Close();
            }
            break;
            case UIState.Ticket:
            {
                MoveUIElement(ticket.transform, ticketInactivePos, ref ctsTicket, newState);

                curTicketIcon.RipStubTicket();
                curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];

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
    private void InitPOVUI()
    {
        float halfCamWidth = cameraStats.camBounds.extents.x;
        float halfCamHeight = cameraStats.camBounds.extents.y;

        notepad.transform.localPosition = NotepadInactiveLocalPos;

        notepadData.checkingNotepad = false;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, -halfCamHeight + ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMap.transform.localPosition.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;
    }
    private void InitTicketIcons()
    {
        ticketIcons = new TicketIcon[8];

        float ticketIconSpacing = ticketIcon_prefab.mainTicket.bounds.size.x + ticketIcon_prefab.stubTicket.bounds.size.x + TICKET_ICON_PADDING;
        for (int i = 0; i < ticketIcons.Length; i++)
        {
            float xPos = ticketIconTransform.position.x + (ticketIconSpacing * i);
            Vector3 pos = new Vector3(xPos, ticketIconTransform.position.y, ticketIconTransform.position.z);
            TicketIcon ticketIcon = Instantiate(ticketIcon_prefab, pos, Quaternion.identity, ticketIconTransform);
            ticketIcon.Init();
            ticketIcon.name = "TicketIcon" + i;
            ticketIcons[i] = ticketIcon;
        }
        curTicketIcon = ticketIcons[0];
    }
    private void InitAbiltiyIcons()
    {
        ruleOutAbilityIcon.custom.w = 1;
        colorAbilityIcon.custom.w = 1;
        multiColorAbilityIcon.custom.w = 1;
    }
    private void SetNewTicketIcons()
    {
        curTicketIcon = ticketIcons[0];
        SettingNewTicketIcons().Forget();
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

        curTicketIcon?.InvertIcon(toggle: false);
    }
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
    private void SetFadeToBlack()
    {
        SetFadeBlack(fadeBlackMaterial, toFadeBlack: true);
        Scenes.SetScene(sceneData, Scenes.SceneType.Score, sceneIndex: 3);
    }
    private void AppearNewAbilityIcon(UnlockType unlockType)
    {
        switch(unlockType)
        {
            case UnlockType.RuleOut:
            {
                Appearing(ruleOutAbilityIcon).Forget();
            }
            break;
            case UnlockType.Color:
            {
                Appearing(colorAbilityIcon).Forget();
            }
            break;
            case UnlockType.MultiColor:
            {
                Appearing(multiColorAbilityIcon).Forget();
            }
            break;
        }
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
        keyIcon.custom.w = 0;
        keyIcon.UpdateSpriteInputsByIndex((int)keySpriteIndex);
        keyIcon.transform.position = new Vector3(position.x, position.y + keyIcon.bounds.size.y, keyIcon.transform.position.z);
    }
    private void HideKeyIcon()
    {
        keyIcon.custom.w = 1;
    }
    private void DisappearKeyIcon()
    {
        if (keyIcon.custom.w == 0)
        {
            DisappearingKeyIcon().Forget();
        }
    }
    private async UniTask DisappearingKeyIcon()
    {
        float elapsedTime = 0;

        while(elapsedTime < APPEARING_TIME)
        {
            elapsedTime += Time.deltaTime;
            keyIcon.custom.w = elapsedTime / APPEARING_TIME;
            await UniTask.Yield();
        }
        keyIcon.custom.w = 1;
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
    private async UniTask Appearing(AtlasRenderer renderer)
    {
        float elapsed = ABILITY_ICON_APPEAR_TIME;

        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;
            float t = elapsed / ABILITY_ICON_APPEAR_TIME;
            renderer.custom.w = t;
            await UniTask.Yield();
        }
        renderer.custom.w = 0;
    }
}
