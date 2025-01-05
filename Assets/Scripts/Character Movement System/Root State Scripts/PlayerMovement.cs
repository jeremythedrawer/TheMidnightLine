using UnityEngine;

public class PlayerMovement : StateCore
{
    void Start()
    {
        SetupInstances();
        Set(stateList.airborneState, true);

        initialGravityScale = body.gravityScale;
    }
    void Update()
    {
        currentAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        GetPlayerInputs();
        movementInputs.JumpController();
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
            if (currentClimbBounds != null && !stateList.wallState.isDropping)
            {
                Set(stateList.wallState, true);
            }
            else
            {
                if (collisionChecker.grounded || movementInputs.adjustingCollider)
                {
                    //reset wall bools
                    stateList.wallState.isDropping = false;
                    stateList.wallState.isHanging = true;

                    Set(stateList.groundState, true);
                }
                else
                {
                    Set(stateList.airborneState, true);
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
