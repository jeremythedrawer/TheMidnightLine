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

    public SpyState curState;
    public LocationState curLocationState;

    public TutorialState tutorialsCompleted;
    public TutorialState curTutorialState;

    public bool spriteFlip;
    public bool walkingIntoWall;
    public bool startTrip;

    public bool checkingNotepad;
    public bool playerInputsEnabled;
    public Bounds bounds;
    
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
}
