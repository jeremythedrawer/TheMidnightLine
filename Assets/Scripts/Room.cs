using UnityEngine;
using static Spy;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif
public class Room : MonoBehaviour
{
    public CameraStatsSO camStats;
    public LocationState locationState;

    [Header("Generated")]
    public Bounds bounds;

    public void Start()
    {
        switch (locationState)
        { 
            case LocationState.MeetingRoom:
            {
                camStats.meetingBounds = bounds;
            }
            break;

            case LocationState.Bunker:
            {
                camStats.bunkerBounds = bounds;
            }
            break;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    BoxBoundsHandle boundsHandle = new BoxBoundsHandle();
    private void OnSceneGUI()
    {
        Room room = (Room)target;

        if (Selection.activeGameObject == room.gameObject)
        {
            EditorGUI.BeginChangeCheck();

            boundsHandle.size = room.bounds.size;
            boundsHandle.center = room.transform.position;
        }
        boundsHandle.SetColor(Color.orange);
        boundsHandle.DrawHandle();


        if (EditorGUI.EndChangeCheck())
        {

            Undo.RecordObject(room, "Resize Bounds");
            
            room.bounds.size = boundsHandle.size;
            room.bounds.center = boundsHandle.center;
        }
    }
}
#endif
