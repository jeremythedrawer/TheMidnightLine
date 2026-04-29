using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Spy;

public class GameplayUI : MonoBehaviour
{
    const float TARGET_MARGIN = 0.01f;
    const float NATURAL_RADIUS = 0.1f;
    const float NATURAL_TICK_RATE = 2.5f;

    const float TICKET_ICON_PADDING = 0.2f;
    const float TICKET_ICON_APPEARING_DURATION = 0.5f;

    const float FOCUS_TRANISTION_DURATION = 1f;
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

    public Material matrix_material; 

    public Notepad notepad;
    public Ticket ticket;
    public AtlasRenderer carriageMap;
    public Transform ticketIconTransform;

    public TicketIcon ticketIcon_prefab;

    public float backgroundMoveDamp = 5;
    public float moveDamp = 4;

    [Header("Generated")]
    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 notepadActivePos;
    public Vector3 notepadInactivePos;
    public Vector3 notepadHoverPos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public Vector3 carriageMapActivePos;
    public Vector3 carriageMapInactivePos;

    public Bounds notepadHoverBounds;

    public State curState;
    public float transitionTime;

    public float naturalMoveClock;

    public float focusTime;

    public Vector3 naturalMovePos;

    public TicketIcon[] ticketIcons;
    public TicketIcon curTicketIcon;
    public int ticketCount;
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
        ChooseState();
        UpdateState();
    }
    private void ChooseState()
    {
        if (spyStats.curState == SpyState.Notepad || notepad.curState != Notepad.State.Stationary)
        {
            SetState(State.Notepad);
        }
        else if (spyStats.curState == SpyState.Ticket)
        {
            SetState(State.Ticket);
        }
        else if (spyStats.curState == SpyState.CarriageMap)
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
                notepad.gameObject.SetActive(true);
                notepad.enabled = true;
                InitNaturalPos(notepadActivePos);
                ctsNotepad?.Cancel();
            }
            break;
            case State.Ticket:
            {
                
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

                carriageMap.gameObject.SetActive(true);
                ctsCarriageMap?.Cancel();

            }
            break;
            case State.None:
            {
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

                if (focusTime < FOCUS_TRANISTION_DURATION)
                {
                    focusTime += Time.deltaTime;
                    float t = focusTime / FOCUS_TRANISTION_DURATION;

                    matrix_material.SetFloat("_Focus", t);
                }
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
                notepadHoverBounds.center = notepad.transform.position;
                if (CursorController.IsInsideBounds(notepadHoverBounds))
                {
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadHoverPos, Time.deltaTime * moveDamp);
                    if (CursorController.IsInsideBounds(notepad.activePage.exitButton_renderer.bounds))
                    {
                        if (playerInputs.mouseLeftDown)
                        {
                            SpyBrain.ToggleNotepad(true);
                            notepad.activePage.InvertExitButton(invert: false, pointDown: false);
                        }
                        else
                        {
                            notepad.activePage.InvertExitButton(invert: true, pointDown: false);
                        }
                    }
                    else
                    {

                        notepad.activePage.InvertExitButton(invert: false, pointDown: true);
                    }
                }
                else
                {
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadInactivePos, Time.deltaTime * moveDamp);
                }

                if (focusTime > 0)
                {
                    focusTime -= Time.deltaTime;
                    float t = focusTime / FOCUS_TRANISTION_DURATION;

                    matrix_material.SetFloat("_Focus", t);
                }
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
    private void MoveNotepad(Vector3 nextPos)
    {
        ctsNotepad?.Cancel();
        ctsNotepad = new CancellationTokenSource();
        Moving(notepad, ctsNotepad.Token, nextPos, moveDamp).Forget();
    }
    private void MoveTicket(Vector3 nextPos)
    {
        ctsTicket?.Cancel();
        ctsTicket = new CancellationTokenSource();
        Moving(ticket, ctsTicket.Token, nextPos, moveDamp).Forget();
    }
    private void MoveCarriageMap(Vector3 nextPos)
    {
        ctsCarriageMap?.Cancel();
        ctsCarriageMap = new CancellationTokenSource();
        Moving(carriageMap, ctsCarriageMap.Token, nextPos, moveDamp).Forget();
    }
    private void InitPOVUI()
    {
        notepad.gameObject.SetActive(true);

        float halfCamWidth = cameraStats.camBounds.extents.x;
        float halfCamHeight = cameraStats.camBounds.extents.y;

        notepadActivePos = notepad.transform.localPosition;
        float binderBoundsOffsetX = notepad.bindingRings_renderer.bounds.max.x - notepad.transform.position.x;
        float binderBoundsOffsetY = notepad.transform.position.y - notepad.bindingRings_renderer.bounds.min.y;
        notepadInactivePos = new Vector3(halfCamWidth - binderBoundsOffsetX, -halfCamHeight + binderBoundsOffsetY, notepad.transform.localPosition.z);
        notepadHoverPos = new Vector3(notepadInactivePos.x, notepadInactivePos.y + notepad.bindingRings_renderer.bounds.size.y, notepadInactivePos.z);
        float ySize = notepad.page_prefab.paper_renderer.bounds.size.y;
        Vector3 hoverSize = new Vector3(notepad.bindingRings_renderer.bounds.size.x, ySize, 0.2f);
        notepadHoverBounds = new Bounds(notepadHoverPos, hoverSize);
        notepad.transform.localPosition = notepadInactivePos;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, -halfCamHeight + ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMap.transform.localPosition.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;

        transitionTime = -Mathf.Log(TARGET_MARGIN) / moveDamp;

        notepad.enabled = false;
        ticket.gameObject.SetActive(false);
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
        SpyBrain.ToggleTicketCheckAbility(toggle: false);
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

        SpyBrain.ToggleTicketCheckAbility(toggle: true);
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
    private async UniTask Moving(Behaviour behaviour, CancellationToken token, Vector3 nextPos, float damp)
    {
        float elapsedTime = 0f;

        behaviour.enabled = true;
        try
        {
            while (elapsedTime < transitionTime)
            {
                behaviour.transform.localPosition = Vector3.Lerp(behaviour.transform.localPosition, nextPos, Time.deltaTime * damp);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(token);
            }
            behaviour.transform.localPosition = nextPos;
            if (curState == State.None) behaviour.enabled = false;
        }
        catch(OperationCanceledException)
        {
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellowNice;

        Gizmos.DrawWireCube(notepadHoverBounds.center, notepadHoverBounds.size);
    }
}
