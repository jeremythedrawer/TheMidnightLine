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
    const float NOTEPAD_INACTIVE_OFFSET = 1.8f;
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

    public Transform ticketIconTransform;


    [Header("Generated")]
    public TicketIcon[] ticketIcons;
    
    public TicketIcon curTicketIcon;
    
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsTicket;
    public CancellationTokenSource ctsCarriageMap;
    public CancellationTokenSource ctsFadeBlack;

    public ColorPicker colorPicker;

    public Bounds notepadHoverBounds;

    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public Vector3 carriageMapActivePos;
    public Vector3 carriageMapInactivePos;

    public Vector3 naturalMovePos;

    public UIState curState;

    public int ticketCount;
    
    public float naturalMoveClock;
    public float fadeBlackClock;
    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.RegisterListener(DisappearTicketIcons);
        gameEventData.OnChangeToScoreScene.RegisterListener(SetFadeToBlack);
        gameEventData.OnChangeToScoreScene.RegisterListener(KeepNotepad);

        SpyBrain.OnTicketCheckHoverDisabled += RevertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverEnabled += InvertCurTicketIcon;
        UnlockPicker.OnNewAbilityUnlocked += AppearNewAbilityIcon;
    }
    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetNewTicketIcons);
        gameEventData.OnStationArrival.UnregisterListener(DisappearTicketIcons);
        gameEventData.OnChangeToScoreScene.UnregisterListener(SetFadeToBlack);
        gameEventData.OnChangeToScoreScene.UnregisterListener(KeepNotepad);

        SpyBrain.OnTicketCheckHoverDisabled -= RevertCurTicketIcon;
        SpyBrain.OnTicketCheckHoverEnabled -= InvertCurTicketIcon;
        UnlockPicker.OnNewAbilityUnlocked -= AppearNewAbilityIcon;
    }
    private void Start()
    {
        InitPOVUI();
        InitTicketIcons();
        InitAbiltiyIcons();
        FadeFromBlack();
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void KeepNotepad()
    {
        SceneController.KeepNotepad(notepad);
    }
    private void ChooseState()
    {
        if (spyStats.curState == SpyState.Notepad || notepadData.curState != NotepadState.Stationary)
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
        switch (curState)
        {
            case UIState.Notepad:
            {
                notepad.gameObject.SetActive(true);
                notepad.enabled = true;
                notepad.EnterNotepad();
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
                if (CursorController.IsInsideBounds(notepad.activePage.exitButton_renderer.bounds))
                {
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NotepadHoverPos, Time.deltaTime * MOVE_DAMP);

                    notepad.activePage.InvertExitButton(invert: true);
                    if (playerInputs.mouseLeftUp)
                    {
                        notepad.activePage.InvertExitButton(invert: false);
                        SpyBrain.ToggleNotepad(true);
                    }
                }
                else
                {
                    notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, NotepadInactivePos, Time.deltaTime * MOVE_DAMP);
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
                MoveUIElement(notepad, NotepadInactivePos, ref ctsNotepad, newState);
                notepad.ExitNotepad();
                colorPicker.Close();
            }
            break;
            case UIState.Ticket:
            {
                MoveUIElement(ticket, ticketInactivePos, ref ctsTicket, newState);

                curTicketIcon.RipStubTicket();
                curTicketIcon = ticketIcons[trip.ticketsCheckedSinceLastStation];

            }
            break;
            case UIState.CarriageMap:
            {
                MoveUIElement(carriageMap, carriageMapInactivePos, ref ctsCarriageMap, newState);
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
        float binderBoundsOffsetY = notepad.transform.position.y - NOTEPAD_INACTIVE_OFFSET;

        NotepadInactivePos = new Vector3(halfCamWidth - binderBoundsOffsetX, -halfCamHeight + binderBoundsOffsetY, notepad.transform.localPosition.z);
        NotepadHoverPos = new Vector3(NotepadInactivePos.x, NotepadInactivePos.y + notepad.bindingRings_renderer.bounds.size.y, NotepadInactivePos.z);

        float ySize = notepad.traitorPage_prefab.paperRenderer.bounds.size.y;
        Vector3 hoverSize = new Vector3(notepad.bindingRings_renderer.bounds.size.x, ySize, 0.2f);
        notepadHoverBounds = new Bounds(NotepadHoverPos, hoverSize);
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

        colorPicker = SceneController.GetColorPicker();
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
        curTicketIcon.InvertIcon(toggle: false);
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
    private async UniTask SettingNewTicketIcons()
    {
        ticketCount = trip.stationAhead.ticketsToCheckBeforeSpawn;
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
        ticketCount = trip.stationAhead.ticketsToCheckBeforeSpawn;
        int curTicketIconIndex = ticketCount - 1;

        while (curTicketIconIndex >= 0)
        {
            ticketIcons[curTicketIconIndex].Disappear();
            curTicketIconIndex--;
            await UniTask.WaitForSeconds(TICKET_ICON_APPEARING_DURATION);
        }
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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellowNice;

        Gizmos.DrawWireCube(notepadHoverBounds.center, notepadHoverBounds.size);
    }
}
