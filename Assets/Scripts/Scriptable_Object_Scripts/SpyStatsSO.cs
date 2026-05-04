using System;
using UnityEngine;
using static Spy;
[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    public Vector2 moveVelocity;
    public float targetXVelocity;
    public SpyState curState;
    public LocationState curLocationState;
    public Bounds curLocationBounds;

    public bool spriteFlip;
    public bool walkingIntoWall;

    public string ticketName;
    public string boardingStationName;
    public string disembarkingStationName;


    [Header("To Save")]
    public Vector3 curWorldPos;
    public int ticketsCheckedTotal;
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
}
