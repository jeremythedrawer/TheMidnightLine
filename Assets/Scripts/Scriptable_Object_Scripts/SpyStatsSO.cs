using System;
using UnityEngine;
using static Spy;
[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    public Vector3 curWorldPos;
    public Vector2 moveVelocity;
    public float targetXVelocity;
    public SpyState curState;
    public LocationState curLocationState;
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
    public Bounds curLocationBounds;

    public bool spriteFlip;
    public bool walkingIntoWall;

    public bool checkingNotepad;

    public string ticketName;
    public string boardingStationName;
    public string disembarkingStationName;
}
