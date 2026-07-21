using UnityEngine;
using static Spy;
using static Curves;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

public class Room : MonoBehaviour
{
    const float MOVE_WALL_TIME = 0.8f;
    public CameraStatsSO camStats;
    public LocationState locationState;
    public AtlasRenderer exteriorWallRenderer;
    [Header("Generated")]
    public Bounds bounds;
    public float curMoveWallTime;
    public CancellationTokenSource ctsWall;
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

    public void MoveUp()
    {
        ctsWall?.Cancel();
        ctsWall = new CancellationTokenSource();

        MovingUp().Forget();
    }

    public void MoveDown()
    {
        ctsWall?.Cancel();
        ctsWall = new CancellationTokenSource();

        MovingDown().Forget();
    }

    private async UniTask MovingDown()
    {
        float elaspedTime = curMoveWallTime * MOVE_WALL_TIME;
        try
        {
            while (elaspedTime < MOVE_WALL_TIME)
            {
                elaspedTime += Time.deltaTime;

                curMoveWallTime = elaspedTime / MOVE_WALL_TIME;
                curMoveWallTime = EaseInOutCubic(curMoveWallTime);

                exteriorWallRenderer.custom.x = curMoveWallTime;

                await UniTask.Yield(cancellationToken: ctsWall.Token);
            }
            exteriorWallRenderer.custom.x = 1;
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingUp()
    {
        float elaspedTime = curMoveWallTime * MOVE_WALL_TIME;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                curMoveWallTime = elaspedTime / MOVE_WALL_TIME;
                curMoveWallTime = EaseInOutCubic(curMoveWallTime);

                exteriorWallRenderer.custom.x = curMoveWallTime;

                await UniTask.Yield(PlayerLoopTiming.Update, ctsWall.Token);
            }

            exteriorWallRenderer.custom.x = 0;
        }
        catch (OperationCanceledException)
        {
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
