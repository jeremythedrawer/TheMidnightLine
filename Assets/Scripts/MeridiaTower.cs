using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static Spy;
public class MeridiaTower : MonoBehaviour
{
    public enum ScrollState
    {
        None,
        Up,
        Down,
    }

    public static event Action<LocationState> OnArriveAtFloor;

    public SpyStatsSO spyStats;
    public MeridiaTowerData meridiaTowerData;

    public Room elevatorRoom;
    public Room meetingRoom;
    public Room bottomRoom;

    public MeetingDoor elevatorDoor;

    public AtlasRenderer[] elevatorScrollingRenderers;
    public AtlasRenderer[] elevatorChains;

    public Transform elevatorTransform;
    
    public BoxCollider2D elevatorGround;
    public BoxCollider2D bottomGround;

    [Header("Generated")]
    public ScrollState scroll;
    public CancellationTokenSource ctsElevatorMove;
    public float startGamePosY;
    private void Start()
    {
        startGamePosY = transform.position.y;
        spyStats.curLocationBounds = elevatorRoom.bounds;
        meridiaTowerData.curLevel = MeetingDoor.Level.Zero;
        meridiaTowerData.elevatorMoving = true;
        scroll = ScrollState.Up;
    }
    private void OnEnable()
    {
        StartUI.OnStartGame += MoveToMeetingFloor;

        Scenes.OnLoadScore += MoveToMeetingFloor;
        
        SpyBrain.OnAfterOutcomeSequence += MoveDownToBetweenFloors;

        StartUI.OnPlayAgain += MoveToBottomFloor;

    }
    private void OnDisable()
    {
        StartUI.OnStartGame -= MoveToMeetingFloor;
        Scenes.OnLoadScore -= MoveToMeetingFloor;

        SpyBrain.OnAfterOutcomeSequence -= MoveDownToBetweenFloors;
        
        StartUI.OnPlayAgain -= MoveToBottomFloor;

        ctsElevatorMove?.Cancel();
    }
    private void Update()
    {
        switch (scroll)
        {
            case ScrollState.Up:
            {
                for (int i = 0; i < elevatorScrollingRenderers.Length; i++)
                {
                    AtlasRenderer rend = elevatorScrollingRenderers[i];
                    rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y >= 1) rend.custom.y = 0;
                }

                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y >= 1) rend.custom.y = 0;
                }
            }
            break;

            case ScrollState.Down:
            {
                for (int i = 0; i < elevatorScrollingRenderers.Length; i++)
                {
                    AtlasRenderer rend = elevatorScrollingRenderers[i];
                    rend.custom.y -= Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y <= -1) rend.custom.y = 0;
                }

                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y -= Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y <= -1) rend.custom.y = 0;
                }
            }
            break;
        }
    }
    private void MoveToMeetingFloor()
    {
        scroll = ScrollState.None;

        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        elevatorTransform.SetParent(null);

        MovingToMeetingFloor().Forget();
    }
    private void MoveToBottomFloor()
    {
        scroll = ScrollState.None;

        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        elevatorTransform.SetParent(null);

        MovingToBottomFloor().Forget();
    }

    private void MoveDownToBetweenFloors()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        elevatorTransform.SetParent(null);

        meridiaTowerData.elevatorMoving = true;
        meridiaTowerData.curLevel = MeetingDoor.Level.Zero;

        meetingRoom.MoveUp();
        spyStats.curLocationBounds = elevatorRoom.bounds;

        MovingDownToBetweenFloors().Forget();
    }
    private async UniTask MovingToMeetingFloor()
    {
        AtlasRenderer wallRend = meetingRoom.exteriorWallRenderer;
        float wallRendSpriteHeight = wallRend.sprite.worldSize.y;

        try
        {
            while (wallRend.custom.y < 1)
            {
                wallRend.custom.y += Time.deltaTime;
                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y >= 1) rend.custom.y = 0;
                }
                await UniTask.Yield(ctsElevatorMove.Token);
            }
            wallRend.custom.y = 1;
        }
        catch(OperationCanceledException)
        {

        }

        float startPosY = transform.position.y;
        float curPosY = startPosY;
        float targetPosY = elevatorTransform.position.y + elevatorGround.bounds.size.y;
        AtlasRenderer elevatorWallRend = elevatorRoom.exteriorWallRenderer;
        float startElevatorZ = elevatorWallRend.custom.z;
        try
        {
            while (curPosY > targetPosY)
            {
                float t = Time.deltaTime * wallRendSpriteHeight;
                curPosY -= t;
                transform.position = new Vector3(transform.position.x, curPosY, transform.position.z);

                float elevatorT = Mathf.InverseLerp(startPosY, targetPosY, curPosY);
                elevatorWallRend.custom.z = Mathf.Lerp(startElevatorZ, Room.MOVE_DOWN_WALL_VALUE_LEVEL_ONE, elevatorT);

                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y >= 1) rend.custom.y = 0;
                }

                await UniTask.Yield(ctsElevatorMove.Token);
            }
            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);
            elevatorWallRend.custom.z = Room.MOVE_DOWN_WALL_VALUE_LEVEL_ONE;
            elevatorDoor.SetRightRoom(meetingRoom, MeetingDoor.Level.One);
            meridiaTowerData.curLevel = MeetingDoor.Level.One;
            meridiaTowerData.elevatorMoving = false;
            OnArriveAtFloor?.Invoke(LocationState.MeetingRoom);
        }
        catch(OperationCanceledException)
        {

        }

    }
    private async UniTask MovingDownToBetweenFloors()
    {
        AtlasRenderer wallRend = meetingRoom.exteriorWallRenderer;
        float wallRendSpriteHeight = wallRend.sprite.worldSize.y;

        float startPosY = transform.position.y;
        float curPosY = startPosY;
        float targetPosY = startGamePosY;

        AtlasRenderer elevatorWallRend = elevatorRoom.exteriorWallRenderer;
        float startElevatorZ = elevatorWallRend.custom.z;

        try
        {
            while (curPosY < targetPosY)
            {
                float t = Time.deltaTime * wallRendSpriteHeight;
                curPosY += t;
                transform.position = new Vector3(transform.position.x, curPosY, transform.position.z);

                float elevatorT = Mathf.InverseLerp(startPosY, targetPosY, curPosY);
                elevatorWallRend.custom.z = Mathf.Lerp(startElevatorZ, Room.MOVE_DOWN_WALL_VALUE_LEVEL_ZERO, elevatorT);

                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y -= Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y <= -1) rend.custom.y = 0;
                }
                await UniTask.Yield(ctsElevatorMove.Token);
            }
            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);
            elevatorWallRend.custom.z = Room.MOVE_DOWN_WALL_VALUE_LEVEL_ZERO;
            scroll = ScrollState.Down;
        }
        catch (OperationCanceledException)
        {

        }
    }
    private async UniTask MovingToBottomFloor()
    {
        AtlasRenderer wallRend = meetingRoom.exteriorWallRenderer;
        float wallRendSpriteHeight = wallRend.sprite.worldSize.y;

        try
        {
            while (wallRend.custom.y > -1)
            {
                wallRend.custom.y -= Time.deltaTime;
                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y -= Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y <= -1) rend.custom.y = 0;
                }
                await UniTask.Yield(ctsElevatorMove.Token);
            }
            wallRend.custom.y = 0;
        }
        catch (OperationCanceledException)
        {

        }

        float curPosY = transform.position.y;
        float targetPosY = elevatorTransform.position.y - bottomRoom.transform.localPosition.y;
        try
        {
            while (curPosY < targetPosY)
            {
                float t = Time.deltaTime * wallRendSpriteHeight;
                curPosY += t;
                transform.position = new Vector3(transform.position.x, curPosY, transform.position.z);

                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y -= Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y <= -1) rend.custom.y = 0;
                }
                await UniTask.Yield(ctsElevatorMove.Token);
            }
            
            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);
            
            elevatorDoor.SetRightRoom(bottomRoom, MeetingDoor.Level.Zero);
            
            meridiaTowerData.curLevel = MeetingDoor.Level.Zero;
            meridiaTowerData.elevatorMoving = false;
            meridiaTowerData.bottomFloorCenterTopPos = new Vector2(bottomGround.bounds.center.x, bottomGround.bounds.max.y);
            
            OnArriveAtFloor?.Invoke(LocationState.BottomFloor);
        }
        catch (OperationCanceledException)
        {

        }
    }
}
