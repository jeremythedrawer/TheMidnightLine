using UnityEngine;

public class MovementInputs : MonoBehaviour
{
    public StateCore core;
    public Transform meleeBoxColliderTransform;

    internal float walkInput;
    internal bool runInput;
    internal bool jumpInput;
    internal bool releaseJumpInput;
    internal bool crouchInput;
    internal bool meleeInput;
    internal bool shootInput;

    //Jump Controller
    private bool jumpRequested;
    private float lastJumpTime;
    private bool earlyFallEnabled;
    private float jumpBufferTime = 0.2f;

    //Set Collision Adjustment
    private float cbcSmoothing = 0.15f;
    private float cbcSmoothingCurrent;
    private float currentOffsetY = 0f;

    public bool canMove { get; set; } = true;
    public bool adjustingCollider { get; private set; } = false;
    public void MoveWithInput()
    {
        if (Mathf.Abs(walkInput) > 0) //check when moving
        {
            if (!canMove)
            {
                core.body.linearVelocityX = 0;
                return;
            }

            float newSpeed;
            float increment = walkInput;
            if (runInput)
            {
                newSpeed = Mathf.Clamp(core.body.linearVelocityX + increment, -core.characterStats.runSpeed, core.characterStats.runSpeed);
            }
            else
            {
                newSpeed = Mathf.Clamp(core.body.linearVelocityX + increment, -core.characterStats.walkSpeed, core.characterStats.walkSpeed);
            }

            core.body.linearVelocityX = newSpeed;
            FlipGameObject();
        }
        else if (!core.collisionChecker.grounded)
        {
            core.body.linearVelocityX *= 0.9f; //slow down when xInput is released 
        }
    }

    public void JumpController()
    {

        //initiate jump
        if (jumpInput)
        {
            jumpRequested = true;
            lastJumpTime = Time.time;
            earlyFallEnabled = false;
        }

        //early fall
        if (core.body.linearVelocityY > 0 && releaseJumpInput)
        {
            core.body.linearVelocityY = 0;
            earlyFallEnabled = true;
        }

        //jump logic
        if (jumpRequested && Time.time - lastJumpTime <= jumpBufferTime && !earlyFallEnabled)
        {
            if (canMove && (core.collisionChecker.grounded || core.stateList.fallState.coyoteEnabled))
            {
                core.stateList.fallState.coyoteEnabled = false;
                core.stateList.fallState.coyoteTime = core.stateList.fallState.initialCoyoteTime;
                jumpRequested = false;
                core.body.linearVelocityY = core.characterStats.jumpSpeed;
            }
        }
    }
    public void SetCollisionAdjustment()
    {
        // Correct Box Collider trigger
        if (core.collisionChecker.cbcEnabled && core.collisionChecker.characterBoxCollider.offset.y == 0 && Mathf.Abs(walkInput) > 0)
        {
            float currentOffsetY = core.collisionChecker.cbcThreshold;
            core.collisionChecker.characterBoxCollider.offset = new Vector2(0.0f, currentOffsetY); // moving collider 1 unit up
            adjustingCollider = true;
        }
        // Correct Box Collider reset
        if (core.collisionChecker.characterBoxCollider.offset.y > 0.0001)
        {
            cbcSmoothingCurrent += Time.fixedDeltaTime;

            float t = cbcSmoothingCurrent / cbcSmoothing;
            currentOffsetY = Mathf.Lerp(core.collisionChecker.characterBoxCollider.offset.y, 0.0f, t);

            if (Mathf.Abs(currentOffsetY) < 0.00001f)
            {
                adjustingCollider = false;
                currentOffsetY = 0f;
            }

            core.collisionChecker.characterBoxCollider.offset = new Vector2(0.0f, currentOffsetY);
            core.collisionChecker.cbcEnabled = false;
        }
        else
        {
            cbcSmoothingCurrent = 0.0f;
            currentOffsetY = 0f;
            core.collisionChecker.characterBoxCollider.offset = Vector2.zero;
        }
    }

    private void FlipGameObject()
    {
        // update sprite direction
        bool posDirection = Mathf.Sign(walkInput) > 0; // 0 is left, 1 is right
        core.spriteRenderer.flipX = !posDirection;

        if (core is not BystanderMovement)
        {
            Vector3 meleeBoxScale = meleeBoxColliderTransform.localScale;
            float intendedScaleX = Mathf.Abs(meleeBoxScale.x) * (posDirection ? 1: -1);
            if (meleeBoxScale.x != intendedScaleX)
            {
                meleeBoxScale.x = intendedScaleX;
                meleeBoxColliderTransform.localScale = meleeBoxScale;
            }
        }
    }



}
