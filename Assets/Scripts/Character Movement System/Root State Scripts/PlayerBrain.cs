using UnityEngine;

public class PlayerBrain : StateCore
{
    void Start()
    {
        SetupInstances();
        Set(stateList.airborneState);

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
        if ((currentClimbBounds?.hangActivated ?? false || stateList.wallState.isClimbing == true) && !movementInputs.crouchInput)
        {
            Set(stateList.wallState);
        }
        else
        {
            if (collisionChecker.grounded || movementInputs.adjustingCollider)
            {
                Set(stateList.groundState);
            }
            else
            {
                Set(stateList.airborneState);
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
