using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class POVUI : MonoBehaviour
{
    const float TARGET_MARGIN = 0.01f;
    const float NATURAL_RADIUS = 0.1f;
    const float NATURAL_TICK_RATE = 2.5f;
    public enum State
    { 
        None,
        Notepad,
        Ticket,
        Map,
    }

    public PlayerInputsSO playerInputs;
    public CameraStatsSO cameraStats;
    public Notepad notepad;
    public Ticket ticket;
    public Transform background;
    public float backgroundMoveDamp = 5;
    public float moveDamp = 4;

    [Header("Generated")]
    public Vector3 backgroundActivePos;
    public Vector3 backgroundInactivePos;

    public Vector3 notepadActivePos;
    public Vector3 notepadInactivePos;

    public Vector3 ticketActivePos;
    public Vector3 ticketInactivePos;

    public State curState;
    public float transitionTime;

    public float naturalMoveClock;
    public Vector3 naturalMovePos;

    public bool canExitTicket;
    public CancellationTokenSource ctsBackground;
    public CancellationTokenSource ctsUIObject;
    private void Start()
    {
        background.gameObject.SetActive(true);
        notepad.gameObject.SetActive(true);


        float halfCamWidth = cameraStats.camWidth * 0.5f;


        backgroundActivePos = background.localPosition;
        backgroundInactivePos = new Vector3(halfCamWidth, background.localPosition.y, background.localPosition.z);
        background.localPosition = backgroundInactivePos;
        
        notepadActivePos = notepad.transform.localPosition;
        notepadInactivePos = new Vector3(halfCamWidth, notepad.transform.localPosition.y - cameraStats.camWorldBottom - notepad.totalBounds.size.y, notepad.transform.localPosition.z);
        notepad.transform.localPosition = notepadInactivePos;

        ticketActivePos = ticket.transform.localPosition;
        ticketInactivePos = new Vector3(halfCamWidth, ticket.transform.localPosition.y - cameraStats.camWorldBottom - ticket.totalBounds.size.y, ticket.transform.localPosition.z);
        ticket.transform.localPosition = ticketInactivePos;

        transitionTime = -Mathf.Log(TARGET_MARGIN) / moveDamp;
    }
    private void Update()
    {
        SelectState();
        UpdateState();
    }
    private void SelectState()
    {
        if (curState != State.None) return;

        if (playerInputs.notepad.x == -1)
        {
            SetState(State.Notepad);
        }
        else if (playerInputs.ticket)
        {
            SetState(State.Ticket);
        }
        else if (playerInputs.map)
        {
            SetState(State.Map);
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

                ctsUIObject?.Cancel();
                ctsUIObject?.Dispose();
                ctsUIObject = null;

                InitNaturalPos(notepadActivePos);
            }
            break;
            case State.Ticket:
            {
                MoveBackground(backgroundActivePos);
                
                ticket.gameObject.SetActive(true);
                canExitTicket = false;

                ctsUIObject?.Cancel();
                ctsUIObject?.Dispose();
                ctsUIObject = null;

                InitNaturalPos(ticketActivePos);

            }
            break;
            case State.Map:
            {

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

                if (playerInputs.notepad.x == 1)
                {
                    SetState(State.None);
                }
            }
            break;
            case State.Ticket:
            {
                NaturalActiveMove(ticketActivePos);
                ticket.transform.localPosition = Vector3.Lerp(ticket.transform.localPosition, naturalMovePos, Time.deltaTime * moveDamp);

                if (!playerInputs.ticket) canExitTicket = true;

                if (playerInputs.ticket && canExitTicket)
                {
                    SetState(State.None);
                }
            }
            break;
            case State.Map:
            {

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
                MoveCurrentUIObject(notepad.transform, notepadInactivePos);
                notepad.enabled = false;
            }
            break;
            case State.Ticket:
            {
                MoveCurrentUIObject(ticket.transform, ticketInactivePos);
            }
            break;
            case State.Map:
            {

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
        ctsBackground?.Dispose();
        ctsBackground = null;
        ctsBackground = new CancellationTokenSource();

        Moving(background.transform, ctsBackground, nextPos, backgroundMoveDamp).Forget();
    }
    private void MoveCurrentUIObject(Transform uiObjectTransform, Vector3 nextPos)
    {
        ctsUIObject?.Cancel();
        ctsUIObject?.Dispose();
        ctsUIObject = null;
        ctsUIObject = new CancellationTokenSource();

        Moving(uiObjectTransform, ctsUIObject, nextPos, moveDamp).Forget();
    }
    private async UniTask Moving(Transform transform, CancellationTokenSource cts, Vector3 nextPos, float damp, State objState = State.None)
    {
        float elapsedTime = 0f;

        try
        {
            transform.gameObject.SetActive(true);
            while (elapsedTime < transitionTime)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, nextPos, Time.deltaTime * damp);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(cts.Token);
            }
            transform.localPosition = nextPos;
            if (curState == State.None) transform.gameObject.SetActive(false);
        }
        catch(OperationCanceledException)
        {
            transform.localPosition = nextPos;
            if (objState != State.None && curState != objState) transform.gameObject.SetActive(false);
        }
    }
}
