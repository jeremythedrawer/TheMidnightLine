using UnityEngine;
using System.Collections;

public class WallState : State
{
    public bool isClimbing { get; set; } = false;
    public bool isHanging { get; set; } = true;
    public bool isDropping { get; set; } = false;

    private bool removedClimbingPoint;
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
            //removedClimbingPoint = false;
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
            Set(stateList.climbState, true);
        }
        else if (isHanging)
        {
            Set(stateList.hangState, true);
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
            if (core is AgentMovement && !removedClimbingPoint)
            {
                removedClimbingPoint = true;
                navigationSystem.pathToTarget.RemoveAt(0);
            }
        }

    }
}
