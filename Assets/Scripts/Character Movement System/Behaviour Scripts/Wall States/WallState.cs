using UnityEngine;
using System.Collections;

public class WallState : State
{
    public bool hasClimbed { get; set; }
    public bool startClimb { get; set; }

    public bool isDropping { get; set; }

    public override void Enter()
    {

    }
    public override void Do()
    {
        MovementInputDetection();

        if(movementInputs.crouchInput)
        {
            bool canDrop = !startClimb || core.currentAnimStateInfo.normalizedTime < 0.5;

            if (canDrop)
            {
                isDropping = true;
            }
        }

        if (isDropping || hasClimbed)
        {
            body.gravityScale = initialGravityScale;
            movementInputs.canMove = true;
            startClimb = false;
            isComplete = true;

        }
        else
        {
            SelectState();
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

        if (startClimb && !hasClimbed)
        {
            Set(stateList.climbState, true);
        }
        else
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
        }
    }
}
