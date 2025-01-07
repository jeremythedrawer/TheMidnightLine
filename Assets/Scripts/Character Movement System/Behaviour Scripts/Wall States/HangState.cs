using UnityEngine;

public class HangState : State
{
    private bool isGrabbingLedge;
    public override void Enter()
    {
    }
    public override void Do()
    {
        HangController();
        stateList.wallState.isHanging = true;
        if (movementInputs.jumpInput)
        {
            core.stateList.wallState.isClimbing = true;
            Exit();
        }

        if (movementInputs.crouchInput)
        {
            Exit();
        }
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {
        isGrabbingLedge = false;
        core.stateList.wallState.isHanging = false;
    }

    private void HangController()
    {
        if (core.currentClimbBounds == null) return;

        if (core.currentClimbBounds.hangActivated)
        {
            stateList.wallState.isHanging = false;
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
        if (!isGrabbingLedge)
        {
            isGrabbingLedge = true;
            PlayAnimation(animStates.grabLedgeState);
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(animStates.hangAnimState, 0, 0);
        }
    }
}
