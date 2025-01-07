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

        normalizedAnimTime = currentAnimStateInfo.normalizedTime;
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
            if (currentClimbBounds != null && !movementInputs.crouchInput)
            {
                Set(stateList.wallState, false);
            }
            else
            {
                if (collisionChecker.grounded || movementInputs.adjustingCollider)
                {

                    Set(stateList.groundState, false);
                }
                else
                {
                    Set(stateList.airborneState, false);
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
