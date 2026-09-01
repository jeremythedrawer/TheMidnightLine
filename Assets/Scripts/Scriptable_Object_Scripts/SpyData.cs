using System;
using UnityEngine;
using static Spy;
using static AtlasUI;

[CreateAssetMenu(fileName = "SpyData", menuName = "Data / Spy Data")]
public class SpyData : ScriptableObject
{
    public SpyBrain spy;

    [Header("Settings")]
    public float moveSpeed = 1.5f;
    public float groundAccelation = 12f;
    public float groundDecelaration = 0.5f;
    [Range(0f, 0.5f)] public float groundBufferVertical = 0.018f;
    [Range(0f, 1f)] public float groundBufferHorizontal = 0.176f;
    public float wallWidthBuffer = 0.09f;
    public float wasteBuffer = 0.35f;
    [Range(0f, 1f)] public int depthPositionInTrain = 0;

    [Header("Generated")]
    public string boardingStationName;
    public string disembarkingStationName;
    
    public Vector2 moveVelocity;
    
    public float targetXVelocity;

    public SpyState curState;

    public bool spriteFlip;
    public bool walkingIntoWall;
    public bool startTrip;
    public bool checkingNotepad;
    public bool playerInputsEnabled;
    public bool canCheckTicket;

    public Bounds bounds;
    
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
}
