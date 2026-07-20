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
        Bunker,
    }

    public GameEventDataSO gameEventData;
    public SpyStatsSO spyStats;
    public CameraStatsSO camStats;

    public AtlasRenderer atlasRenderer;
    public AtlasRenderer bunkerWallRenderer;
    public SpyBrain spy;

    public MeetingDoorType doorType;


    [Header("Generated")]
    public bool opened;
    public bool spyInBounds;
    public AtlasClip clip;
    public Bounds triggerBounds;
    private void Start()
    {
        clip = atlasRenderer.atlas.clipDict[(int)MeridiaCabinetMotion.Door];
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
            if (spyStats.curWorldPos.x > triggerBounds.min.x && spyStats.curWorldPos.x < triggerBounds.max.x)
            {
                switch (doorType)
                {
                    case MeetingDoorType.Start:
                    {
                        if (spyStats.moveVelocity.x > 0)
                        {
                            gameEventData.OnStartGame.Raise();
                        }
                    }
                    break;
                    case MeetingDoorType.Bunker:
                    {
                        if (spyStats.moveVelocity.x > 0)
                        {
                            spy.transform.position = new Vector3(spy.transform.position.x, spy.transform.position.y, 11);
                        }
                        else
                        {
                            spy.transform.position = new Vector3(spy.transform.position.x, spy.transform.position.y, 2);
                        }
                    }
                    break;
                }

                spyInBounds = true;
            }
        }
        else if (spyStats.curWorldPos.x < triggerBounds.min.x || spyStats.curWorldPos.x > triggerBounds.max.x)
        {
            spyInBounds = false;
        }
    }
    public void OpenDoor()
    {
        float spyXPos = spyStats.curWorldPos.x;
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
                case MeetingDoorType.Bunker:
                {
                    spyStats.curLocationState = Spy.LocationState.Bunker;
                    spyStats.curLocationBounds = camStats.bunkerBounds;
                    bunkerWallRenderer.boxCollider.enabled = false;
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

    private void OnSceneGUI()
    {
        MeetingDoor door = (MeetingDoor)target;

        if (Selection.activeGameObject == door.gameObject)
        {
            EditorGUI.BeginChangeCheck();
            boundsHandle.size = door.triggerBounds.size;
            boundsHandle.center = door.transform.position;
        }

        boundsHandle.SetColor(Color.magenta);
        boundsHandle.DrawHandle();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(door, "Resize Bounds");
            door.triggerBounds.size = boundsHandle.size;
            door.triggerBounds.center = boundsHandle.center;
        }
    }
}
#endif