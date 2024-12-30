using UnityEngine;
using System.Collections;

public class WallState : State
{
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
            body.gravityScale = initialGravityScale;
            movementInputs.canMove = true;
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
            StartCoroutine(DelayState());
    }

    private IEnumerator DelayState()
    {

        yield return null; // one frame delay


        if (isClimbing)
        {
            Set(core.stateList.climbState, true);
        }
        else if (isHanging)
        {
            Set(core.stateList.hangState, true);
        }
    }

    private void MovementInputDetection()
    {
        if (movementInputs.crouchInput)
        {
            body.gravityScale = initialGravityScale;
            movementInputs.canMove = true;

            body.linearVelocityY = 0;
            isDropping = true;
            isClimbing = false;
        }

        if (movementInputs.jumpInput)
        {
            isClimbing = true;
        }

    }
}
