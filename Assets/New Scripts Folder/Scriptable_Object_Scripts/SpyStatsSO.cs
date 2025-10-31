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
    internal bool isGrounded;
    internal bool coyoteJump;
    internal float coyoteTimeElapsed;

    internal bool canBoardTrain;
    internal bool inCarriage;
}
