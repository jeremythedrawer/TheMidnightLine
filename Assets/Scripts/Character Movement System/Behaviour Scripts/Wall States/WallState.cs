using UnityEngine;
using System.Collections;

public class WallState : State
{
    //child states
    public HangState hangState;
    public ClimbState climbState;

    public bool pendingState { get; set; }
    public bool isClimbing { get; set; } = false;
    public bool isHanging { get; set; } = true;
    public bool isDropping { get; set; } = false;
    public override void Enter()
    {

    }
    public override void Do()
    {
        MovementInputDetection();
        SelectState();

        if ((!isClimbing && !isHanging) || isDropping)
        {
            isComplete = true;
        }
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }
    private void SelectState()
    {
        if (!pendingState)
        {
            StartCoroutine(DelayState());
        }
    }

    private IEnumerator DelayState()
    {

        yield return null; // one frame delay


        if (isClimbing)
        {
            Set(climbState, true);
        }
        else if (isHanging)
        {
            Set(hangState, true);
        }

        pendingState = false; // reset bool
    }

    private void MovementInputDetection()
    {
        if (movementInputs.crouchInput)
        {
            body.gravityScale = initialGravityScale;
            movementInputs.canMove = true;

            isDropping = true;
            isClimbing = false;
            isHanging = false;
        }

        if (movementInputs.jumpInput)
        {
            isClimbing = true;
        }

    }
}
