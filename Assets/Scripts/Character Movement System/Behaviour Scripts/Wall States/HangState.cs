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
            movementInputs.canMove = true;
            playingAnimation = false;
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
        if (CarriageClimbingBounds.Instance == null)
        {
            wallState.isHanging = false;
            return;
        }

        if (CarriageClimbingBounds.Instance.hangActivated)
        {
            movementInputs.canMove = false;
            body.gravityScale = 0;
            body.linearVelocityY = 0;

            //snap player to position
            Vector2 newPosition = core.transform.position;
            newPosition = CarriageClimbingBounds.Instance.newPos;
            core.transform.position = newPosition;

            core.spriteRenderer.flipX = !CarriageClimbingBounds.Instance.isLeftEdge;

            AnimationController();
        }
    }

    private void AnimationController()
    {
        if (!playingAnimation)
        {
            PlayAnimation(animStates.hangAnimState);
        }
    }
}
