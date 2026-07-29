using System;
using UnityEngine;
using static Spy;
using static AtlasUI;

[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    public string boardingStationName;
    public string disembarkingStationName;

    public Bounds curLocationBounds;
    
    public Vector2 moveVelocity;
    
    public float targetXVelocity;
    public float spyHeight;

    public SpyState curState;
    public LocationState curLocationState;

    public TutorialState tutorialState;

    public bool spriteFlip;
    public bool walkingIntoWall;

    [Header("To Save")]
    public Vector3 curWorldPos;
    
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
}
