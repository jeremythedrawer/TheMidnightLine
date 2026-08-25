using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

using static Spy;
using static Curves;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

public class Room : MonoBehaviour
{
    public const float MOVE_DOWN_WALL_VALUE_LEVEL_ONE = 1.5f;
    public const float MOVE_DOWN_WALL_VALUE_LEVEL_ZERO = 5.5f;

    const float MOVE_WALL_TIME = 0.8f;

    public CameraData camStats;
    public MeridiaTowerData meridiaTowerData;

    public AtlasRenderer exteriorWallRenderer;
    
    public BoxCollider2D leftWallCollider;
    public BoxCollider2D rightWallCollider;
    
    public LocationState locationState;
    [Header("Generated")]
    public Bounds bounds;
    public CancellationTokenSource ctsWall;
    public float curMoveDownWallValue;
    public void MoveUp()
    {
        if (exteriorWallRenderer == null) return;
        ctsWall?.Cancel();
        ctsWall = new CancellationTokenSource();

        MovingUp().Forget();
    }
    public void MoveDown()
    {
        if (exteriorWallRenderer == null) return;
        ctsWall?.Cancel();
        ctsWall = new CancellationTokenSource();

        switch(meridiaTowerData.curLevel)
        {
            case 0:
            {
                curMoveDownWallValue = MOVE_DOWN_WALL_VALUE_LEVEL_ZERO;
            }
            break;
            case 1:
            {
                curMoveDownWallValue = MOVE_DOWN_WALL_VALUE_LEVEL_ONE;
            }
            break;
        }

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
        float elaspedTime = (exteriorWallRenderer.custom.z / curMoveDownWallValue) * MOVE_WALL_TIME;
        try
        {
            while (elaspedTime < MOVE_WALL_TIME)
            {
                elaspedTime += Time.deltaTime;

                float t = elaspedTime / MOVE_WALL_TIME;
                t = EaseInOutCubic(t);

                exteriorWallRenderer.custom.z = t * curMoveDownWallValue;

                await UniTask.Yield(ctsWall.Token);
            }
            exteriorWallRenderer.custom.z = curMoveDownWallValue;
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingUp()
    {
        float elaspedTime = (exteriorWallRenderer.custom.z / MOVE_DOWN_WALL_VALUE_LEVEL_ONE) * MOVE_WALL_TIME;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                float t = elaspedTime / MOVE_WALL_TIME;
                t = EaseInOutCubic(t);

                exteriorWallRenderer.custom.z = t * MOVE_DOWN_WALL_VALUE_LEVEL_ONE;

                await UniTask.Yield(ctsWall.Token);
            }

            exteriorWallRenderer.custom.z = 0;
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
