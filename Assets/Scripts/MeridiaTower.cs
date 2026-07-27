using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class MeridiaTower : MonoBehaviour
{
    public static event Action OnArriveAtTopFloor;

    public SpyStatsSO spyStats;
    public Room elevatorRoom;
    public Room meetingRoom;
    public AtlasRenderer[] elevatorScrollingRenderers;
    public AtlasRenderer[] elevatorChains;
    public Transform elevator;
    public BoxCollider2D elevatorGround;

    [Header("Generated")]
    public bool scroll;
    public CancellationTokenSource ctsTopFloor;

    private void Start()
    {
        spyStats.curLocationBounds = elevatorRoom.bounds;
        scroll = true;
    }
    private void OnEnable()
    {
        StartUI.OnStartGame += MoveToTopFloor;
    }
    private void OnDisable()
    {
        StartUI.OnStartGame -= MoveToTopFloor;
        ctsTopFloor?.Cancel();
    }
    private void Update()
    {
        if (scroll)
        {
            for (int i = 0; i < elevatorScrollingRenderers.Length; i++)
            {
                AtlasRenderer rend = elevatorScrollingRenderers[i];
                rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                if (rend.custom.y >= 1) rend.custom.y = 0;
            }

            for (int i = 0; i < elevatorChains.Length; i++)
            {
                AtlasRenderer rend = elevatorScrollingRenderers[i];
                rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                if (rend.custom.y >= 1) rend.custom.y = 0;
            }
        }
    }
    private void MoveToTopFloor()
    {
        scroll = false;

        ctsTopFloor?.Cancel();
        ctsTopFloor = new CancellationTokenSource();
        elevator.SetParent(null);

        MovingToTopFloor().Forget();
    }
    private async UniTask MovingToTopFloor()
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
                await UniTask.Yield(ctsTopFloor.Token);
            }
            wallRend.custom.y = 1;
        }
        catch(OperationCanceledException)
        {

        }

        float startPosY = transform.position.y;
        float curPosY = startPosY;
        float targetPosY = elevator.position.y + elevatorGround.bounds.size.y;
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
                elevatorWallRend.custom.z = Mathf.Lerp(startElevatorZ, Room.MOVE_WALL_VALUE, elevatorT);


                for (int i = 0; i < elevatorChains.Length; i++)
                {
                    AtlasRenderer rend = elevatorChains[i];
                    rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
                    if (rend.custom.y >= 1) rend.custom.y = 0;
                }

                await UniTask.Yield(ctsTopFloor.Token);
            }
            transform.position = new Vector3(transform.position.x, targetPosY, transform.position.z);
            elevatorWallRend.custom.z = Room.MOVE_WALL_VALUE;
            OnArriveAtTopFloor?.Invoke();
        }
        catch(OperationCanceledException)
        {

        }

    }
}
