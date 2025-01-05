using UnityEngine;

public class HangState : State
{
    public override void Enter()
    {
    }
    public override void Do()
    {
        if (collisionChecker.grounded || stateList.wallState.isClimbing || movementInputs.crouchInput)
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
        if (core.currentClimbBounds == null)
        {
            stateList.wallState.isHanging = false;
            return;
        }

        if (core.currentClimbBounds.hangActivated)
        {
            movementInputs.canMove = false;
            body.gravityScale = 0;
            body.linearVelocityY = 0;

            //snap player to position
            Vector2 newPosition = core.transform.position;
            newPosition = core.currentClimbBounds.newPos;
            core.transform.position = newPosition;

            core.spriteRenderer.flipX = !core.currentClimbBounds.isLeftEdge;

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
