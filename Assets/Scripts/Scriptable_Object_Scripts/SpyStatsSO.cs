using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpyStats_SO", menuName = "Midnight Line SOs / Spy Stats SO")]
public class SpyStatsSO : ScriptableObject
{
    public string spyID;
    public Bounds curLocationBounds;
    public Vector3 curWorldPos;
    public Vector2 startPos;
    public Vector2 checkpointPos;
    public Vector2 moveVelocity;
    public Vector3 phonePosition;
    public float targetXVelocity;
    public float curRunSpeed;
    public float gravityScale;
    public float curJumpHorizontalForce;
    public float lastJumpTime;
    public float coyoteTimeElapsed;
    public float curCarriageMinXPos;
    public float curCarriageMaxXPos;
    public LayerMask curLocationLayer;
    public LayerMask curGroundLayer;
    public bool spriteFlip;
    public bool coyoteJump;
    public bool willJump;
    public bool isGrounded;
    public bool isStepping;
    public bool canHang;
    public bool isClimbing;
    public bool isLeftClimbBound;
    public bool firstFixedFrameClimb;
    public bool canBoardTrain;
    public bool onTrain;
    public bool onPhone;
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
