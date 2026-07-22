using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static Atlas;
public class MeetingDoor : MonoBehaviour
{
    public enum MeetingDoorType
    { 
        Start,
        BetweenRooms,
    }
    
    public GameEventDataSO gameEventData;
    public SpyStatsSO spyStats;
    public CameraStatsSO camStats;

    public AtlasRenderer atlasRenderer;
    public AtlasRenderer iconRenderer;

    public Room rightRoom;
    public Room leftRoom;

    public MeetingDoorType doorType;
    public MeridiaCabinetMotion motion;

    public float leftDepth;
    public float rightDepth;

    public bool unlocked;

    [Header("Generated")]

    public AtlasClip clip;
    public Bounds triggerBounds;
    public Vector3 boundsOffset;

    public bool opened;
    public bool spyInBounds;
    private void Start()
    {
        clip = atlasRenderer.atlas.clipDict[(int)motion];
    }
    private void OnEnable()
    {
        gameEventData.OnInteract.RegisterListener(OpenDoor);    
    }
    private void OnDisable()
    {
        gameEventData.OnInteract.UnregisterListener(OpenDoor);
    }
    private void Update()
    {
        if (!opened) return;

        if (!spyInBounds)
        {
            switch (doorType)
            {
                case MeetingDoorType.Start:
                {
                    if (spyStats.moveVelocity.x > 0 && spyStats.curWorldPos.x > triggerBounds.center.x && spyStats.curWorldPos.x < triggerBounds.max.x)
                    {
                        gameEventData.OnStartGame.Raise();
                        SceneController.Spy.transform.position = new Vector3(SceneController.Spy.transform.position.x, SceneController.Spy.transform.position.y, 11);
                        rightRoom.MoveUp();

                        spyInBounds = true;
                    }
                }
                break;
                case MeetingDoorType.BetweenRooms:
                {
                    Transform spyTransform = SceneController.Spy.transform;

                    if (spyStats.moveVelocity.x > 0 && spyStats.curWorldPos.x > triggerBounds.center.x && spyStats.curWorldPos.x < triggerBounds.max.x)
                    {
                        spyStats.curLocationState = Spy.LocationState.Bunker;
                        spyStats.curLocationBounds = camStats.bunkerBounds;
                        rightRoom.MoveDown();
                        leftRoom.MoveUp();

                        spyTransform.position = new Vector3(spyTransform.position.x, spyTransform.position.y, rightDepth);
                        spyInBounds = true;
                    }
                    else if (spyStats.moveVelocity.x < 0 && spyStats.curWorldPos.x > triggerBounds.min.x && spyStats.curWorldPos.x < triggerBounds.center.x)
                    {
                        spyStats.curLocationState = Spy.LocationState.MeetingRoom;
                        spyStats.curLocationBounds = camStats.meetingBounds;

                        spyTransform.position = new Vector3(spyTransform.position.x, spyTransform.position.y, leftDepth);

                        rightRoom.MoveUp();
                        leftRoom.MoveDown();

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
                    if (spyStats.moveVelocity.x > 0 && spyStats.curWorldPos.x > triggerBounds.max.x)
                    {
                        gameEventData.OnStartGame.Raise();
                        SceneController.Spy.transform.position = new Vector3(SceneController.Spy.transform.position.x, SceneController.Spy.transform.position.y, 11);
                        rightRoom.MoveUp();
                        spyInBounds = false;
                    }
                }
                break;
                case MeetingDoorType.BetweenRooms:
                {
                    Transform spyTransform = SceneController.Spy.transform;

                    if (spyStats.moveVelocity.x > 0 && spyStats.curWorldPos.x > triggerBounds.max.x)
                    {
                        spyStats.curLocationState = Spy.LocationState.Bunker;
                        spyStats.curLocationBounds = camStats.bunkerBounds;
                        rightRoom.MoveDown();
                        leftRoom.MoveUp();

                        spyTransform.position = new Vector3(spyTransform.position.x, spyTransform.position.y, rightDepth);
                        
                        spyInBounds = false;
                    }
                    else if (spyStats.moveVelocity.x < 0 && spyStats.curWorldPos.x < triggerBounds.min.x)
                    {
                        spyStats.curLocationState = Spy.LocationState.MeetingRoom;
                        spyStats.curLocationBounds = camStats.meetingBounds;

                        spyTransform.position = new Vector3(spyTransform.position.x, spyTransform.position.y, leftDepth);

                        rightRoom.MoveUp();
                        leftRoom.MoveDown();

                        atlasRenderer.PlayClipOneShotReverse(clip);
                        spyInBounds = false;
                        opened = false;
                        leftRoom.ToggleRightWall(true);
                    }
                }
                break;
            }


        }
    }
    public void OpenDoor()
    {
        if (!unlocked) return;

        Bounds atlasBounds = atlasRenderer.bounds;
        
        if (!opened && spyStats.curWorldPos.x > atlasBounds.min.x && spyStats.curWorldPos.x < atlasBounds.max.x)
        {
            atlasRenderer.PlayClipOneShot(clip);
            opened = true;
            switch(doorType)
            {
                case MeetingDoorType.Start:
                {
                    gameEventData.OnStartGame.Raise();
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
