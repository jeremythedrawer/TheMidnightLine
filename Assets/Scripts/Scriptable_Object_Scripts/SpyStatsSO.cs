using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    internal int curHealth = 100;

    internal Vector2 curWorldPos;
    internal bool spriteFlip;
    internal Vector2 startPos;
    internal Vector2 checkpointPos;

    internal Vector2 moveVelocity;
    internal float targetXVelocity;
    internal float curRunSpeed;
    internal float gravityScale;

    internal float curJumpHorizontalForce;
    internal bool willJump;
    internal float lastJumpTime;
    internal bool coyoteJump;
    internal float coyoteTimeElapsed;

    internal bool isGrounded;
    internal bool isStepping;
    internal bool canHang;
    internal bool isClimbing;
    internal bool isLeftClimbBound;
    internal bool firstFixedFrameClimb;

    internal bool canBoardTrain;
    internal bool onTrain;
    internal Bounds curLocationBounds;
    internal LayerMask curLocationLayer;
    internal LayerMask curGroundLayer;

    public void ResetStats()
    {
        curHealth = 100;

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
