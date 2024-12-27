using UnityEngine;

public class HangState : State
{
    public WallState wallState;
    public override void Enter()
    {

    }
    public override void Do()
    {
        if (collisionChecker.grounded || wallState.isClimbing || movementInputs.crouchInput)
        {
            wallState.pendingState = false;
            movementInputs.canMove = true;
            isComplete = true;
        }
        else
        {
            HangController();
        }
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }

    private void HangController()
    {
        movementInputs.canMove = false;
        body.gravityScale = 0;
        body.linearVelocityY = 0;
    }
}
