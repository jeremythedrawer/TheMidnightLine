using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static Spy;
public class MeridiaTower : MonoBehaviour
{
    public static event Action OnArriveAtBottomFloor;

    public static event Action OnEnterMeetingRoomFromElevator;
    public static event Action OnEnterStartElevator;

    public enum ScrollState
    {
        None,
        Up,
        Down,
    }

    public SpyStatsSO spyStats;
    public MeridiaTowerData meridiaTowerData;
    public CameraStatsSO camStats;
    public SceneData sceneData;
    public GameEventDataSO gameEventData;

    public Room elevatorRoom;
    public Room meetingRoom;
    public Room bottomRoom;

    public RoomDoor tripElevatorDoor;
    public RoomDoor elevatorMeetingDoor;
    public RoomDoor bottomElevatorDoor;

    public StartElevator startElevator;

    public AtlasRenderer[] elevatorScrollingRenderers;

    public BoxCollider2D bottomGround;

    [Header("Generated")]
    public ScrollState scroll;
    public float startGamePosY;
    public CancellationTokenSource ctsElevatorMove;
    private void OnEnable()
    {
        StartUI.OnStartButtonClicked += MoveToMeetingFloor;

        Scenes.OnLoadScore += MoveToMeetingFloor;

        NotepadProp.OnNotepadCollect += UnlockTripDoor;
        SpyBrain.OnAfterOutcomeSequence += MoveDownToBetweenFloors;
        SpyBrain.OnEnteredElevatorGoingUp += MoveUpToBetweenFloors;

        StartUI.OnPlayAgain += MoveToBottomFloor;

        gameEventData.OnInteract.RegisterListener(WalkThroughStartDoor);

    }
    private void OnDisable()
    {
        StartUI.OnStartButtonClicked -= MoveToMeetingFloor;
        Scenes.OnLoadScore -= MoveToMeetingFloor;

        SpyBrain.OnAfterOutcomeSequence -= MoveDownToBetweenFloors;
        SpyBrain.OnEnteredElevatorGoingUp -= MoveUpToBetweenFloors;

        StartUI.OnPlayAgain -= MoveToBottomFloor;

        gameEventData.OnInteract.UnregisterListener(WalkThroughStartDoor);
        
        ctsElevatorMove?.Cancel();
    }

    private void Start()
    {
        startGamePosY = transform.position.y;
        camStats.curLocationBounds = elevatorRoom.bounds;
        meridiaTowerData.curLevel = 0;
        meridiaTowerData.elevatorMoving = true;
        scroll = ScrollState.Up;
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

                startElevator.ScrollingChainsDown();
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

                startElevator.ScrollingChainsDown();

            }
            break;
        }

        if (sceneData.activeSceneType == Scenes.SceneType.Start)
        {
            if ((elevatorMeetingDoor.curSubState & RoomDoor.SubState.ExitBoundsRight) != 0)
            {
                OnEnterMeetingRoomFromElevator?.Invoke();
            }
            else if ((elevatorMeetingDoor.curSubState & RoomDoor.SubState.ExitBoundsLeft) != 0)
            {
                OnEnterStartElevator?.Invoke();
            }
        }


    }
    public void MoveToMeetingFloor()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        transform.SetParent(null);

        scroll = ScrollState.None;

        MovingToMeetingFloor().Forget();
    }
    public void MoveToBottomFloor()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        transform.SetParent(null);

        scroll = ScrollState.None;

        MovingToBottomFloor().Forget();
    }
    public void MoveDownToBetweenFloors()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        transform.SetParent(null);

        meridiaTowerData.elevatorMoving = true;
        meridiaTowerData.curLevel = 0;

        camStats.curLocationBounds = elevatorRoom.bounds;

        scroll = ScrollState.None;

        elevatorMeetingDoor.CloseDoor();

        MovingDownToBetweenFloors().Forget();
    }
    public void MoveUpToBetweenFloors()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        transform.SetParent(null);

        meridiaTowerData.elevatorMoving = true;
        meridiaTowerData.curLevel = 0;

        camStats.curLocationBounds = elevatorRoom.bounds;

        scroll = ScrollState.None;

        MovingUpToBetweenFloors().Forget();
    }
    public void UnlockTripDoor()
    {
        tripElevatorDoor.UnlockDoor();
    }
    private void WalkThroughStartDoor()
    {
        tripElevatorDoor.WalkThroughStartDoor();
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

                startElevator.ScrollingChainsUp();

                await UniTask.Yield(ctsElevatorMove.Token);
            }
            wallRend.custom.y = 1;
        }
        catch (OperationCanceledException)
        {

        }

        float startPosY = transform.position.y;
        float curPosY = startPosY;
        float targetPosY = startElevator.transform.position.y + startElevator.GetGroundBounds().size.y;
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

                startElevator.ScrollingChainsUp();

                await UniTask.Yield(ctsElevatorMove.Token);
            }
            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);
            elevatorWallRend.custom.z = Room.MOVE_DOWN_WALL_VALUE_LEVEL_ONE;

            meridiaTowerData.curLevel = 1;
            startElevator.SetRightRoom(meetingRoom);

            meridiaTowerData.elevatorMoving = false;

            elevatorMeetingDoor.OpenDoor();
            
            startElevator.OpenElevatorDoor();
        }
        catch (OperationCanceledException)
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

                startElevator.ScrollingChainsDown();

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
    private async UniTask MovingUpToBetweenFloors()
    {
        AtlasRenderer wallRend = meetingRoom.exteriorWallRenderer;
        float wallRendSpriteHeight = wallRend.sprite.worldSize.y;

        float startPosY = transform.position.y;
        float curPosY = startPosY;
        float targetPosY = startGamePosY;

        try
        {
            while (curPosY > targetPosY)
            {
                float t = Time.deltaTime * wallRendSpriteHeight;
                curPosY -= t;
                transform.position = new Vector3(transform.position.x, curPosY, transform.position.z);

                float elevatorT = Mathf.InverseLerp(startPosY, targetPosY, curPosY);

                startElevator.ScrollingChainsUp();

                await UniTask.Yield(ctsElevatorMove.Token);
            }
            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);
            scroll = ScrollState.Up;
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
                
                startElevator.ScrollingChainsDown();

                await UniTask.Yield(ctsElevatorMove.Token);
            }
            wallRend.custom.y = 0;
        }
        catch (OperationCanceledException)
        {

        }

        float curPosY = transform.position.y;
        float targetPosY = startElevator.transform.position.y - bottomRoom.transform.localPosition.y;
        try
        {
            while (curPosY < targetPosY)
            {
                float t = Time.deltaTime * wallRendSpriteHeight;
                curPosY += t;
                transform.position = new Vector3(transform.position.x, curPosY, transform.position.z);

                startElevator.ScrollingChainsDown();

                await UniTask.Yield(ctsElevatorMove.Token);
            }

            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);

            meridiaTowerData.curLevel = 0;
            startElevator.SetRightRoom(bottomRoom);
            
            bottomElevatorDoor.OpenDoor();
            
            meridiaTowerData.elevatorMoving = false;
            meridiaTowerData.bottomFloorCenterTopPos = new Vector2(bottomGround.bounds.center.x, bottomGround.bounds.max.y);

            OnArriveAtBottomFloor?.Invoke();
        }
        catch (OperationCanceledException)
        {

        }
    }
}
