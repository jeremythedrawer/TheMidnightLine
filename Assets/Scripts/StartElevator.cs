using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static MeridiaTower;
using static Spy;

public class StartElevator : MonoBehaviour
{
    public MeridiaTowerData meridiaTowerData;
    public CameraStatsSO camStats;

    public BoxCollider2D elevatorGround;
    
    public AtlasRenderer[] elevatorChains;

    public Room elevatorRoom;
    public RoomDoor elevatorDoor;
    private void Start()
    {
        transform.SetParent(null);
    }
    private void OnEnable()
    {
        SpyBrain.OnEnteredElevatorGoingUp += CloseElevatorDoor;
        SpyBrain.OnAfterOutcomeSequence += CloseElevatorDoor;
    }
    private void OnDisable()
    {
        SpyBrain.OnEnteredElevatorGoingUp -= CloseElevatorDoor;
        SpyBrain.OnAfterOutcomeSequence -= CloseElevatorDoor;
    }
    public void ScrollingChainsUp()
    {
        for (int i = 0; i < elevatorChains.Length; i++)
        {
            AtlasRenderer rend = elevatorChains[i];
            rend.custom.y += Time.deltaTime / rend.sprite.worldSize.y;
            if (rend.custom.y >= 1) rend.custom.y = 0;
        }
    }
    public void ScrollingChainsDown()
    {
        for (int i = 0; i < elevatorChains.Length; i++)
        {
            AtlasRenderer rend = elevatorChains[i];
            rend.custom.y -= Time.deltaTime / rend.sprite.worldSize.y;
            if (rend.custom.y <= -1) rend.custom.y = 0;
        }
    }
    public void SetRightRoom(Room room)
    {
        elevatorDoor.rightRoom = room;
        elevatorDoor.level = meridiaTowerData.curLevel;
    }
    public void OpenElevatorDoor()
    {
        elevatorDoor.OpenDoor();
    }
    public void CloseElevatorDoor()
    {
        elevatorDoor.CloseDoor();
    }
    public Bounds GetGroundBounds()
    {
        return elevatorGround.bounds;
    }
}
