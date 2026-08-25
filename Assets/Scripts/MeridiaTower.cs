using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static Spy;
public class MeridiaTower : MonoBehaviour
{
    public static event Action OnArriveAtBottomFloor;

    public static event Action OnExitStartElevator;
    public static event Action OnEnterStartElevator;

    public static event Action OnScrollingUp;

    public static event Action<Vector2> OnSpyEnterTripDoor;
    public static event Action OnSpyExitTripDoor;

    public enum ScrollState
    {
        None,
        Up,
        Down,
    }

    public SpyData spyStats;
    public MeridiaTowerData meridiaTowerData;
    public CameraData camStats;
    public SceneData sceneData;
    public GameEventData gameEventData;
    public InputData playerInputs;

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
        StartUI.OnStartButtonClicked += StartButtonClicked;

        Scenes.OnLoadScore += MoveToMeetingFloor;

        SpyBrain.OnAfterOutcomeSequence += MoveDownToBetweenFloors;
        SpyBrain.OnAfterOutcomeSequence += MoveMeetingWallUp;
        SpyBrain.OnInteract += WalkThroughStartDoor;

        StartUI.OnPlayAgain += MoveToBottomFloor;

        startElevator.elevatorDoor.onDoorClose += MoveUpToBetweenFloors;

    }
    private void OnDisable()
    {
        StartUI.OnStartButtonClicked -= StartButtonClicked;

        Scenes.OnLoadScore -= MoveToMeetingFloor;


        SpyBrain.OnAfterOutcomeSequence -= MoveDownToBetweenFloors;
        SpyBrain.OnAfterOutcomeSequence -= MoveMeetingWallUp;
        SpyBrain.OnInteract -= WalkThroughStartDoor;

        StartUI.OnPlayAgain -= MoveToBottomFloor;

        startElevator.elevatorDoor.onDoorClose += MoveUpToBetweenFloors;
        
        ctsElevatorMove?.Cancel();
    }

    private void Start()
    {
        Init();
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
                OnExitStartElevator?.Invoke();
            }
            else if ((elevatorMeetingDoor.curSubState & RoomDoor.SubState.ExitBoundsLeft) != 0)
            {
                OnEnterStartElevator?.Invoke();
            }

            if ((tripElevatorDoor.curSubState & RoomDoor.SubState.EnteredBounds) != 0)
            {
                OnSpyEnterTripDoor.Invoke(new Vector2(tripElevatorDoor.atlasRenderer.bounds.center.x, tripElevatorDoor.atlasRenderer.bounds.max.y));
            }
            else if ((tripElevatorDoor.curSubState & RoomDoor.SubState.ExitBounds) != 0)
            {
                OnSpyExitTripDoor.Invoke();
            }
            
        }
    }
    
    private void Init()
    {
        startGamePosY = transform.position.y;
        camStats.curLocationBounds = elevatorRoom.bounds;
        meridiaTowerData.curLevel = 0;
        meridiaTowerData.elevatorMoving = true;
        scroll = ScrollState.Up;
    }
    public void StartButtonClicked()
    {
        if (meridiaTowerData.curLevel == 1) return;
        MoveToMeetingFloor();
    }
    public void MoveToMeetingFloor()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        startElevator.transform.SetParent(null);

        scroll = ScrollState.None;

        MovingToMeetingFloor().Forget();
    }
    public void MoveToBottomFloor()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        startElevator.transform.SetParent(null);

        scroll = ScrollState.None;

        MovingToBottomFloor().Forget();
    }
    public void MoveDownToBetweenFloors()
    {
        ctsElevatorMove?.Cancel();
        ctsElevatorMove = new CancellationTokenSource();
        startElevator.transform.SetParent(null);

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
        startElevator.transform.SetParent(null);

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
        if (playerInputs.interactKeyDown)
        {
            tripElevatorDoor.WalkThroughStartDoor();
        }
    }
    private void MoveMeetingWallUp()
    {
        meetingRoom.MoveUp();
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
            OnScrollingUp?.Invoke();
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
            startElevator.OpenElevatorDoor();
            
            meridiaTowerData.elevatorMoving = false;
            meridiaTowerData.bottomFloorCenterTopPos = new Vector2(bottomGround.bounds.center.x, bottomGround.bounds.max.y);

            OnArriveAtBottomFloor?.Invoke();
        }
        catch (OperationCanceledException)
        {

        }
    }
}
