using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class ClimbState : State
{
    public WallState wallState;

    private string climbRightAnimation = "climbRight";

    private float cachePosX;
    private float cachePosY;

    private float newPosX;
    private float newPosY;

    private float offsetX;
    private float offsetY;

    private Vector2 currentPos;
    private bool checkPos = true;
    public override void Enter()
    {
    }
    public override void Do()
    {

        AnimationController();
        UpdatePos();

    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }

    private void UpdatePos()
    {
        if (CarriageClimbingBounds.Instance == null)
        {
            return;
        }

        if (checkPos)
        {
            cachePosX = core.transform.position.x;
            cachePosY = core.transform.position.y;
            if (CarriageClimbingBounds.Instance.isLeftEdge)
            {
                offsetX = boxCollider2D.bounds.size.x;
            }
            else
            {
                offsetX = -boxCollider2D.bounds.size.x;
            }

            newPosX = cachePosX + offsetX;
            offsetY = (boxCollider2D.bounds.size.y + CarriageClimbingBounds.Instance.boxHeight) - (CarriageClimbingBounds.Instance.hangThresholdLine - CarriageClimbingBounds.Instance.boundsMinY);

            newPosY = cachePosY + offsetY; 
            checkPos = false;
        }

        if (core.currentAnimStateInfo.normalizedTime < 1f && !checkPos)
        {
            if (movementInputs.crouchInput)
            {
                ResetClimbingParaemeters();
            }
            else
            {
                UpdateBoxCollider(-offsetX,-offsetY);
                currentPos = new Vector2(newPosX, newPosY);
            }
            core.transform.position = currentPos;
        }
    }

    private void UpdateBoxCollider(float offsetX, float offsetY)
    {
        Vector2 offset = new Vector2(offsetX, offsetY);

        boxCollider2D.offset = Vector2.Lerp(offset, Vector2.zero, core.currentAnimStateInfo.normalizedTime);
    }

    private void AnimationController()
    {
        if (!playingAnimation)
        {
            PlayAnimation(climbRightAnimation);
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1f)
        {
            ResetClimbingParaemeters();
        }
    }

    private void ResetClimbingParaemeters()
    {
        boxCollider2D.offset = Vector2.zero;
        currentPos = new Vector2(cachePosX, cachePosY);
        playingAnimation = false;
        checkPos = true;
        isComplete = true;

        body.linearVelocityY = 0f;

        wallState.isClimbing = false;
        wallState.isDropping = true;
        offsetX = 0f; offsetY = 0f;
    }


}
