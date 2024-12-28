using UnityEngine;

public class PlayerMovement : StateCore
{
    //child states
    public AirborneState airborneState;
    public GroundState groundState;
    public WallState wallState;

    void Start()
    {
        SetupInstances();
        Set(airborneState, true);

        initialGravityScale = body.gravityScale;
    }
    void Update()
    {
        currentAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        GetPlayerInputs();
        movementInputs.JumpController(airborneState);
        SelectState();

        state.DoBranch();
    }

    private void FixedUpdate()
    {
        movementInputs.MoveWithInput();
        movementInputs.SetCollisionAdjustment();

        state.FixedDoBranch();
    }
    void SelectState()
    {
        if (state.isComplete)
        {
            if (CarriageClimbingBounds.Instance != null && !wallState.isDropping)
            {
                Set(wallState, true);
            }
            else
            {
                if (collisionChecker.grounded || movementInputs.adjustingCollider)
                {
                    //reset wall bools
                    wallState.isDropping = false;
                    wallState.isHanging = true;

                    Set(groundState, true);
                }
                else
                {
                    Set(airborneState, true);
                }
            }
        }
    }

    private void GetPlayerInputs()
    {
        movementInputs.walkInput = Input.GetAxisRaw("Horizontal");
        movementInputs.runInput = Input.GetKey(KeyCode.LeftShift);

        movementInputs.jumpInput = Input.GetButtonDown("Jump");
        movementInputs.releaseJumpInput = Input.GetButtonUp("Jump");

        movementInputs.crouchInput = Input.GetKey(KeyCode.S);

        movementInputs.meleeInput = Input.GetKeyDown(KeyCode.E);
    }
}
