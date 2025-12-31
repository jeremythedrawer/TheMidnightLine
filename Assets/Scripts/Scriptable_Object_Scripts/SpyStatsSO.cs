using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    internal Bounds curLocationBounds;

    internal Vector3 curWorldPos;

    internal Vector2 startPos;
    internal Vector2 checkpointPos;
    internal Vector2 moveVelocity;
    public Vector2 phonePosition;

    internal float targetXVelocity;
    internal float curRunSpeed;
    internal float gravityScale;
    internal float curJumpHorizontalForce;
    internal float lastJumpTime;
    internal float coyoteTimeElapsed;
    internal float curCarriageMinXPos;
    internal float curCarriageMaxXPos;

    internal LayerMask curLocationLayer;
    internal LayerMask curGroundLayer;

    internal bool spriteFlip;
    internal bool coyoteJump;
    internal bool willJump;
    internal bool isGrounded;
    internal bool isStepping;
    internal bool canHang;
    internal bool isClimbing;
    internal bool isLeftClimbBound;
    internal bool firstFixedFrameClimb;
    internal bool canBoardTrain;
    internal bool onTrain;

    public void ResetStats()
    {
        curWorldPos = Vector2.zero;
        spriteFlip = false;
        startPos = Vector2.zero;
        checkpointPos = Vector2.zero;

        moveVelocity = Vector2.zero;
        targetXVelocity = 0.0f;
        curRunSpeed = 1.0f;
        gravityScale = 1;

        curJumpHorizontalForce = 0.0f;
        willJump = false;
        lastJumpTime = float.MinValue;
        coyoteJump = false;
        coyoteTimeElapsed = 0.0f;

        isGrounded = false;
        isStepping = false;
        canHang = false;
        isClimbing = false;
        firstFixedFrameClimb = true;

        canBoardTrain = false;
        onTrain = false;
    }

}
