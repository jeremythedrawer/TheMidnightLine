using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    public Bounds curLocationBounds;
    public Vector3 curWorldPos;
    public Vector2 moveVelocity;
    public float targetXVelocity;
    public float curGravityScale;
    public float coyoteTimeElapsed;
    public float maxJumpHeight;
    public float lastGroundHeight;
    public SpyBrain.State curState;
    public LayerMask curLocationLayer;
    public LayerMask curGroundLayer;
    public LayerMask curWallLayer;
    public bool spriteFlip;
    public bool isGrounded;
    public bool isStepping;
    public bool walkingIntoWall;
    public bool onTrain;

    public bool checkingNotepad;



    public string ticketName;
    public string boardingStationName;
    public string disembarkingStationName;
    public void ResetStats()
    {
        curWorldPos = Vector2.zero;
        spriteFlip = false;

        moveVelocity = Vector2.zero;
        targetXVelocity = 0.0f;
        curGravityScale = 1;

        isGrounded = false;
        onTrain = false;
    }
}
