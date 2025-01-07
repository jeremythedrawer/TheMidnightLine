using UnityEngine;
using System.Collections;

public class WallState : State
{
    public bool isHanging { get; set; }
    public bool isClimbing { get; set; }

    public override void Enter()
    {

    }
    public override void Do()
    {
        if(movementInputs.crouchInput)
        {
            bool canDrop = !isClimbing || core.currentAnimStateInfo.normalizedTime < 0.5; // cannot drop in the second half of the climbing animation

            if (canDrop)
            {
                Exit();
            }
        }
        if (isClimbing && core.currentAnimStateInfo.normalizedTime >= 1f)
        {
            Exit();
        }
        SelectState();
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {
        base.Exit();
        body.gravityScale = initialGravityScale;
        movementInputs.canMove = true;
        body.linearVelocityY = 0;
        isHanging = false;
        isClimbing = false;
    }

    private void SelectState()
    {
        if (isClimbing)
        {
            Set(stateList.climbState, false);
        }
        else
        {
            Set(stateList.hangState, false);
        }
    }
}
