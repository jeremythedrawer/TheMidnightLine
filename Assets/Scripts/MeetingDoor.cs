using UnityEngine;
using static AtlasUI;
using static Spy;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static Atlas;
public class MeetingDoor : MonoBehaviour
{
    public static event Action<Vector2> OnSpyEnter;
    public static event Action OnSpyExit;

    public enum MeetingDoorType
    { 
        Start,
        BetweenRooms,
    }
    public enum Level
    {
        Zero,
        One,
    }

    public GameEventDataSO gameEventData;
    public SpyStatsSO spyStats;
    public CameraStatsSO camStats;
    public SceneData sceneData;
    public MeridiaTowerData meridiaTowerData;

    public AtlasRenderer atlasRenderer;
    public AtlasRenderer iconRenderer;

    public Room rightRoom;
    public Room leftRoom;

    public MeetingDoorType doorType;
    public MeridiaCabinetMotion motion;
    public Level level;
    public float leftDepth;
    public float rightDepth;

    public bool unlocked;
    public bool auto;

    [Header("Generated")]

    public AtlasClip clip;
    public Bounds triggerBounds;
    public Vector3 boundsOffset;

    public bool opened;
    public bool spyInBounds;
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
        gameEventData.OnInteract.RegisterListener(OpenDoor);
        gameEventData.OnInteract.RegisterListener(WalkThroughStartDoor);
        NotepadProp.OnNotepadCollect += UnlockStartDoor;
        MeridiaTower.OnArriveAtFloor += OpenArrivingAtFloor;

    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenDoor);
        gameEventData.OnInteract.UnregisterListener(WalkThroughStartDoor);

        NotepadProp.OnNotepadCollect -= UnlockStartDoor;
        MeridiaTower.OnArriveAtFloor -= OpenArrivingAtFloor;
    }
    private void Update()
    {
        if (meridiaTowerData.curLevel != level || meridiaTowerData.elevatorMoving) return;
        if (!spyInBounds)
        {
            switch (doorType)
            {
                case MeetingDoorType.Start:
                {
                    if (unlocked && spyStats.bounds.center.x > triggerBounds.min.x && spyStats.bounds.center.x < triggerBounds.max.x && !atlasRenderer.isAnimating)
                    {
                        OnSpyEnter?.Invoke(new Vector2(triggerBounds.center.x, spyStats.bounds.max.y));
                        spyInBounds = true;
                    }
                }
                break;
                case MeetingDoorType.BetweenRooms:
                {
                    if (!opened && !auto)
                    {
                        if (spyStats.bounds.center.x > triggerBounds.min.x && spyStats.bounds.center.x < triggerBounds.max.x)
                        {
                            OnSpyEnter?.Invoke(new Vector2(triggerBounds.center.x, spyStats.bounds.max.y));
                            spyInBounds = true;
                        }
                    }
                    else if (spyStats.moveVelocity.x > 0 && spyStats.bounds.center.x > triggerBounds.center.x && spyStats.bounds.center.x < triggerBounds.max.x)
                    {
                        spyStats.curLocationState = rightRoom.locationState;
                        spyStats.curLocationBounds = rightRoom.bounds;

                        SpyBrain spy = SceneController.GetSpy();
                        spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, leftDepth));

                        rightRoom.MoveDown();
                        leftRoom.MoveUp();

                        if (auto) OpenDoor();
                        
                        if (leftRoom.locationState == Spy.LocationState.Elevator)
                        {
                            gameEventData.OnFromStartMenu.Raise();
                        }


                        spyInBounds = true;
                    }
                    else if (spyStats.moveVelocity.x < 0 && spyStats.bounds.center.x > triggerBounds.min.x && spyStats.bounds.center.x < triggerBounds.center.x)
                    {
                        spyStats.curLocationState = leftRoom.locationState;
                        spyStats.curLocationBounds = leftRoom.bounds;

                        SpyBrain spy = SceneController.GetSpy();
                        spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, leftDepth));

                        rightRoom?.MoveUp();
                        leftRoom.MoveDown();

                        if (auto) OpenDoor();

                        if (leftRoom.locationState == Spy.LocationState.Elevator)
                        {
                            if (sceneData.activeSceneType == Scenes.SceneType.Start)
                            {
                                gameEventData.OnToStartMenu.Raise();
                            }
                            atlasRenderer.PlayClipOneShot(clip);
                        }

                        spyInBounds = true;
                    }
                }
                break;
            }
        }
        else
        {
            switch (doorType)
            {
                case MeetingDoorType.Start:
                {
                    if (spyStats.bounds.center.x < triggerBounds.min.x || spyStats.bounds.center.x > triggerBounds.max.x)
                    {
                        OnSpyExit?.Invoke();
                        spyInBounds = false;
                    }
                    else if (opened && !atlasRenderer.isAnimating && !spyStats.startTrip)
                    {
                        spyInBounds = false;
                    }

                }
                break;
                case MeetingDoorType.BetweenRooms:
                {
                    if (!opened)
                    {
                        if (spyStats.bounds.center.x < triggerBounds.min.x || spyStats.bounds.center.x > triggerBounds.max.x)
                        {
                            OnSpyExit?.Invoke();
                            spyInBounds = false;
                        }
                    }
                    else if (spyStats.moveVelocity.x > 0 && spyStats.bounds.center.x > triggerBounds.max.x)
                    {
                        spyStats.curLocationState = rightRoom.locationState;
                        spyStats.curLocationBounds = rightRoom.bounds;
                        rightRoom.MoveDown();
                        leftRoom.MoveUp();

                        SpyBrain spy = SceneController.GetSpy();
                        spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, rightDepth));

                        if (leftRoom.locationState == Spy.LocationState.Elevator)
                        {
                            atlasRenderer.PlayClipOneShotReverse(clip);
                            opened = false;
                        }

                        spyInBounds = false;
                    }
                    else if (spyStats.moveVelocity.x < 0 && spyStats.bounds.center.x < triggerBounds.min.x)
                    {
                        spyStats.curLocationState = leftRoom.locationState;
                        spyStats.curLocationBounds = leftRoom.bounds;

                        SpyBrain spy = SceneController.GetSpy();
                        spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, leftDepth));

                        rightRoom.MoveUp();
                        leftRoom.MoveDown();
                        if (leftRoom.locationState != Spy.LocationState.Elevator)
                        {
                            atlasRenderer.PlayClipOneShotReverse(clip);
                            opened = false;
                            leftRoom.ToggleRightWall(true);
                        }

                        spyInBounds = false;
                    }
                }
                break;
            }


        }
    }
    private void WalkThroughStartDoor()
    {
#if UNITY_EDITOR

        if (doorType == MeetingDoorType.Start && opened && spyInBounds && !spyStats.startTrip && !atlasRenderer.isAnimating)
        {
            if (skipToScore)
            {
                gameEventData.OnFinishTripScene.Raise();
            }
            else
            {
                gameEventData.OnStartTrip.Raise();
            }
            SpyBrain spy = SceneController.GetSpy();
            spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, rightDepth));
            leftRoom.MoveUp();
            spyStats.startTrip = true;
        }
#else
        if (doorType == MeetingDoorType.Start && opened && spyInBounds && !spyStats.startTrip && !atlasRenderer.isAnimating)
        {
            gameEventData.OnStartTrip.Raise();
            SpyBrain spy = SceneController.GetSpy();
            spy.SetNewPosition(new Vector3(spy.transform.position.x, spy.transform.position.y, rightDepth));
            leftRoom.MoveUp();
            spyStats.startTrip = true;
        }
#endif
    }
    public void OpenDoor()
    {
        if (!unlocked) return;

        Bounds atlasBounds = atlasRenderer.bounds;
        
        if (!opened && spyStats.bounds.max.x > atlasBounds.min.x && spyStats.bounds.min.x < atlasBounds.max.x)
        {
            atlasRenderer.PlayClipOneShot(clip);
            opened = true;
            switch(doorType)
            {
                case MeetingDoorType.Start:
                {
                    OnSpyExit?.Invoke();
                }
                break;
                case MeetingDoorType.BetweenRooms:
                {
                    leftRoom.ToggleRightWall(false);
                    OnSpyExit?.Invoke();
                }
                break;
            }
        }
    }
    public void UnlockStartDoor()
    {
        if (doorType != MeetingDoorType.Start) return;
        unlocked = true;
        iconRenderer.enabled = false;
    }
    public void OpenArrivingAtFloor(LocationState locationState)
    {
        if (!auto || rightRoom.locationState != locationState) return;

        if (!opened)
        {
            atlasRenderer.PlayClipOneShot(clip);
            unlocked = true;
            opened = true;
            level = meridiaTowerData.curLevel;
            switch (doorType)
            {
                case MeetingDoorType.Start:
                {

                }
                break;
                case MeetingDoorType.BetweenRooms:
                {
                    leftRoom.ToggleRightWall(false);
                }
                break;
            }
        }
    }
    public void SetRightRoom(Room room, Level newLevel)
    {
        rightRoom = room;
        level = newLevel;
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(MeetingDoor))]
public class MeetingDoorEditor : Editor
{
    BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    private Vector3 lastPosition;

    private void OnEnable()
    {
        lastPosition = ((MeetingDoor)target).transform.position;
    }

    private void OnSceneGUI()
    {
        MeetingDoor door = (MeetingDoor)target;
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
