using System.Collections;
using UnityEngine;

public class PlayerMovement : StateCore
{
    //child states
    public AirborneState airborneState;
    public GroundState groundState;

    public bool jumpRequested { get; private set; }
    private float jumpBufferTime = 0.2f;
    public float lastJumpTime { get; private set; }
    private bool earlyFallEnabled = false;
    private float cmjSmoothing = 0.1f;
    private float cmjSmoothingCurrent;

    void Start()
    {
        SetupInstances();
        Set(airborneState, true);

        initialGravityScale = body.gravityScale;
    }
    void Update()
    {
        GetInput();
        JumpController();
        SelectState();

        state.DoBranch();
    }

    private void FixedUpdate()
    {
        MoveWithInput();
        SetCollisionAdjustment();

        state.FixedDoBranch();
    }
    void SelectState()
    {
        if (state.isComplete)
        {
            if (collisionChecker.grounded)
            {
                Set(groundState, true);
            }
            else
            {
                Set(airborneState, true);
            }
        }
    }


    private void GetInput()
    {
        inputChecker.walkInput = Input.GetAxisRaw("Horizontal");
        inputChecker.runInput = Input.GetKey(KeyCode.LeftShift);

        inputChecker.jumpInput = Input.GetButtonDown("Jump");
        inputChecker.releaseJumpInput = Input.GetButtonUp("Jump");

        inputChecker.meleeInput = Input.GetKeyDown(KeyCode.E);
    }

    private void MoveWithInput()
    {
        if (Mathf.Abs(inputChecker.walkInput) > 0) //check when moving
        {
            float newSpeed;
            float increment = inputChecker.walkInput;

            if (inputChecker.runInput)
            {
                newSpeed = Mathf.Clamp(body.linearVelocityX + increment, -characterStats.runSpeed, characterStats.runSpeed);
            }
            else
            {
                newSpeed = Mathf.Clamp(body.linearVelocityX + increment, -characterStats.walkSpeed, characterStats.walkSpeed);
            }

            body.linearVelocityX = newSpeed;

            // update sprite direction
            bool posDirection =  Mathf.Sign(inputChecker.walkInput) > 0;
            spriteRenderer.flipX = !posDirection;
        }
        else if (!collisionChecker.grounded)
        {
            body.linearVelocityX *= 0.9f; //slow down when xInput is released 
        }
    }

    private void JumpController()
    {
        //initiate jump
        if (inputChecker.jumpInput)
        {
            jumpRequested = true;
            lastJumpTime = Time.time;
            earlyFallEnabled = false;
        }

        //early fall
        if (body.linearVelocityY > 0 && inputChecker.releaseJumpInput)
        {
            body.linearVelocityY = 0;
            earlyFallEnabled = true;
        }

        //jump logic
        if ((jumpRequested && Time.time - lastJumpTime <= jumpBufferTime) && !earlyFallEnabled)
        {
            if (collisionChecker.grounded || airborneState.fallState.coyoteEnabled)
            {
                airborneState.fallState.coyoteEnabled = false;
                airborneState.fallState.coyoteTime = airborneState.fallState.initialCoyoteTime;
                jumpRequested = false;
                body.linearVelocityY = characterStats.jumpSpeed;
            }
        }
    }

    private void SetCollisionAdjustment() //TODO put SetCollisionAdjustment() in a script that can be used by all state game objects
    {
        // Catch Missed Jumps
        if (collisionChecker.cmjEnabled && collisionChecker.characterBoxCollider.offset.y == 0 && Mathf.Abs(inputChecker.walkInput) > 0)
        {
            float currentOffsetY = collisionChecker.CMJThreshold;
            collisionChecker.characterBoxCollider.offset = new Vector2(0.0f, currentOffsetY); // moving collider 1 unit up
            Debug.Log("cmj enabled");

        }
        //catch missed jump (corrects box collider when CMJ is enabled)
        if (collisionChecker.characterBoxCollider.offset.y > 0.0001)
        {
            cmjSmoothingCurrent += Time.fixedDeltaTime;

            float t = cmjSmoothingCurrent / cmjSmoothing;
            float currentOffsetY = Mathf.Lerp(collisionChecker.characterBoxCollider.offset.y, 0.0f, t);

            if (Mathf.Abs(currentOffsetY) < 0.00001f)
            {
                currentOffsetY = 0.0f;
            }

            collisionChecker.characterBoxCollider.offset = new Vector2(0.0f, currentOffsetY);
            collisionChecker.cmjEnabled = false;
        }
        else
        {
            cmjSmoothingCurrent = 0.0f;
        }
    }
}
