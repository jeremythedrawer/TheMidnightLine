using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    const float TARGET_MARGIN = 0.01f;
    const float NATURAL_RADIUS = 0.1f;
    const float NATURAL_TICK_RATE = 2.5f;

    const float TICKET_ICON_PADDING = 0.2f;
    const float TICKET_ICON_APPEARING_DURATION = 0.5f;
    public enum State
    { 
        None,
        Notepad,
        Ticket,
        CarriageMap,
    }
    public PlayerInputsSO playerInputs;
    public CameraStatsSO cameraStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    public TripSO trip;
    public Notepad notepad;
    public Ticket ticket;
    public AtlasRenderer carriageMap;
    public Transform background;
    public Transform ticketIconTransform;

    public TicketIcon ticketIcon_prefab;

    public float backgroundMoveDamp = 5;
    public float moveDamp = 4;

    [Header("Generated")]
    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 notepadActivePos;
    public Vector3 notepadInactivePos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public Vector3 carriageMapActivePos;
    public Vector3 carriageMapInactivePos;

    public State curState;
    public float transitionTime;

    public float naturalMoveClock;
    public Vector3 naturalMovePos;

    public TicketIcon[] ticketIcons;
    public TicketIcon curTicketIcon;
    public int ticketCount;
    public CancellationTokenSource ctsBackground;
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsTicket;
    public CancellationTokenSource ctsCarriageMap;

    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.RegisterListener(DisappearTicketIcons);
    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.UnregisterListener(DisappearTicketIcons);
    }
    private void Start()
    {
        InitPOVUI();
        InitTicketIcons();
    }
    private void Update()
    {
        SelectState();
        UpdateState();
    }
    private void SelectState()
    {
        if (spyStats.curState == SpyBrain.State.Notepad || notepad.curState != Notepad.State.Stationary)
        {
            SetState(State.Notepad);
        }
        else if (spyStats.curState == SpyBrain.State.Ticket)
        {
            SetState(State.Ticket);
        }
        else if (spyStats.curState == SpyBrain.State.CarriageMap)
        {
            SetState(State.CarriageMap);
        }
        else
        {
            SetState(State.None);
        }
    }
    private void SetState(State newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    public void EnterState()
    {
        switch (curState)
        {
            case State.Notepad:
            {
                MoveBackground(backgroundActivePos);
                notepad.gameObject.SetActive(true);
                notepad.enabled = true;
                InitNaturalPos(notepadActivePos);
                ctsNotepad?.Cancel();
            }
            break;
            case State.Ticket:
            {
                MoveBackground(backgroundActivePos);
                
                ticket.gameObject.SetActive(true);
                InitNaturalPos(ticketActivePos);
                ticket.SetText(spyStats.boardingStationName, spyStats.disembarkingStationName);
                ctsTicket?.Cancel();

                curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];
                trip.ticketsCheckedSinceStart++;
                trip.ticketsCheckedSinceLastStation++;
                curTicketIcon.CheckTicket();
            }
            break;
            case State.CarriageMap:
            {
                MoveBackground(backgroundActivePos);

                carriageMap.gameObject.SetActive(true);
                ctsCarriageMap?.Cancel();

            }
            break;
            case State.None:
            {
                MoveBackground(backgroundInactivePos);
            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case State.Notepad:
            {
                NaturalActiveMove(notepadActivePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * moveDamp);
            }
            break;
            case State.Ticket:
            {
                NaturalActiveMove(ticketActivePos);
                ticket.transform.localPosition = Vector3.Lerp(ticket.transform.localPosition, naturalMovePos, Time.deltaTime * moveDamp);
            }
            break;
            case State.CarriageMap:
            {
                carriageMap.transform.localPosition = Vector3.Lerp(carriageMap.transform.localPosition, carriageMapActivePos, Time.deltaTime * moveDamp);
            }
            break;
            case State.None:
            {

            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case State.Notepad:
            {
                MoveNotepad(notepadInactivePos);
                notepad.enabled = false;
            }
            break;
            case State.Ticket:
            {
                MoveTicket(ticketInactivePos);

                curTicketIcon.RipStubTicket();

            }
            break;
            case State.CarriageMap:
            {
                MoveCarriageMap(carriageMapInactivePos);
            }
            break;
            case State.None:
            {

            }
            break;
        }
    }
    private void NaturalActiveMove(Vector3 activePos)
    {
        naturalMoveClock += Time.deltaTime;

        if (naturalMoveClock > NATURAL_TICK_RATE)
        {
            Vector2 xyPos = UnityEngine.Random.insideUnitCircle * NATURAL_RADIUS;
            naturalMovePos.x = activePos.x + (xyPos.x * 0.1f);
            naturalMovePos.y = activePos.y + xyPos.y;
            naturalMovePos.z = activePos.z;
            naturalMoveClock = 0;
        }
    }
    private void InitNaturalPos(Vector3 activePos)
    {
        naturalMovePos = activePos;
    }
    private void MoveBackground(Vector3 nextPos)
    {
        ctsBackground?.Cancel();
        ctsBackground = new CancellationTokenSource();

        Moving(background.transform, ctsBackground.Token, nextPos, backgroundMoveDamp).Forget();
    }
    private void MoveNotepad(Vector3 nextPos)
    {
        ctsNotepad?.Cancel();
        ctsNotepad = new CancellationTokenSource();
        Moving(notepad.transform, ctsNotepad.Token, nextPos, moveDamp).Forget();
    }
    private void MoveTicket(Vector3 nextPos)
    {
        ctsTicket?.Cancel();
        ctsTicket = new CancellationTokenSource();
        Moving(ticket.transform, ctsTicket.Token, nextPos, moveDamp).Forget();
    }
    private void MoveCarriageMap(Vector3 nextPos)
    {
        ctsCarriageMap?.Cancel();
        ctsCarriageMap = new CancellationTokenSource();
        Moving(carriageMap.transform, ctsCarriageMap.Token, nextPos, moveDamp).Forget();
    }
    private void InitPOVUI()
    {
        background.gameObject.SetActive(true);
        notepad.gameObject.SetActive(true);

        float halfCamWidth = cameraStats.worldWidth * 0.5f;

        backgroundActivePos = background.localPosition;
        backgroundInactivePos = new Vector3(halfCamWidth, background.localPosition.y, background.localPosition.z);
        background.localPosition = backgroundInactivePos;

        notepadActivePos = notepad.transform.localPosition;
        notepadInactivePos = new Vector3(halfCamWidth + notepad.boundsOffset.x, notepad.transform.localPosition.y - cameraStats.camWorldBottom - notepad.boundsOffset.y, notepad.transform.localPosition.z);
        notepad.transform.localPosition = notepadInactivePos;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, ticket.transform.localPosition.y - cameraStats.camWorldBottom - ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMap.transform.localPosition.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;

        transitionTime = -Mathf.Log(TARGET_MARGIN) / moveDamp;

        ticket.gameObject.SetActive(false);
        notepad.gameObject.SetActive(false);
        carriageMap.gameObject.SetActive(false);

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
            ticketIcons[i] = ticketIcon;
        }
        curTicketIcon = ticketIcons[0];
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
    private async UniTask SettingNewTicketIcons()
    {
        ticketCount = trip.nextStation.ticketsToCheckBeforeSpawn;
        int curTicketIconIndex = 0;

        while(curTicketIconIndex < ticketCount)
        {
            ticketIcons[curTicketIconIndex].Appear();
            curTicketIconIndex++;
            await UniTask.WaitForSeconds(TICKET_ICON_APPEARING_DURATION);
        }
    }
    private async UniTask DisappearingTicketIcons()
    {
        ticketCount = trip.nextStation.ticketsToCheckBeforeSpawn;
        int curTicketIconIndex = ticketCount - 1;

        while (curTicketIconIndex >= 0)
        {
            ticketIcons[curTicketIconIndex].Disappear();
            curTicketIconIndex--;
            await UniTask.WaitForSeconds(TICKET_ICON_APPEARING_DURATION);
        }
    }
    private async UniTask Moving(Transform transform, CancellationToken token, Vector3 nextPos, float damp)
    {
        float elapsedTime = 0f;
        transform.gameObject.SetActive(true);
        try
        {
            while (elapsedTime < transitionTime)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, nextPos, Time.deltaTime * damp);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(token);
            }
            transform.localPosition = nextPos;
            if (curState == State.None) transform.gameObject.SetActive(false);
        }
        catch(OperationCanceledException)
        {
        }
    }
}
