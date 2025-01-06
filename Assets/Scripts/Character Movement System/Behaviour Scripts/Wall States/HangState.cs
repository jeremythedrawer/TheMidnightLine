using UnityEngine;

public class HangState : State
{
    private bool startGrabLedgeAnim;
    public override void Enter()
    {
    }
    public override void Do()
    {
        if (stateList.wallState.isDropping || movementInputs.jumpInput)
        {
            startGrabLedgeAnim = false;
            playingAnimation = false;
            isComplete = true;
            if (movementInputs.jumpInput)
            {
                core.stateList.wallState.startClimb = true;
            }
            if (stateList.wallState.isDropping)
            {
                movementInputs.canMove = true;
            }
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
            return;
        }

        if (core.currentClimbBounds.hangActivated)
        {
            stateList.wallState.hasClimbed = false;
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
        if (!startGrabLedgeAnim)
        {
            startGrabLedgeAnim = true;
            PlayAnimation(animStates.grabLedgeState);
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(animStates.hangAnimState, 0, 0);
        }
    }
}
