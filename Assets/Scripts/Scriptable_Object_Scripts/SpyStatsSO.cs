using System;
using UnityEngine;
using static Spy;
using static AtlasUI;

[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    public string boardingStationName;
    public string disembarkingStationName;
    
    public Vector2 moveVelocity;
    public Vector2 startPos;
    
    public float targetXVelocity;

    public SpyState curState;

    public TutorialState tutorialsCompleted;
    public TutorialState curTutorialState;

    public bool spriteFlip;
    public bool walkingIntoWall;
    public bool startTrip;
    public bool checkingNotepad;
    public bool playerInputsEnabled;
    public bool canCheckTicket;

    public bool movedFirstTime;

    public Bounds bounds;
    
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
}
