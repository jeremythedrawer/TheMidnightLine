using UnityEngine;
using static AtlasUI;
using static Spy;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static Atlas;
public class RoomDoor : MonoBehaviour
{
    public enum State
    {
        Closed,
        Closing,
        Opened,
        Opening,
    }
    [Flags] public enum SubState
    {
        None = 0,
        InBounds = 1 << 0,
        EnterBoundsLeft = 1 << 1,
        EnterBoundsRight = 1 << 2,
        ExitBoundsLeft = 1 << 3,
        ExitBoundsRight = 1 << 4,

        EnteredBounds = EnterBoundsLeft | EnterBoundsRight,
        ExitBounds = ExitBoundsLeft | ExitBoundsRight,
    }

    public GameEventDataSO gameEventData;
    public SpyStatsSO spyStats;
    public CameraStatsSO camStats;
    public SceneData sceneData;
    public MeridiaTowerData meridiaTowerData;

    public AtlasRenderer atlasRenderer;

    public Room rightRoom;
    public Room leftRoom;

    public MeridiaCabinetMotion motion;
    
    public int level;
    
    public float leftDepth;
    public float rightDepth;

    public bool auto;
    public bool unlocked;

    [Header("Generated")]

    public AtlasClip clip;

    public Bounds triggerBounds;

    public Vector3 boundsOffset;
    public State curState;
    public SubState curSubState;
    public SubState prevSubState;


#if UNITY_EDITOR
    [Header("Editor")]
    public bool skipToScore;
#endif
    private void Start()
    {
        clip = atlasRenderer.atlas.clipDict[(int)motion];
    }
    private void OnEnable()
    {
        gameEventData.OnInteract.RegisterListener(OpenDoorOnInteract);
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenDoorOnInteract);
    }
    private void Update()
    {
        if (meridiaTowerData.curLevel != level || !unlocked || meridiaTowerData.elevatorMoving) return;
        UpdateSubStates();
        UpdateState();
    }
    private void LateUpdate()
    {

    }
    private void SetState(State newState)
    {
        if (newState == curState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch(curState)
        {
            case State.Opened:
            {
                prevSubState = SubState.None;
                if ((curSubState & SubState.InBounds) != 0)
                {
                    curSubState |= SubState.EnteredBounds;
                }
            }
            break;
            case State.Closed:
            {

            }
            break;
            case State.Opening:
            {

            }
            break;

            case State.Closing:
            {

            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case State.Opened:
            {   
                HandleSpyPositionsAuto();
            }
            break;
            case State.Closed:
            {
                if ((curSubState & SubState.EnteredBounds) != 0)
                {
                    if (auto) OpenDoor();
                }
            }
            break;
            case State.Opening:
            {
                HandleSpyPositionsAuto();
                if (!atlasRenderer.isAnimating) SetState(State.Opened);
            }
            break;

            case State.Closing:
            {
                if (!atlasRenderer.isAnimating) SetState(State.Closed);
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case State.Opened:
            {

            }
            break;
            case State.Closed:
            {

            }
            break;
            case State.Opening:
            {

            }
            break;

            case State.Closing:
            {

            }
            break;
        }
    }
    private void UpdateSubStates()
    {
        curSubState &= ~(SubState.EnteredBounds | SubState.ExitBounds);

        if ((curSubState & SubState.InBounds) == 0 && spyStats.bounds.center.x > triggerBounds.min.x && spyStats.bounds.center.x < triggerBounds.center.x)
        {
            curSubState |= SubState.InBounds;

            if (spyStats.moveVelocity.x > 0)
            {
                curSubState |= SubState.EnterBoundsRight;
                prevSubState = SubState.EnterBoundsRight;
            }
            else if (spyStats.moveVelocity.x < 0)
            {
                curSubState |= SubState.EnterBoundsLeft;
                prevSubState = SubState.EnterBoundsLeft;
            }
        }
        else if ((curSubState & SubState.InBounds) != 0 && (spyStats.bounds.center.x < triggerBounds.min.x || spyStats.bounds.center.x > triggerBounds.center.x))
        {
            curSubState &= ~SubState.InBounds;

            if (spyStats.moveVelocity.x > 0 && prevSubState == SubState.EnterBoundsRight)
            {
                curSubState |= SubState.ExitBoundsRight;
                prevSubState = SubState.ExitBoundsRight;
            }
            else if (spyStats.moveVelocity.x < 0 && prevSubState == SubState.EnterBoundsLeft)
            {
                curSubState |= SubState.ExitBoundsLeft;
                prevSubState = SubState.ExitBoundsLeft;

            }
        }
    }
    public void WalkThroughStartDoor()
    {
        if (curState == State.Opened && (curSubState & SubState.InBounds) != 0 && !spyStats.startTrip && !atlasRenderer.isAnimating)
        {
#if UNITY_EDITOR
            if (skipToScore)
            {
                gameEventData.OnFinishTripScene.Raise();
            }
#else
            gameEventData.OnStartTrip.Raise();
#endif
            SpyBrain spy = SceneController.GetSpy();
            spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, rightDepth));
            leftRoom.MoveUp();
            spyStats.startTrip = true;
        }
    }
    public void CloseDoor(bool unlock = true)
    {
        atlasRenderer.PlayClipOneShotReverse(clip);
        SetState(State.Closing);

        unlocked = unlock;
    }
    public void OpenDoorOnInteract()
    {
        if (auto || !unlocked || curState != State.Closed || (curSubState & SubState.InBounds) == 0) return;
        OpenDoor();
    }
    public void OpenDoor()
    {
        atlasRenderer.PlayClipOneShot(clip);
        SetState(State.Opening);
    }
    public void UnlockDoor()
    {
        unlocked = true;
    }
    public void LockDoor()
    {
        unlocked = false;
    }
    private void HandleSpyPositionsAuto()
    {
        if (!auto) return;

        if ((curSubState & SubState.EnterBoundsRight) != 0)
        {
            leftRoom.ToggleRightWall(false);
        }
        else if ((curSubState & SubState.ExitBoundsRight) != 0)
        {
            if (rightRoom != null)
            {
                camStats.curLocationState = rightRoom.locationState;
                camStats.curLocationBounds = rightRoom.bounds;
            }
            SpyBrain spy = SceneController.GetSpy();
            spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, rightDepth));

            rightRoom?.MoveDown();
            leftRoom?.MoveUp();
        }
        else if ((curSubState & SubState.EnterBoundsLeft) != 0)
        {
            if (leftRoom != null)
            {
                camStats.curLocationState = leftRoom.locationState;
                camStats.curLocationBounds = leftRoom.bounds;
            }

            SpyBrain spy = SceneController.GetSpy();
            spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, leftDepth));

            rightRoom?.MoveUp();
            leftRoom?.MoveDown();

            if (motion != MeridiaCabinetMotion.Elevator)
            {
                CloseDoor();
            }
            leftRoom?.ToggleRightWall(true);
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(RoomDoor))]
public class MeetingDoorEditor : Editor
{
    BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    private Vector3 lastPosition;

    private void OnEnable()
    {
        lastPosition = ((RoomDoor)target).transform.position;
    }

    private void OnSceneGUI()
    {
        RoomDoor door = (RoomDoor)target;
        Transform t = door.transform;

        Vector3 delta = t.position - lastPosition;

        if (delta != Vector3.zero)
        {
            door.triggerBounds.center += delta;
            lastPosition = t.position;

            EditorUtility.SetDirty(door);
        }

        boundsHandle.center = door.triggerBounds.center;
        boundsHandle.size = door.triggerBounds.size;

        EditorGUI.BeginChangeCheck();

        boundsHandle.SetColor(Color.magenta);
        boundsHandle.DrawHandle();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(door, "Edit Door Trigger Bounds");

            door.triggerBounds.center = boundsHandle.center;
            door.triggerBounds.size = boundsHandle.size;

            EditorUtility.SetDirty(door);
        }
    }
}
#endif
