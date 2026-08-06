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
    public const float MOVE_WALL_VALUE = 2f;
    public CameraStatsSO camStats;
    public LocationState locationState;
    public AtlasRenderer exteriorWallRenderer;
    public BoxCollider2D leftWallCollider;
    public BoxCollider2D rightWallCollider;
    [Header("Generated")]
    public Bounds bounds;
    public float startWallValue;
    public CancellationTokenSource ctsWall;

    private void Start()
    {
        startWallValue = exteriorWallRenderer.custom.z;
    }
    public void MoveUp()
    {
        ctsWall?.Cancel();
        ctsWall = new CancellationTokenSource();

        MovingUp().Forget();
    }
    public void MoveDown(bool toStart = false)
    {
        ctsWall?.Cancel();
        ctsWall = new CancellationTokenSource();

        MovingDown(toStart).Forget();
    }
    public void ToggleLeftWall(bool toggle)
    {
        leftWallCollider.enabled = toggle;
    }
    public void ToggleRightWall(bool toggle)
    {
        rightWallCollider.enabled = toggle;
    }
    private async UniTask MovingDown(bool toStart)
    {
        float moveWallValue = toStart ? startWallValue : MOVE_WALL_VALUE;

        float elaspedTime = (exteriorWallRenderer.custom.z / moveWallValue) * MOVE_WALL_TIME;
        try
        {
            while (elaspedTime < MOVE_WALL_TIME)
            {
                elaspedTime += Time.deltaTime;

                float t = elaspedTime / MOVE_WALL_TIME;
                t = EaseInOutCubic(t);

                exteriorWallRenderer.custom.z = t * moveWallValue;

                await UniTask.Yield(ctsWall.Token);
            }
            exteriorWallRenderer.custom.z = moveWallValue;
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingUp()
    {
        float elaspedTime = (exteriorWallRenderer.custom.z / MOVE_WALL_VALUE) * MOVE_WALL_TIME;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                float t = elaspedTime / MOVE_WALL_TIME;
                t = EaseInOutCubic(t);

                exteriorWallRenderer.custom.z = t * MOVE_WALL_VALUE;

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
