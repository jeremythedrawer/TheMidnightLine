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

    public InputData playerInputs;
    public CameraData cameraStats;
    public SpyData spyStats;
    public NotepadData notepadData;
    public TripData trip;
    public Options options;
    public TrainData trainData;
    public CursorData cursorData;

    public FadeBlack fadeBlack;

    public UnlockPicker unlockPicker;
    

    public Material fadeBlackMaterial; 

    public Ticket ticket;

    public RailMap railMap;

    public AtlasRenderer carriageMap;
    public AtlasRenderer traitorIconRenderer;
    public AtlasRenderer keyIcon;

    public AtlasTextRenderer traitorCountText;
    public AtlasTextRenderer tutorialRenderer;

    [Header("Generated")]
    
    public Notepad notepad;
    
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
    public int traitorCount;

    public float naturalMoveClock;
    public float fadeBlackClock;
    
    public float resetClock;

    public bool canExitState;
    public bool uiAtNightMode;
    private void OnEnable()
    {
        TrainController.OnStationLeave += SetTraitorIcons;
        TrainController.OnFinishTripScene += HideKeyIcon;

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

        PassengerBrain.OnTraitorDisembarkedTrain += DecreaseTraitorCount;
        PassengerBrain.OnTraitorBoardedTrain += IncreaseTraitorCount;
    }
    private void OnDisable()
    {
        TrainController.OnStationLeave -= SetTraitorIcons;
        TrainController.OnFinishTripScene -= HideKeyIcon;

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
        
        PassengerBrain.OnTraitorDisembarkedTrain -= DecreaseTraitorCount;
        PassengerBrain.OnTraitorBoardedTrain -= IncreaseTraitorCount;
    }
    private void Update()
    {
        UpdateState();

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
    private void InitPOVUI()
    {
        notepad.transform.localPosition = notepadData.inactiveLocalPos;

        float halfCamWidth = cameraStats.bounds.extents.x;
        float halfCamHeight = cameraStats.bounds.extents.y;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, -halfCamHeight + ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMapActivePos.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;
    }
    private void SetTraitorIcons()
    {
        if (trainData.curStationIndex == 1)
        {
            traitorIconRenderer.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 4);
            traitorCountText.ChangeCustom(time: 0.8f, newValue: 1, customChannel: 4);
        }
    }
    private void ShowSpaceIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeyboardBindingIconIndices.Spacebar, Direction.Up);
    }
    private void ShowEIcon(Vector2 position)
    {
        ShowKeyIcon(keyIcon, position, KeyboardBindingIconIndices.E, Direction.Up);
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
    private void IncreaseTraitorCount()
    {
        traitorCount++;
        traitorCountText.SetText("x" + traitorCount);
    }
    private void DecreaseTraitorCount()
    {
        traitorCount--;
        traitorCountText.SetText("x" + traitorCount);
    }

    private void Redo()
    {
        fadeBlack.FadeInChangeScene("New Game", sceneIndex: 1);
        notepad.gameObject.SetActive(false);
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
