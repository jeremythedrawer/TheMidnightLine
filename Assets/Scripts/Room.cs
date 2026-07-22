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
    public BoxCollider2D leftWallCollider;
    public BoxCollider2D rightWallCollider;

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
    public void ToggleLeftWall(bool toggle)
    {
        leftWallCollider.enabled = toggle;
    }
    public void ToggleRightWall(bool toggle)
    {
        rightWallCollider.enabled = toggle;
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

    private Vector3 lastPosition;

    private void OnEnable()
    {
        lastPosition = ((Room)target).transform.position;
    }

    private void OnSceneGUI()
    {
        Room room = (Room)target;
        Transform t = room.transform;

        Vector3 delta = t.position - lastPosition;

        if (delta != Vector3.zero)
        {
            room.bounds.center += delta;
            lastPosition = t.position;
            EditorUtility.SetDirty(room);
        }

        boundsHandle.center = room.bounds.center;
        boundsHandle.size = room.bounds.size;

        EditorGUI.BeginChangeCheck();

        boundsHandle.SetColor(Color.orange);
        boundsHandle.DrawHandle();


        if (EditorGUI.EndChangeCheck())
        {

            Undo.RecordObject(room, "Resize Room Bounds");
            
            room.bounds.center = boundsHandle.center;
            room.bounds.size = boundsHandle.size;
            EditorUtility.SetDirty(room);
        }
    }
}
#endif
