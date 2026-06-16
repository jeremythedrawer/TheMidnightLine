using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Spy;
using static AtlasUI;
using UnityEngine.SceneManagement;
public class GameplayUI : MonoBehaviour
{
    const float TICKET_ICON_PADDING = 0.2f;
    const float TICKET_ICON_APPEARING_DURATION = 0.5f;

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
    public AtlasRenderer carriageMap;
    public Transform ticketIconTransform;

    public TicketIcon ticketIcon_prefab;


    [Header("Generated")]
    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public Vector3 carriageMapActivePos;
    public Vector3 carriageMapInactivePos;

    public Vector3 naturalMovePos;

    public Bounds notepadHoverBounds;

    public UIState curState;
    public float naturalMoveClock;
    public float fadeBlackClock;

    public TicketIcon[] ticketIcons;
    public TicketIcon curTicketIcon;
    public int ticketCount;
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsTicket;
    public CancellationTokenSource ctsCarriageMap;
    public CancellationTokenSource ctsFadeBlack;
    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.RegisterListener(DisappearTicketIcons);
        gameEventData.OnChangeToScoreScene.RegisterListener(SetFadeToBlack);
        gameEventData.OnChangeToScoreScene.RegisterListener(KeepNotepad);
    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.UnregisterListener(DisappearTicketIcons);
        gameEventData.OnChangeToScoreScene.UnregisterListener(SetFadeToBlack);
        gameEventData.OnChangeToScoreScene.UnregisterListener(KeepNotepad);
    }
    private void Start()
    {
        InitPOVUI();
        InitTicketIcons();
        FadeFromBlack();
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void ChooseState()
    {
        if (spyStats.curState == SpyState.Notepad || notepadData.curState != NotepadState.Stationary)
        {
            SetState(UIState.Notepad);
        }
        else if (SpyBrain.checkingTicket)
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
        ExitState();
        curState = newState;
        EnterState();
    }
    public void EnterState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                notepad.gameObject.SetActive(true);
                notepad.enabled = true;
                naturalMovePos = NotepadActivePos;
                ctsNotepad?.Cancel();
            }
            break;
            case UIState.Ticket:
            {
                
                ticket.gameObject.SetActive(true);
                naturalMovePos = ticketActivePos;
                ticket.SetText(spyStats.boardingStationName, spyStats.disembarkingStationName);
                ctsTicket?.Cancel();

                curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];
                curTicketIcon.CheckTicket();
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
                UpdateNaturalPos(NotepadActivePos, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
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
                notepadHoverBounds.center = notepad.transform.position;
                if (CursorController.IsInsideBounds(notepadHoverBounds))
                {
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NnotepadHoverPos, Time.deltaTime * MOVE_DAMP);
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
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NotepadInactivePos, Time.deltaTime * MOVE_DAMP);
                }
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case UIState.Notepad:
            {
                MoveUIElement(notepad, NotepadInactivePos, ref ctsNotepad, curState);
                notepad.enabled = false;
            }
            break;
            case UIState.Ticket:
            {
                MoveUIElement(ticket, ticketInactivePos, ref ctsTicket, curState);

                curTicketIcon.RipStubTicket();

            }
            break;
            case UIState.CarriageMap:
            {
                MoveUIElement(carriageMap, carriageMapInactivePos, ref ctsCarriageMap, curState);
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
        notepad.gameObject.SetActive(true);

        float halfCamWidth = cameraStats.camBounds.extents.x;
        float halfCamHeight = cameraStats.camBounds.extents.y;

        NotepadActivePos = notepad.transform.localPosition;
        float binderBoundsOffsetX = notepad.bindingRings_renderer.bounds.max.x - notepad.transform.position.x;
        float binderBoundsOffsetY = notepad.transform.position.y - notepad.activePage.exitButton_renderer.bounds.min.y;

        NotepadInactivePos = new Vector3(halfCamWidth - binderBoundsOffsetX, -halfCamHeight + binderBoundsOffsetY, notepad.transform.localPosition.z);
        NnotepadHoverPos = new Vector3(NotepadInactivePos.x, NotepadInactivePos.y + notepad.bindingRings_renderer.bounds.size.y, NotepadInactivePos.z);

        float ySize = notepad.traitorPage_prefab.paperRenderer.bounds.size.y;
        Vector3 hoverSize = new Vector3(notepad.bindingRings_renderer.bounds.size.x, ySize, 0.2f);
        notepadHoverBounds = new Bounds(NnotepadHoverPos, hoverSize);
        notepad.transform.localPosition = NotepadInactivePos;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, -halfCamHeight + ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        carriageMapActivePos = carriageMap.transform.localPosition;
        carriageMapInactivePos = new Vector3(halfCamWidth, carriageMap.transform.localPosition.y, carriageMap.transform.localPosition.z);
        carriageMap.transform.localPosition = carriageMapInactivePos;

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
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
    private void SetFadeToBlack()
    {
        SetFadeBlack(fadeBlackMaterial, toFadeBlack: true);
        Scenes.SetScoreScene(sceneData);
    }
    public void KeepNotepad()
    {
        SceneController.KeepNotepad(notepad);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellowNice;

        Gizmos.DrawWireCube(notepadHoverBounds.center, notepadHoverBounds.size);
    }
}
